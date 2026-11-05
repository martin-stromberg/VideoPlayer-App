using System.Net;
using Microsoft.Extensions.Logging;
using UIKit;
using VideoPlayer.ApiClient;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;
using VideoPlayer.TvOS.Ui;

namespace VideoPlayer.TvOS.Services
{
    /// <summary>
    /// Führt MVU-Effekte am App-Rand aus — tvOS-Gegenstück des
    /// <c>ShellAppEffectHandler</c>. Abgedeckt sind die Effekte des Start- und
    /// Verbindungs-Workflows (Ablage, Einstellungen, Serververwaltung,
    /// Erreichbarkeitsprüfung, Pairing/Bootstrap, Anmeldung,
    /// Netzwerk-Monitoring, Ereignisverbindung, Synchronisation, Startseiten-
    /// Listen, Navigation). Alle übrigen Effekte (Downloads, Wiedergabe/PiP,
    /// Inhalts-/Playlist-Verwaltung, Protokollversand u. a.) laufen in ein
    /// protokolliertes No-Op — für tvOS nicht vorgesehene Bereiche werden
    /// bewusst nicht verdrahtet und erzeugen keine Fehler.
    /// </summary>
    public sealed class TvosAppEffectHandler : IAppEffectHandler
    {
        private readonly AppComposition services;
        private readonly IServerHealthProbe healthProbe;
        private readonly AuthSessionService authSession;
        private readonly DevicePairingClient pairing;
        private readonly DeviceTokenService deviceTokens;
        private readonly AppSecretsService secrets;
        private readonly INetworkStatusMonitor networkMonitor;
        private readonly ServerEventsService serverEvents;
        private readonly TvosNavigator navigator;
        private readonly ILogger<TvosAppEffectHandler> logger;

        /// <summary>Initialisiert eine neue Instanz der <see cref="TvosAppEffectHandler"/>-Klasse.</summary>
        /// <param name="services">Kompositionswurzel zum Auflösen der am Effekt beteiligten Dienste (inkl. <see cref="MvuHost"/>).</param>
        /// <param name="healthProbe">Erreichbarkeitsprüfung für den Server.</param>
        /// <param name="authSession">Sitzungsdienst für den Anmelde-Workflow.</param>
        /// <param name="pairing">Öffentlicher Pairing-Endpunkt-Client (ohne Gate-/Bearer-Header).</param>
        /// <param name="deviceTokens">Verwaltung der gepaarten Geräte-Tokens (Provider, Widerruf).</param>
        /// <param name="secrets">Sichere Ablage (Geräte-Tokens je Server).</param>
        /// <param name="networkMonitor">Netzwerkstatus-Erkennung; meldet Wechsel in den MVU-Zyklus.</param>
        /// <param name="serverEvents">Server-Ereignisverbindung; meldet Ereignisse und Trennung in den MVU-Zyklus.</param>
        /// <param name="navigator">Modellgetriebene Navigation des Seitenrahmens.</param>
        /// <param name="logger">Logger des Handlers — Diagnosemeldungen laufen ins Programmprotokoll.</param>
        public TvosAppEffectHandler(
            AppComposition services,
            IServerHealthProbe healthProbe,
            AuthSessionService authSession,
            DevicePairingClient pairing,
            DeviceTokenService deviceTokens,
            AppSecretsService secrets,
            INetworkStatusMonitor networkMonitor,
            ServerEventsService serverEvents,
            TvosNavigator navigator,
            ILogger<TvosAppEffectHandler> logger)
        {
            this.services = services;
            this.healthProbe = healthProbe;
            this.authSession = authSession;
            this.pairing = pairing;
            this.deviceTokens = deviceTokens;
            this.secrets = secrets;
            this.networkMonitor = networkMonitor;
            this.serverEvents = serverEvents;
            this.navigator = navigator;
            this.logger = logger;

            // Rückmeldungen aus dem Dienst-Rand in den MVU-Zyklus
            // (Muster: ShellAppEffectHandler-Konstruktor).
            this.authSession.SessionExpired += OnSessionExpired;
            this.deviceTokens.DeviceTokenRejected += OnDeviceTokenRejected;
            this.networkMonitor.AvailabilityChanged += OnNetworkAvailabilityChanged;
            this.serverEvents.EventReceived += OnServerEventReceived;
            this.serverEvents.ConnectionClosed += OnServerEventsConnectionClosed;
        }

        /// <inheritdoc/>
        public async Task HandleAsync(AppEffect effect, CancellationToken cancellationToken = default)
        {
            switch (effect)
            {
                case CheckServerHealthEffect:
                    await RunHealthCheckAsync(cancellationToken);
                    break;
                case NavigateToPageEffect navigate:
                    await MainThreadInvoker.InvokeOnMainThreadAsync(
                        () => navigator.NavigateTo(navigate.Page));
                    break;
                case InitializeStorageEffect:
                    await InitializeStorageAsync();
                    break;
                case LoadSettingsEffect:
                    await LoadSettingsAsync();
                    break;
                case PersistSettingsEffect persist:
                    SaveSettings(persist.Settings);
                    break;
                case LoadServersEffect:
                    await LoadServersAsync(cancellationToken);
                    break;
                case DiscoverServersEffect:
                    await DiscoverServersAsync(cancellationToken);
                    break;
                case CheckCandidateServerEffect check:
                    await CheckCandidateServerAsync(check.Address, check.DisplayName, cancellationToken);
                    break;
                case PersistActiveServerEffect persist:
                    await PersistActiveServerAsync(persist.Address, persist.DisplayName);
                    break;
                case ExchangePairingCodeEffect exchange:
                    await ExchangePairingCodeAsync(exchange.ServerAddress, exchange.Code, cancellationToken);
                    break;
                case CheckBootstrapServerEffect check:
                    await CheckBootstrapServerAsync(check.Address, check.Ticket, cancellationToken);
                    break;
                case ExchangeBootstrapTicketEffect bootstrap:
                    await ExchangeBootstrapTicketAsync(bootstrap.ServerAddress, bootstrap.Ticket, cancellationToken);
                    break;
                case AttemptAutoLoginEffect autoLogin:
                    await AttemptAutoLoginAsync(autoLogin.ServerAddress, cancellationToken);
                    break;
                case AttemptLoginEffect login:
                    await AttemptLoginAsync(login.ServerAddress, login.Username, login.Password, cancellationToken);
                    break;
                case CheckOfflineDataEffect:
                    await CheckOfflineDataAsync();
                    break;
                case StartNetworkMonitoringEffect:
                    await StartNetworkMonitoringAsync();
                    break;
                case ConnectServerEventsEffect connect:
                    await ConnectServerEventsAsync(connect.ServerAddress, cancellationToken);
                    break;
                case DisconnectServerEventsEffect:
                    await DisconnectServerEventsAsync();
                    break;
                case RunFullSyncEffect:
                    await RunFullSyncAsync(cancellationToken);
                    break;
                case SyncContinueWatchingEffect:
                    await RunPartialSyncAsync("Weiterschauen", s => s.SyncContinueWatchingAsync(cancellationToken));
                    break;
                case SyncFavoritesEffect:
                    await RunPartialSyncAsync("Favoriten", s => s.SyncFavoritesAsync(cancellationToken));
                    break;
                case SyncMetadataEffect:
                    await RunPartialSyncAsync("Metadaten", s => s.SyncMetadataAsync(cancellationToken));
                    break;
                case LoadHomeListsEffect:
                    await LoadHomeListsAsync();
                    break;
                case LogoutEffect:
                    await LogoutAsync();
                    break;
                default:
                    // Für tvOS nicht verdrahtete Bereiche (Downloads,
                    // Wiedergabe/PiP, Inhalts-/Playlist-Verwaltung,
                    // Protokollversand u. a.): bewusst wirkungslos.
                    logger.LogDebug("Effekt {EffectType} nicht verdrahtet — wird ignoriert.", effect.GetType().Name);
                    break;
            }
        }

        private async Task InitializeStorageAsync()
        {
            // Fehler der Ablage-Initialisierung dürfen den Start nicht
            // blockieren (Muster: ShellAppEffectHandler).
            var recovered = false;
            var failed = false;
            try
            {
                _ = services.StorageLayout;
                recovered |= await services.BackupDatabase.InitializeWithRecoveryAsync();
            }
            catch (Exception ex)
            {
                failed = true;
                logger.LogError(ex, "Backup-Ablage-Initialisierung fehlgeschlagen");
            }

            try
            {
                recovered |= await services.CacheDatabase.InitializeWithRecoveryAsync();
            }
            catch (Exception ex)
            {
                failed = true;
                logger.LogError(ex, "Cache-Ablage-Initialisierung fehlgeschlagen");
            }

            logger.LogInformation("Ablage initialisiert (Wiederherstellung: {Recovered}).", recovered);
            if (recovered)
            {
                await DispatchMessageAsync(new StorageRecoveredMessage(
                    "Die lokalen Daten waren beschädigt und wurden neu initialisiert. " +
                    "Zwischengespeicherte Inhalte werden neu synchronisiert."));
            }
            else if (failed)
            {
                await DispatchMessageAsync(new StorageRecoveredMessage(
                    "Ein Teil der lokalen Daten konnte nicht gelesen werden. " +
                    "Die App läuft mit eingeschränkten lokalen Daten weiter."));
            }
        }

        private async Task LoadSettingsAsync()
        {
            AppSettings settings;
            try
            {
                settings = services.Settings.Load();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Einstellungen laden fehlgeschlagen");
                settings = new AppSettings();
            }

            services.AppLog.SetEnabled(settings.LoggingEnabled);
            await DispatchMessageAsync(new SettingsLoadedMessage(settings));
        }

        private void SaveSettings(AppSettings settings)
        {
            try
            {
                services.Settings.Save(settings);
                services.AppLog.SetEnabled(settings.LoggingEnabled);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Einstellungen speichern fehlgeschlagen");
            }
        }

        private async Task LoadServersAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<KnownServer> servers = [];
            string? activeAddress = null;
            try
            {
                var knownServers = services.KnownServers;
                await knownServers.MigrateLegacyServerAddressAsync();
                servers = await knownServers.GetAllAsync();

                var configured = await services.ServerAddressProvider
                    .GetConfiguredServerAddressAsync(cancellationToken);
                activeAddress = configured?.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Serververwaltung laden fehlgeschlagen");
            }

            // Geräte-Token des aktiven Servers laden und den Gate-Provider
            // auf dessen Server-Kontext einstellen.
            await deviceTokens.LoadForServerAsync(activeAddress, cancellationToken);
            var pairingRequired = await IsPairingRequiredAsync(activeAddress);

            await DispatchMessageAsync(new ServersLoadedMessage(servers, activeAddress, pairingRequired));
        }

        private async Task DiscoverServersAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<VideoPlayer.ApiClient.Discovery.DiscoveredServer> found = [];
            try
            {
                found = await services.ServerDiscovery.DiscoverAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server-Erkennung fehlgeschlagen");
            }

            await DispatchMessageAsync(new ServerDiscoveryCompletedMessage(found));
        }

        private async Task CheckCandidateServerAsync(string address, string? displayName, CancellationToken cancellationToken)
        {
            var success = false;
            var pairingRequired = false;
            try
            {
                var result = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
                if (result == HealthCheckResult.GateRejected)
                {
                    // Gate-Ablehnung des Kandidaten-Tokens: gezielt löschen
                    // (Muster: ShellAppEffectHandler).
                    try
                    {
                        await secrets.ClearDeviceTokenAsync(address);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Geräte-Token des Kandidaten konnte nicht verworfen werden");
                    }

                    await deviceTokens.DiscardIfCurrentAsync(address);
                }

                success = result != HealthCheckResult.Unreachable;
                if (success)
                {
                    pairingRequired = await IsPairingRequiredAsync(address);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kandidatenprüfung fehlgeschlagen");
            }

            await DispatchMessageAsync(new ServerCheckCompletedMessage(address, displayName, success, pairingRequired));
        }

        private async Task PersistActiveServerAsync(string address, string? displayName)
        {
            try
            {
                await services.KnownServers.SetActiveServerAsync(address, displayName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server speichern fehlgeschlagen");
            }

            // Provider auf das Geräte-Token des neuen Servers umstellen, bevor
            // der nachfolgende Auto-Login-Effekt läuft (Effekte laufen
            // sequenziell).
            await deviceTokens.LoadForServerAsync(address);
        }

        private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
        {
            // Jeder Fehler der Prüfung führt zu „nicht erreichbar" — die App
            // darf nicht auf der Ladeanzeige hängen bleiben.
            var result = HealthCheckResult.Unreachable;
            try
            {
                result = await healthProbe.CheckAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreichbarkeitsprüfung fehlgeschlagen");
            }

            if (result == HealthCheckResult.GateRejected)
            {
                // Verworfenes Geräte-Token entfernen und einmalig mit der
                // statischen Auflösung erneut prüfen (Muster:
                // ShellAppEffectHandler).
                await deviceTokens.DiscardCurrentAsync();
                try
                {
                    result = await healthProbe.CheckAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreichbarkeits-Nachprüfung fehlgeschlagen");
                    result = HealthCheckResult.Unreachable;
                }

                if (result == HealthCheckResult.GateRejected)
                {
                    deviceTokens.NotifyRejected();
                    return;
                }
            }

            var pairingRequired = result == HealthCheckResult.Reachable
                && await IsPairingRequiredAsync(services.Host.Model.ActiveServerAddress);

            await DispatchMessageAsync(new ServerHealthCheckCompletedMessage(
                result == HealthCheckResult.Reachable, pairingRequired));
        }

        private async Task<bool> IsPairingRequiredAsync(string? serverAddress)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                return false;
            }

            try
            {
                if (await secrets.HasDeviceTokenAsync(serverAddress))
                {
                    return false;
                }

                return ClientGate.ResolveStatic() == ClientGate.ApiTokenPlaceholder;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pairing-Erforderlichkeit fehlgeschlagen");
                return false;
            }
        }

        private async Task ExchangePairingCodeAsync(string serverAddress, string code, CancellationToken cancellationToken)
        {
            PairingStatus status;
            try
            {
                var token = await pairing.ExchangeAsync(
                    new Uri(serverAddress), code, GetDeviceName(), cancellationToken);

                // Atomarer Stale-Guard wie im MAUI-Handler: persistiert +
                // aktiviert das Token nur, wenn die Adresse noch der aktuell
                // bediente Server ist.
                var model = services.Host.Model;
                var pairingCurrent = model.IsPairingRunning
                    && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                    && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                    && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
                if (pairingCurrent)
                {
                    var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                        serverAddress, token, cancellationToken);
                    if (activated)
                    {
                        authSession.Client?.SetApiKey(token);
                    }
                }
                else
                {
                    await secrets.SetDeviceTokenAsync(serverAddress, token);
                }

                status = PairingStatus.Succeeded;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException ex)
            {
                status = ex.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => PairingStatus.InvalidCode,
                    HttpStatusCode.TooManyRequests => PairingStatus.RateLimited,
                    HttpStatusCode.BadRequest => PairingStatus.InvalidRequest,
                    HttpStatusCode.NotFound => PairingStatus.NotSupported,
                    null => PairingStatus.ServerUnreachable,
                    _ => PairingStatus.Failed
                };
                logger.LogError(ex, "Pairing-Austausch fehlgeschlagen");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pairing-Austausch fehlgeschlagen");
                status = PairingStatus.Failed;
            }

            await DispatchMessageAsync(new PairingCompletedMessage(status));
        }

        private async Task CheckBootstrapServerAsync(string address, string ticket, CancellationToken cancellationToken)
        {
            var success = false;
            try
            {
                var result = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
                success = result != HealthCheckResult.Unreachable;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bootstrap-Serverprüfung fehlgeschlagen");
            }

            await DispatchMessageAsync(new BootstrapServerCheckedMessage(address, success, ticket));
        }

        private async Task ExchangeBootstrapTicketAsync(string serverAddress, string ticket, CancellationToken cancellationToken)
        {
            BootstrapStatus status;
            try
            {
                var payload = await pairing.BootstrapAsync(
                    new Uri(serverAddress), ticket, GetDeviceName(), cancellationToken);

                var model = services.Host.Model;
                var bootstrapCurrent = model.IsBootstrapRunning
                    && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                    && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                    && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
                if (bootstrapCurrent)
                {
                    var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                        serverAddress, payload.DeviceToken, cancellationToken);
                    if (activated)
                    {
                        authSession.Client?.SetApiKey(payload.DeviceToken);
                    }

                    await secrets.SetAuthTokenAsync(payload.Token);
                    await secrets.SetRefreshTokenAsync(payload.RefreshToken);
                }
                else
                {
                    await secrets.SetDeviceTokenAsync(serverAddress, payload.DeviceToken);
                }

                status = BootstrapStatus.Succeeded;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (ApiException ex)
            {
                status = ex.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => BootstrapStatus.InvalidTicket,
                    HttpStatusCode.TooManyRequests => BootstrapStatus.RateLimited,
                    HttpStatusCode.BadRequest => BootstrapStatus.InvalidRequest,
                    HttpStatusCode.NotFound => BootstrapStatus.NotSupported,
                    null => BootstrapStatus.ServerUnreachable,
                    _ => BootstrapStatus.Failed
                };
                logger.LogError(ex, "Bootstrap-Austausch fehlgeschlagen");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bootstrap-Austausch fehlgeschlagen");
                status = BootstrapStatus.Failed;
            }

            await DispatchMessageAsync(new BootstrapCompletedMessage(status));
        }

        private async Task AttemptAutoLoginAsync(string serverAddress, CancellationToken cancellationToken)
        {
            AuthAttempt attempt;
            try
            {
                attempt = await authSession.TryStoredLoginAsync(new Uri(serverAddress), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Automatische Anmeldung fehlgeschlagen");
                attempt = new AuthAttempt(AuthAttemptStatus.Failed);
            }

            await DispatchMessageAsync(new AutoLoginCompletedMessage(attempt.Status, attempt.Username));
        }

        private async Task AttemptLoginAsync(string serverAddress, string username, string password, CancellationToken cancellationToken)
        {
            AuthAttempt attempt;
            try
            {
                attempt = await authSession.LoginAsync(new Uri(serverAddress), username, password, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Anmeldung fehlgeschlagen");
                attempt = new AuthAttempt(AuthAttemptStatus.Failed);
            }

            await DispatchMessageAsync(new LoginCompletedMessage(attempt.Status));
        }

        private async Task CheckOfflineDataAsync()
        {
            // Jeder Fehler der Prüfung gilt als „keine Offline-Daten" — die
            // App zeigt dann die Serverauswahl statt im Ladezustand zu
            // verharren.
            var hasOfflineData = false;
            try
            {
                hasOfflineData = await services.CacheDatabase.HasOfflineDataAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Offline-Daten-Prüfung fehlgeschlagen");
            }

            await DispatchMessageAsync(new OfflineDataCheckCompletedMessage(hasOfflineData));
        }

        private async Task StartNetworkMonitoringAsync()
        {
            try
            {
                networkMonitor.Start();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Netzwerk-Monitoring fehlgeschlagen");
            }

            // Ist-Wert in den MVU-Zyklus melden.
            await DispatchMessageAsync(new NetworkAvailabilityChangedMessage(networkMonitor.IsOnline));
        }

        private async Task ConnectServerEventsAsync(string serverAddress, CancellationToken cancellationToken)
        {
            try
            {
                await serverEvents.ConnectAsync(new Uri(serverAddress), cancellationToken);
            }
            catch (Exception ex)
            {
                // Nicht fatal — nächster Netzwerkwechsel bietet eine neue
                // Gelegenheit.
                logger.LogError(ex, "Ereignisverbindung fehlgeschlagen");
            }
        }

        private async Task DisconnectServerEventsAsync()
        {
            try
            {
                await serverEvents.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Trennen der Ereignisverbindung fehlgeschlagen");
            }
        }

        private async Task RunFullSyncAsync(CancellationToken cancellationToken)
        {
            SyncResult result;
            try
            {
                result = await services.ServerSync.SynchronizeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Synchronisation fehlgeschlagen");
                result = new SyncResult(0, 0, false, false, false);
            }

            await DispatchMessageAsync(new SynchronizationCompletedMessage(result));
        }

        private async Task RunPartialSyncAsync(string listName, Func<ServerSyncService, Task> sync)
        {
            try
            {
                await sync(services.ServerSync);
                logger.LogDebug("Teil-Synchronisation {List} abgeschlossen.", listName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Teil-Synchronisation fehlgeschlagen");
            }

            await DispatchMessageAsync(new CachedListsChangedMessage());
        }

        private async Task LoadHomeListsAsync()
        {
            HomeLists lists;
            try
            {
                lists = services.Host.Model.IsOffline
                    ? await services.OfflineContinueWatching.LoadHomeListsAsync()
                    : await services.HomeLists.LoadHomeListsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Startseiten-Listen laden fehlgeschlagen");
                lists = HomeLists.Empty;
            }

            await DispatchMessageAsync(new HomeListsLoadedMessage(lists));
        }

        private async Task LogoutAsync()
        {
            try
            {
                await authSession.LogoutAsync();
                // Der Erfolgs-Eintrag obliegt AuthSessionService (eine
                // Schicht pro Ereignis) — hier nur der Fehlerpfad.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Abmeldung fehlgeschlagen");
            }
        }

        private void OnSessionExpired(object? sender, EventArgs e) =>
            _ = DispatchMessageAsync(new SessionExpiredMessage());

        private void OnDeviceTokenRejected(object? sender, EventArgs e) =>
            _ = DispatchMessageAsync(new DeviceTokenRevokedMessage());

        private void OnNetworkAvailabilityChanged(object? sender, bool isOnline) =>
            _ = DispatchMessageAsync(new NetworkAvailabilityChangedMessage(isOnline));

        private void OnServerEventReceived(object? sender, ServerEventReceivedEventArgs e) =>
            _ = DispatchMessageAsync(new ServerEventReceivedMessage(e.Kind, e.SourceId));

        private void OnServerEventsConnectionClosed(object? sender, EventArgs e) =>
            _ = DispatchMessageAsync(new ServerEventsConnectionClosedMessage());

        private async Task DispatchMessageAsync(AppMessage message)
        {
            try
            {
                var host = services.Host;
                await MainThreadInvoker.InvokeOnMainThreadAsync(() => host.DispatchAsync(message));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dispatch von {MessageType} fehlgeschlagen", message.GetType().Name);
            }
        }

        private static string? GetDeviceName()
        {
            var name = UIDevice.CurrentDevice.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = UIDevice.CurrentDevice.Model;
            }

            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            return trimmed.Length > 200 ? trimmed[..200] : trimmed;
        }
    }
}
