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
    /// Listen, Navigation), des lesenden Playlist-Zugriffs
    /// (Übersicht, Kopfdaten, paginierte Einträge, Wiedergabekontext)
    /// sowie der manuellen Downloads (Einreihen, Abbrechen,
    /// Wiederholen, Löschen, Wiederaufnahme, Ablauf-/Abgleichslauf).
    /// Alle übrigen Effekte (Auto-Download-Vorlauf,
    /// Inhalts-/Playlist-Verwaltung, Protokollversand u. a.) laufen in
    /// ein protokolliertes No-Op — für tvOS nicht vorgesehene Bereiche
    /// werden bewusst nicht verdrahtet und erzeugen keine Fehler.
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
            // Zustands-/Entfernungsmeldungen des Download-Manifests in
            // den MVU-Zyklus (Wiedergabe-Schutz/Ablauf-Verkürzung
            // melden ihre Änderungen darüber zurück — Muster
            // ShellAppEffectHandler).
            services.Downloads.ItemChanged += OnDownloadItemChanged;
            services.Downloads.ItemRemoved += OnDownloadItemRemoved;
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
                case LoadSourcesEffect:
                    await LoadSourcesAsync(cancellationToken);
                    break;
                case LoadSourceGenresEffect genres:
                    await LoadSourceGenresAsync(genres, cancellationToken);
                    break;
                case LoadSourceItemsEffect items:
                    await LoadSourceItemsAsync(items, cancellationToken);
                    break;
                case NavigateToSourceOverviewEffect overview:
                    // Modale Browsing-Seite der Quelle — Pendant zur
                    // Shell-Route sourceoverview (Hauptthread:
                    // UIKit-Präsentation).
                    await MainThreadInvoker.InvokeOnMainThreadAsync(() =>
                    {
                        navigator.PresentSourceBrowser(overview.SourceId, overview.SourceName);
                        return Task.CompletedTask;
                    });
                    break;
                case NavigateToMediaDetailsEffect details:
                    // Echte Detailseite des Mediums — Pendant zur
                    // Shell-Route mediadetails (Hauptthread:
                    // UIKit-Präsentation auf dem modalen Stapel).
                    await MainThreadInvoker.InvokeOnMainThreadAsync(() =>
                    {
                        navigator.PresentMediaDetails(details);
                        return Task.CompletedTask;
                    });
                    break;
                case LoadMediaDetailsEffect loadDetails:
                    await LoadMediaDetailsAsync(loadDetails, cancellationToken);
                    break;
                case LoadMediaDetailsHeaderEffect header:
                    await LoadMediaDetailsHeaderAsync(header, cancellationToken);
                    break;
                case ToggleFavoriteEffect toggle:
                    await ToggleFavoriteAsync(toggle, cancellationToken);
                    break;
                case LoadDownloadsEffect load:
                    await LoadDownloadsAsync(load.Settings, cancellationToken);
                    break;
                case EnqueueDownloadEffect enqueue:
                    await EnqueueDownloadAsync(enqueue, cancellationToken);
                    break;
                case CancelDownloadEffect cancel:
                    await CancelDownloadAsync(cancel, cancellationToken);
                    break;
                case DeleteDownloadEffect delete:
                    await DeleteDownloadAsync(delete, cancellationToken);
                    break;
                case DeleteAllDownloadsEffect deleteAll:
                    await DeleteAllDownloadsAsync(deleteAll.Settings, cancellationToken);
                    break;
                case PurgeOrphanedDownloadsEffect purge:
                    await PurgeOrphanedDownloadsAsync(purge.Settings, cancellationToken);
                    break;
                case DeleteFailedDownloadsEffect deleteFailed:
                    await DeleteFailedDownloadsAsync(deleteFailed.Settings, cancellationToken);
                    break;
                case ResumeDownloadsEffect resume:
                    await ResumeDownloadsAsync(resume.Settings, cancellationToken);
                    break;
                case ReconcileDownloadsEffect reconcile:
                    await ReconcileDownloadsAsync(reconcile.Settings, cancellationToken);
                    break;
                case NavigateToPlayerEffect:
                    // Nativer Vollbild-Player (AVPlayerViewController) —
                    // Pendant zur Shell-Route player der MAUI-App
                    // (Hauptthread: UIKit-Präsentation auf dem modalen
                    // Stapel).
                    await MainThreadInvoker.InvokeOnMainThreadAsync(() =>
                    {
                        navigator.PresentPlayer();
                        return Task.CompletedTask;
                    });
                    break;
                case ResolvePlaybackSourceEffect resolve:
                    await ResolvePlaybackSourceAsync(resolve, cancellationToken);
                    break;
                case LoadUpcomingEpisodesEffect loadUpcoming:
                    await LoadUpcomingEpisodesAsync(loadUpcoming, cancellationToken);
                    break;
                case ReportPlaybackProgressEffect report:
                    await ReportPlaybackProgressAsync(report, cancellationToken);
                    break;
                case LoadPlaylistsEffect:
                    await LoadPlaylistsAsync(cancellationToken);
                    break;
                case NavigateToPlaylistDetailsEffect playlistDetails:
                    // Modale Playlist-Detailseite — Pendant zur
                    // Shell-Route playlistdetails (Hauptthread:
                    // UIKit-Präsentation auf dem modalen Stapel).
                    await MainThreadInvoker.InvokeOnMainThreadAsync(() =>
                    {
                        navigator.PresentPlaylistDetails(
                            playlistDetails.PlaylistId, playlistDetails.Name);
                        return Task.CompletedTask;
                    });
                    break;
                case LoadPlaylistDetailsEffect playlistDetailsLoad:
                    await LoadPlaylistDetailsAsync(playlistDetailsLoad, cancellationToken);
                    break;
                case LoadPlaylistEntriesEffect playlistEntries:
                    await LoadPlaylistEntriesAsync(playlistEntries, cancellationToken);
                    break;
                case StartPlaylistPlaybackEffect start:
                    await StartPlaylistPlaybackAsync(start, cancellationToken);
                    break;
                case LoadNextPlaylistEntryEffect next:
                    await NavigatePlaylistEntryAsync(
                        next.PlaylistId, next.CurrentEntryId, forward: true, advance: false, cancellationToken);
                    break;
                case LoadPreviousPlaylistEntryEffect previous:
                    await NavigatePlaylistEntryAsync(
                        previous.PlaylistId, previous.CurrentEntryId, forward: false, advance: false, cancellationToken);
                    break;
                case AdvancePlaylistEntryEffect advance:
                    await NavigatePlaylistEntryAsync(
                        advance.PlaylistId, advance.CurrentEntryId, forward: true, advance: true, cancellationToken);
                    break;
                case ProtectDownloadExpiryEffect protect:
                    await ProtectDownloadExpiryAsync(protect, cancellationToken);
                    break;
                case ExpireWatchedDownloadEffect expireWatched:
                    await ExpireWatchedDownloadAsync(expireWatched, cancellationToken);
                    break;
                case LogoutEffect:
                    await LogoutAsync();
                    break;
                default:
                    // Für tvOS nicht verdrahtete Bereiche (Auto-
                    // Download-Vorlauf, Wiedergabe/PiP, Inhalts-/
                    // Playlist-Verwaltung, Protokollversand u. a.):
                    // bewusst wirkungslos.
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

        private async Task LoadSourcesAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<SourceItem> sources = [];
            string? error = null;
            try
            {
                sources = await services.SourceBrowser.LoadSourcesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Quellen laden fehlgeschlagen");
                error = "Die Quellen konnten nicht geladen werden.";
            }

            await DispatchMessageAsync(new SourcesLoadedMessage(sources, error));
        }

        private async Task LoadSourceGenresAsync(LoadSourceGenresEffect genres, CancellationToken cancellationToken)
        {
            IReadOnlyList<SourceGenreItem> items;
            try
            {
                items = await services.SourceBrowser.LoadGenresAsync(genres.SourceId, cancellationToken);
            }
            catch (Exception ex)
            {
                // Genre-Ladefehler sind nicht fatal — die Leiste zeigt
                // dann nur „Alle" (kein Filter) — Muster:
                // ShellAppEffectHandler.
                logger.LogError(ex, "Genres laden fehlgeschlagen");
                items = [new SourceGenreItem { Id = 0, Name = SourceBrowserService.AllGenresName, IsSelected = true }];
            }

            await DispatchMessageAsync(new SourceGenresLoadedMessage(genres.SourceId, items));
        }

        private async Task LoadSourceItemsAsync(LoadSourceItemsEffect items, CancellationToken cancellationToken)
        {
            SourceItemsPage page;
            string? error = null;
            try
            {
                page = await services.SourceBrowser.LoadItemsAsync(
                    items.SourceId, items.Page, items.SearchText, items.GenreId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Quelleneinträge laden fehlgeschlagen");
                page = new SourceItemsPage(items.Page, [], HasMore: false);
                error = "Die Einträge konnten nicht geladen werden.";
            }

            await DispatchMessageAsync(new SourceItemsLoadedMessage(
                items.SourceId, page.Page, page.Items, page.HasMore, items.Append, error));
        }

        private async Task LoadMediaDetailsAsync(LoadMediaDetailsEffect loadDetails, CancellationToken cancellationToken)
        {
            MediaDetailsView? details = null;
            string? error = null;
            try
            {
                // Online aus dem Server, ohne Client aus dem gespeicherten
                // Offline-Snapshot (Muster: ShellAppEffectHandler).
                details = await services.MediaDetails.LoadAsync(
                    loadDetails.MediaType, loadDetails.MediaId, loadDetails.Title,
                    loadDetails.ShowId, loadDetails.SeasonId, loadDetails.EpisodeId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Detail-Ladung fehlgeschlagen");
                error = "Die Details konnten nicht geladen werden.";
            }

            await DispatchMessageAsync(new MediaDetailsLoadedMessage(
                loadDetails.MediaType, loadDetails.MediaId, details, error));
        }

        private async Task LoadMediaDetailsHeaderAsync(LoadMediaDetailsHeaderEffect header, CancellationToken cancellationToken)
        {
            byte[]? imageBytes = null;
            try
            {
                // Episoden-Still-Endpunkt bzw. aufgelöste Bildquelle
                // (Bearer + stille Token-Erneuerung in der
                // Sende-Pipeline). Ohne Sitzung bzw. bei Fehlschlag
                // bleibt das Bild aus — der Kopfbereich zeigt dann nur
                // Verlauf und Texte.
                var client = authSession.Client;
                if (client is not null)
                {
                    var bytes = header.HeaderEpisodeId is > 0
                        ? await client.GetEpisodeBackgroundImageAsync(header.HeaderEpisodeId.Value, cancellationToken)
                        : header.HeaderPictureId is > 0
                            ? await client.GetPictureAsync(header.HeaderPictureId.Value, cancellationToken)
                            : [];
                    imageBytes = bytes.Length > 0 ? bytes : null;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Detail-Kopfbild laden fehlgeschlagen");
            }

            await DispatchMessageAsync(new MediaDetailsHeaderImageLoadedMessage(
                header.MediaType, header.MediaId,
                header.HeaderPictureId, header.HeaderEpisodeId, imageBytes));
        }

        private async Task ToggleFavoriteAsync(ToggleFavoriteEffect toggle, CancellationToken cancellationToken)
        {
            bool? isFavorite = null;
            try
            {
                // Erlaubtes Nutzungsdatum: Favoriten-Umschaltung über den
                // Server (kein Inhalts-Schreibzugriff).
                isFavorite = await services.Favorites.ToggleAsync(
                    toggle.MediaType, toggle.MediaId, toggle.Title, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Favoriten-Umschaltung fehlgeschlagen");
            }

            await DispatchMessageAsync(new FavoriteToggleCompletedMessage(
                isFavorite, toggle.MediaType, toggle.MediaId));

            if (isFavorite is not null)
            {
                // Die lokale Favoriten-Kopie wurde aktualisiert — die
                // Startseiten-Liste lädt den neuen Stand nach.
                await DispatchMessageAsync(new CachedListsChangedMessage());
            }
        }

        private async Task ResolvePlaybackSourceAsync(ResolvePlaybackSourceEffect resolve, CancellationToken cancellationToken)
        {
            PlaybackSource? source = null;
            string? error = null;
            try
            {
                // Im Offline-Zustand werden nur lokale Dateien aufgelöst —
                // eine unerreichbare Stream-URL würde erst im Player
                // fehlschlagen (Muster: ShellAppEffectHandler).
                var allowServerStream = !services.Host.Model.IsOffline;
                source = await services.PlaybackSources.ResolveAsync(
                    resolve.MediaType, resolve.MediaId, cancellationToken,
                    allowServerStream: allowServerStream,
                    resumePositionSecondsOverride: resolve.StartPositionSeconds);
                if (source is null)
                {
                    error = AppUpdate.PlaybackUnavailableError;
                }
                else
                {
                    // Die Quell-Adresse bleibt absichtlich unprotokolliert
                    // (access_token-Query).
                    var player = services.Host.Model.Player;
                    logger.LogInformation(
                        "Wiedergabe gestartet: {MediaType}/{MediaId} ({Title}) über {Kind}.",
                        resolve.MediaType,
                        resolve.MediaId,
                        player.MediaId == resolve.MediaId ? player.Title : null,
                        source.Kind);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Quellauflösung fehlgeschlagen");
                error = AppUpdate.PlaybackUnavailableError;
            }

            await DispatchMessageAsync(new PlaybackSourceResolvedMessage(
                resolve.MediaType, resolve.MediaId, source, error));
        }

        private async Task LoadUpcomingEpisodesAsync(LoadUpcomingEpisodesEffect loadUpcoming, CancellationToken cancellationToken)
        {
            IReadOnlyList<PlayerEpisodeRef> episodes = [];
            try
            {
                episodes = await services.UpcomingEpisodes.LoadAsync(
                    loadUpcoming.MediaId, loadUpcoming.ShowId, cancellationToken);
            }
            catch (Exception ex)
            {
                // Fehlschlag ergibt eine leere Fortsetzungsliste statt
                // eines Fehlerzustands (Muster: ShellAppEffectHandler).
                logger.LogError(ex, "Folge-Episoden laden fehlgeschlagen");
            }

            await DispatchMessageAsync(new UpcomingEpisodesLoadedMessage(loadUpcoming.MediaId, episodes));
        }

        private async Task ReportPlaybackProgressAsync(ReportPlaybackProgressEffect report, CancellationToken cancellationToken)
        {
            try
            {
                await services.ProgressReports.ReportProgressAsync(
                    report.MediaType, (int)report.MediaId,
                    report.Position, report.Duration, report.PlaylistId,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Ein Fehlschlag ist nicht fatal — der nächste Takt
                // meldet erneut.
                logger.LogWarning(ex, "Fortschrittsmeldung fehlgeschlagen");
            }
        }

        private async Task LoadPlaylistsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<PlaylistListItem> own = [];
            IReadOnlyList<PlaylistListItem> publicPlaylists = [];
            string? error = null;
            try
            {
                var overview = await services.Playlists.LoadPlaylistsAsync(cancellationToken);
                own = overview.Own;
                publicPlaylists = overview.Public;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playlists laden fehlgeschlagen");
                error = AppUpdate.PlaylistsLoadError;
            }

            await DispatchMessageAsync(new PlaylistsLoadedMessage(own, publicPlaylists, error));
        }

        private async Task LoadPlaylistDetailsAsync(LoadPlaylistDetailsEffect details, CancellationToken cancellationToken)
        {
            PlaylistListItem? info = null;
            var isAdmin = false;
            try
            {
                info = await services.Playlists.LoadPlaylistAsync(details.PlaylistId, cancellationToken);
                // Der IsAdmin-Claim steuert die Sichtbarkeit des
                // Öffentlich-Schalters (auf tvOS ohne Oberfläche); er
                // ist nicht fatal — bei Fehlschlag bleibt er aus.
                isAdmin = await services.Playlists.IsCurrentUserAdminAsync();
            }
            catch (Exception ex)
            {
                // Kopf-Nachschlag ist nicht fatal — die Detailseite zeigt
                // dann die Felder der Listenauswahl bzw. den Pfad-Namen.
                logger.LogError(ex, "Playlist-Kopfdaten laden fehlgeschlagen");
            }

            await DispatchMessageAsync(new PlaylistDetailsLoadedMessage(details.PlaylistId, info, isAdmin));
        }

        private async Task LoadPlaylistEntriesAsync(LoadPlaylistEntriesEffect entries, CancellationToken cancellationToken)
        {
            PlaylistEntriesPage page;
            string? error = null;
            try
            {
                page = await services.Playlists.LoadEntriesAsync(
                    entries.PlaylistId, entries.Page, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playlist-Einträge laden fehlgeschlagen");
                page = new PlaylistEntriesPage(entries.Page, [], HasMore: false, TotalCount: 0);
                error = AppUpdate.PlaylistEntriesLoadError;
            }

            await DispatchMessageAsync(new PlaylistEntriesLoadedMessage(
                entries.PlaylistId, page.Page, page.Items, page.HasMore, page.TotalCount, entries.Append, error));
        }

        private async Task StartPlaylistPlaybackAsync(StartPlaylistPlaybackEffect start, CancellationToken cancellationToken)
        {
            PlaylistPlaybackStartInfo? info = null;
            string? error = null;
            try
            {
                info = await services.Playlists.StartPlaybackAsync(
                    start.PlaylistId, start.EntryId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playlist-Wiedergabestart fehlgeschlagen");
                error = AppUpdate.PlaybackUnavailableError;
            }

            await DispatchMessageAsync(new PlaylistPlaybackStartedMessage(start.PlaylistId, info, error));
        }

        private async Task NavigatePlaylistEntryAsync(
            long playlistId,
            long currentEntryId,
            bool forward,
            bool advance,
            CancellationToken cancellationToken)
        {
            PlaylistNavigationItem? result = null;
            string? error = null;
            try
            {
                var service = services.Playlists;
                result = advance
                    ? await service.AdvanceAsync(playlistId, currentEntryId, cancellationToken)
                    : forward
                        ? await service.GetNextEntryAsync(playlistId, currentEntryId, cancellationToken)
                        : await service.GetPreviousEntryAsync(playlistId, currentEntryId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Playlist-Navigation fehlgeschlagen");
                error = AppUpdate.PlaybackUnavailableError;
            }

            await DispatchMessageAsync(new PlaylistNavigationCompletedMessage(
                playlistId, currentEntryId, result, forward, error));
        }

        private async Task ProtectDownloadExpiryAsync(
            ProtectDownloadExpiryEffect protect, CancellationToken cancellationToken)
        {
            try
            {
                // Wiedergabe-Schutz: Ablaufdatum des lokal gespielten
                // Downloads hochsetzen (Zielwert/Faktor rechnet der
                // Dienst über seine Zeitquelle).
                await services.Downloads.ExtendExpiryAsync(
                    protect.MediaType, protect.MediaId,
                    protect.RemainingDuration,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ablaufdatum des Downloads hochsetzen fehlgeschlagen");
            }
        }

        private async Task ExpireWatchedDownloadAsync(
            ExpireWatchedDownloadEffect expireWatched, CancellationToken cancellationToken)
        {
            try
            {
                await services.Downloads.ExpireAfterAsync(
                    expireWatched.MediaType, expireWatched.MediaId,
                    expireWatched.Delay,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ablaufdatum des Downloads verkürzen fehlgeschlagen");
            }
        }

        // --- Downloads (Muster: ShellAppEffectHandler) ----------------

        private async Task LoadDownloadsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DownloadListItem> items = [];
            long usedBytes = 0;
            long orphanedBytes = 0;
            string? error = null;
            try
            {
                // Abgleich Ablageort ↔ Manifest inkl. Ablauf-Prüfung:
                // überholte Teildateien werden entfernt, „fertig" ohne
                // Datei wird auf den Fehler für fehlende Dateien
                // korrigiert (tvOS purgt App-Daten unter Speicherdruck),
                // Restdaten ohne Manifest-Bezug werden erkannt und
                // abgelaufene Downloads entfernt. Auto-Download-
                // Kandidaten gibt es auf tvOS nicht (kein Vorlauf).
                orphanedBytes = (await services.Downloads.ReconcileAsync(
                    settings, null, cancellationToken)).OrphanedBytes;
                items = await services.Downloads.LoadItemsAsync(cancellationToken);
                usedBytes = services.Downloads.GetUsedBytes();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download-Liste laden fehlgeschlagen");
                error = "Die Downloads konnten nicht geladen werden.";
            }

            await DispatchMessageAsync(new DownloadsLoadedMessage(
                items, usedBytes, settings.DownloadQuotaGb, error, orphanedBytes));
        }

        private async Task EnqueueDownloadAsync(EnqueueDownloadEffect enqueue, CancellationToken cancellationToken)
        {
            DownloadEnqueueResult result;
            try
            {
                var seriesContext = enqueue.ShowId is null && enqueue.SeasonNumber is null && enqueue.EpisodeNumber is null
                    ? null
                    : new DownloadSeriesContext(enqueue.ShowId, enqueue.ShowTitle, enqueue.SeasonNumber, enqueue.EpisodeNumber);
                result = await services.Downloads.EnqueueAsync(
                    new DownloadEnqueueRequest(
                        enqueue.MediaType, enqueue.MediaId, enqueue.Title,
                        SeriesContext: seriesContext,
                        PictureId: enqueue.PictureId,
                        Origin: DownloadOrigin.Manual),
                    enqueue.Settings,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download einreihen fehlgeschlagen");
                result = new DownloadEnqueueResult
                {
                    Item = null,
                    Notice = "Der Download konnte nicht eingeplant werden.",
                    IsError = true
                };
            }

            // Der Einplanungs-Eintrag mit dem Notice-Text obliegt
            // DownloadService (eine Schicht pro Ereignis) — hier nur
            // der Fehlerpfad. Quota-Hinweise kommen ebenfalls als
            // Notice aus dem Dienst.
            await DispatchMessageAsync(new DownloadEnqueueCompletedMessage(
                result.Item, result.Notice, result.IsError,
                MediaTypeNames.Normalize(enqueue.MediaType), enqueue.MediaId));
        }

        private async Task CancelDownloadAsync(CancelDownloadEffect cancel, CancellationToken cancellationToken)
        {
            try
            {
                await services.Downloads.CancelAsync(cancel.MediaType, cancel.MediaId, cancellationToken);
                // Der Abbruch-Eintrag (Zustand Cancelled) obliegt
                // DownloadService (eine Schicht pro Ereignis) — hier
                // nur der Fehlerpfad.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download-Abbruch fehlgeschlagen");
            }
        }

        private async Task DeleteDownloadAsync(DeleteDownloadEffect delete, CancellationToken cancellationToken)
        {
            try
            {
                // Das Entfernen wird über das ItemRemoved-Ereignis des
                // Dienstes als DownloadDeletedMessage in den MVU-Zyklus
                // gemeldet — keine Rückmeldung nötig.
                await services.Downloads.DeleteAsync(delete.MediaType, delete.MediaId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download löschen fehlgeschlagen");
            }
        }

        private async Task DeleteAllDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                await services.Downloads.DeleteAllAsync(cancellationToken);
                // Liste + Belegung neu laden (OrphanedBytes aktualisieren).
                await LoadDownloadsAsync(settings, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alle Downloads löschen fehlgeschlagen");
            }
        }

        private async Task PurgeOrphanedDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                await services.Downloads.PurgeOrphansAsync(cancellationToken);
                await LoadDownloadsAsync(settings, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Restdaten bereinigen fehlgeschlagen");
            }
        }

        private async Task DeleteFailedDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                await services.Downloads.DeleteFailedAsync(cancellationToken);
                // Liste + Belegung neu laden.
                await LoadDownloadsAsync(settings, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fehlerhafte Downloads löschen fehlgeschlagen");
            }
        }

        private async Task ResumeDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                // Abgleich Ablageort ↔ Manifest inkl. Ablauf-Prüfung vor
                // der Wiederaufnahme (gleicher Lauf wie beim Laden der
                // Liste); ein Fehler des Abgleichs unterbricht die
                // Wiederaufnahme nicht.
                await services.Downloads.ReconcileAsync(settings, null, cancellationToken);
                await services.Downloads.ResumeInterruptedAsync(settings, cancellationToken);
                // Der Wiederaufnahme-Eintrag mit der Anzahl obliegt
                // DownloadService (eine Schicht pro Ereignis) — hier
                // nur der Fehlerpfad.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download-Wiederaufnahme fehlgeschlagen");
            }
        }

        private async Task ReconcileDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            try
            {
                // Reiner Ablauf-/Abgleichslauf (App-Aktivierung ohne
                // Verbindung) — Fehler protokollieren, Zyklus nicht
                // unterbrechen.
                await services.Downloads.ReconcileAsync(settings, null, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Download-Abgleich fehlgeschlagen");
            }
        }

        private void OnDownloadItemChanged(object? sender, DownloadItemEventArgs e) =>
            _ = DispatchMessageAsync(new DownloadItemChangedMessage(e.Item));

        private void OnDownloadItemRemoved(object? sender, DownloadRemovedEventArgs e) =>
            _ = DispatchMessageAsync(new DownloadDeletedMessage(e.MediaType, e.MediaId));

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
