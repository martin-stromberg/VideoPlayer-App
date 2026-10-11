using System.Net;
using VideoPlayer.ApiClient;
using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell, Server-Erreichbarkeits-
/// prüfung über <see cref="IServerHealthProbe"/>, Ablage-Initialisierung und
/// Einstellungs-Persistenz über <see cref="VideoPlayer.Storage"/>, die
/// Serververwaltung (Laden, Discovery, Kandidaten-Prüfung, Speichern), das
/// Geräte-Pairing über <see cref="DevicePairingClient"/>/<see cref="DeviceTokenService"/>
/// sowie den Anmelde-Workflow über <see cref="AuthSessionService"/>.
/// </summary>
public sealed class ShellAppEffectHandler : IAppEffectHandler
{
    private readonly IServiceProvider services;
    private readonly IServerHealthProbe healthProbe;
    private readonly AuthSessionService authSession;
    private readonly DevicePairingClient pairing;
    private readonly DeviceTokenService deviceTokens;
    private readonly AppSecretsService secrets;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly ServerEventsService serverEvents;
    private readonly DownloadService downloads;
    private readonly AutoDownloadService autoDownloads;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ShellAppEffectHandler"/>-Klasse.</summary>
    /// <param name="services">Dienstanbieter zum Auflösen der am Effekt beteiligten Dienste (inkl. <see cref="MvuHost"/>).</param>
    /// <param name="healthProbe">Erreichbarkeitsprüfung für den Server.</param>
    /// <param name="authSession">Sitzungsdienst für den Anmelde-Workflow.</param>
    /// <param name="pairing">Öffentlicher Pairing-Endpunkt-Client (ohne Gate-/Bearer-Header).</param>
    /// <param name="deviceTokens">Verwaltung der gepaarten Geräte-Tokens (Provider, Widerruf, Gate-Probe).</param>
    /// <param name="secrets">Sichere Ablage (Geräte-Tokens je Server).</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung; meldet Wechsel in den MVU-Zyklus.</param>
    /// <param name="serverEvents">Server-Ereignisverbindung; meldet Ereignisse und Trennung in den MVU-Zyklus.</param>
    /// <param name="downloads">Download-Verwaltung; meldet Zustands-/Fortschrittsänderungen in den MVU-Zyklus.</param>
    /// <param name="autoDownloads">Automatischer Vorlauf der Weiterschauen-Liste.</param>
    public ShellAppEffectHandler(
        IServiceProvider services,
        IServerHealthProbe healthProbe,
        AuthSessionService authSession,
        DevicePairingClient pairing,
        DeviceTokenService deviceTokens,
        AppSecretsService secrets,
        INetworkStatusMonitor networkMonitor,
        ServerEventsService serverEvents,
        DownloadService downloads,
        AutoDownloadService autoDownloads)
    {
        this.services = services;
        this.healthProbe = healthProbe;
        this.authSession = authSession;
        this.pairing = pairing;
        this.deviceTokens = deviceTokens;
        this.secrets = secrets;
        this.networkMonitor = networkMonitor;
        this.serverEvents = serverEvents;
        this.downloads = downloads;
        this.autoDownloads = autoDownloads;

        // Endgültig abgelehnte stille Neuanmeldung (401 ohne gültige
        // Zugangsdaten) → die App wechselt in den Anmeldezustand.
        this.authSession.SessionExpired += OnSessionExpired;

        // Serverseitig verworfenes Geräte-Token (Gate-Ablehnung) →
        // die App wechselt in die Re-Pairing-Eingabe.
        this.deviceTokens.DeviceTokenRejected += OnDeviceTokenRejected;

        // Netzwerkwechsel und Server-Ereignisse laufen auf beliebigen
        // Threads — die Messages gehen über den MainThread in den
        // MVU-Zyklus.
        this.networkMonitor.AvailabilityChanged += OnNetworkAvailabilityChanged;
        this.serverEvents.EventReceived += OnServerEventReceived;
        this.serverEvents.ConnectionClosed += OnServerEventsConnectionClosed;

        // Download-Transfers laufen auf Hintergrundthreads — Zustands-/
        // Fortschrittsmeldungen und Entfernungen gehen über den
        // MainThread in den MVU-Zyklus.
        this.downloads.ItemChanged += OnDownloadItemChanged;
        this.downloads.ItemRemoved += OnDownloadItemRemoved;
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
                await MainThread.InvokeOnMainThreadAsync(() => NavigateAsync(navigate.Page));
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
                await RunPartialSyncAsync(s => s.SyncContinueWatchingAsync(cancellationToken));
                break;
            case HideContinueWatchingEntryEffect hide:
                await MutateContinueWatchingAsync(s => s.HideAsync(hide.MediaType, hide.MediaId, cancellationToken));
                break;
            case SkipContinueWatchingEntryEffect skip:
                await MutateContinueWatchingAsync(s => s.SkipAsync(skip.MediaType, skip.MediaId, cancellationToken));
                break;
            case SyncFavoritesEffect:
                await RunPartialSyncAsync(s => s.SyncFavoritesAsync(cancellationToken));
                break;
            case SyncMetadataEffect:
                await RunPartialSyncAsync(s => s.SyncMetadataAsync(cancellationToken));
                break;
            case LoadHomeListsEffect:
                await LoadHomeListsAsync();
                break;
            case LoadHeroBackgroundEffect:
                await LoadHeroBackgroundAsync(cancellationToken);
                break;
            case NavigateToMediaDetailsEffect details:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateToMediaDetailsAsync(details));
                break;
            case LoadSourcesEffect:
                await LoadSourcesAsync(cancellationToken);
                break;
            case NavigateToSourceOverviewEffect overview:
                await MainThread.InvokeOnMainThreadAsync(() => NavigateToSourceOverviewAsync(overview));
                break;
            case LoadSourceGenresEffect genres:
                await LoadSourceGenresAsync(genres, cancellationToken);
                break;
            case LoadSourceItemsEffect items:
                await LoadSourceItemsAsync(items, cancellationToken);
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
            case LoadFavoritesPageEffect loadFavorites:
                await LoadFavoritesPageAsync(loadFavorites.RefreshFromServer, cancellationToken);
                break;
            case RemoveFavoriteEffect remove:
                await RemoveFavoriteAsync(remove.FavoriteId, cancellationToken);
                break;
            case LoadProfileInfoEffect:
                await LoadProfileInfoAsync(cancellationToken);
                break;
            case NavigateToPlayerEffect:
                await MainThread.InvokeOnMainThreadAsync(NavigateToPlayerAsync);
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
            case TogglePictureInPictureEffect:
                await TogglePictureInPictureAsync();
                break;
            case LogoutEffect:
                await LogoutAsync();
                break;
            case LoadDownloadsEffect load:
                await LoadDownloadsAsync(load.Settings);
                break;
            case EnqueueDownloadEffect enqueue:
                await EnqueueDownloadAsync(enqueue, cancellationToken);
                break;
            case CancelDownloadEffect cancel:
                await downloads.CancelAsync(cancel.MediaType, cancel.MediaId, cancellationToken);
                break;
            case DeleteDownloadEffect delete:
                await DeleteDownloadAsync(delete.MediaType, delete.MediaId, cancellationToken);
                break;
            case ResumeDownloadsEffect resume:
                await ResumeDownloadsAsync(resume.Settings, cancellationToken);
                break;
            case RunAutoDownloadsEffect autoRun:
                await autoDownloads.RunAsync(autoRun.Settings, autoRun.Force, cancellationToken);
                break;
            case DeleteWatchedDownloadEffect deleteWatched:
                await DeleteDownloadAsync(deleteWatched.MediaType, deleteWatched.MediaId, cancellationToken, completedOnly: true);
                break;
        }
    }

    private async Task NavigateToPlayerAsync()
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("player");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Player-Navigation fehlgeschlagen: {ex.Message}");
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
            // fehlschlagen.
            var allowServerStream = !services.GetRequiredService<MvuHost>().Model.IsOffline;
            source = await services.GetRequiredService<PlaybackSourceService>()
                .ResolveAsync(resolve.MediaType, resolve.MediaId, cancellationToken,
                    allowServerStream: allowServerStream);
            if (source is null)
            {
                error = AppUpdate.PlaybackUnavailableError;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Quellauflösung fehlgeschlagen: {ex.Message}");
            error = AppUpdate.PlaybackUnavailableError;
        }

        await DispatchMessageAsync(new PlaybackSourceResolvedMessage(
            resolve.MediaType, resolve.MediaId, source, error));
    }

    /// <summary>
    /// Serienkontext einer direkt gestarteten Episode laden (ohne
    /// Detailseite) und die Folge-Episoden in den MVU-Zyklus melden —
    /// ein Fehlschlag ergibt eine leere Fortsetzungsliste statt eines
    /// Fehlerzustands (die Folgevideo-Funktion entfällt dann still).
    /// </summary>
    /// <param name="loadUpcoming">Der Effekt mit Episoden- und Serien-Id.</param>
    /// <param name="cancellationToken">Bricht den Ladevorgang ab.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach dem Melden der Fortsetzungsliste.</returns>
    private async Task LoadUpcomingEpisodesAsync(LoadUpcomingEpisodesEffect loadUpcoming, CancellationToken cancellationToken)
    {
        IReadOnlyList<PlayerEpisodeRef> episodes = [];
        try
        {
            episodes = await services.GetRequiredService<UpcomingEpisodesService>()
                .LoadAsync(loadUpcoming.MediaId, loadUpcoming.ShowId, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Folge-Episoden laden fehlgeschlagen: {ex.Message}");
        }

        await DispatchMessageAsync(new UpcomingEpisodesLoadedMessage(loadUpcoming.MediaId, episodes));
    }

    private async Task ReportPlaybackProgressAsync(ReportPlaybackProgressEffect report, CancellationToken cancellationToken)
    {
        try
        {
            await services.GetRequiredService<ProgressReportService>()
                .ReportProgressAsync(
                    report.MediaType, (int)report.MediaId,
                    report.Position, report.Duration, cancellationToken);
        }
        catch (Exception ex)
        {
            // Ein Fehlschlag ist nicht fatal — der nächste Takt meldet erneut.
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Fortschrittsmeldung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task TogglePictureInPictureAsync()
    {
        var isActive = false;
        try
        {
            isActive = await MainThread.InvokeOnMainThreadAsync(
                () => services.GetRequiredService<IPictureInPictureService>().ToggleAsync());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] PiP-Umschaltung fehlgeschlagen: {ex.Message}");
        }

        await DispatchMessageAsync(new PictureInPictureChangedMessage(isActive));
    }

    private async Task StartNetworkMonitoringAsync()
    {
        try
        {
            networkMonitor.Start();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Netzwerk-Monitoring fehlgeschlagen: {ex.Message}");
        }

        // Ist-Wert in den MVU-Zyklus melden (ConnectivityChanged meldet
        // nur Wechsel — der Startzustand ginge sonst verloren).
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
            // Ein Fehlschlag ist nicht fatal — der nächste Netzwerkwechsel
            // oder das nächste Ereignis bietet eine neue Gelegenheit.
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Ereignisverbindung fehlgeschlagen: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Trennen der Ereignisverbindung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task RunFullSyncAsync(CancellationToken cancellationToken)
    {
        SyncResult result;
        try
        {
            result = await services.GetRequiredService<ServerSyncService>().SynchronizeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Synchronisation fehlgeschlagen: {ex.Message}");
            result = new SyncResult(0, 0, false, false, false);
        }

        await DispatchMessageAsync(new SynchronizationCompletedMessage(result));
    }

    private async Task RunPartialSyncAsync(Func<ServerSyncService, Task> sync)
    {
        try
        {
            await sync(services.GetRequiredService<ServerSyncService>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Teil-Synchronisation fehlgeschlagen: {ex.Message}");
        }

        // Alle Teil-Synchronisationen betreffen Startseiten-Listen — die
        // Anzeige lädt danach den (ggf. unveränderten) Cache-Stand neu.
        await DispatchMessageAsync(new CachedListsChangedMessage());
    }

    /// <summary>
    /// Weiterschauen-Kontextmenü-Mutation (Ausblenden/Überspringen):
    /// führt den Server-Aufruf samt Cache-Erneuerung bzw. das lokale
    /// Offline-Entfernen aus und meldet bei Erfolg die Cache-Änderung —
    /// die Startseite lädt den neuen Stand ohne manuelle Aktualisierung
    /// nach. Ein Fehlschlag lässt die Liste unverändert (der Eintrag
    /// bleibt sichtbar).
    /// </summary>
    /// <param name="mutate">Die Mutation auf dem aufgelösten <see cref="ContinueWatchingMutationService"/>.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Mutation.</returns>
    private async Task MutateContinueWatchingAsync(Func<ContinueWatchingMutationService, Task<bool>> mutate)
    {
        var success = false;
        try
        {
            success = await mutate(services.GetRequiredService<ContinueWatchingMutationService>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Weiterschauen-Mutation fehlgeschlagen: {ex.Message}");
        }

        if (success)
        {
            // Die Cache-Kopie wurde aktualisiert (Server-Sync bzw.
            // lokales Entfernen) — die Weiterschauen-Liste lädt den
            // neuen Stand ohne manuelle Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task LoadHomeListsAsync()
    {
        HomeLists lists;
        try
        {
            // Offline bildet „Weiterschauen" aus den lokalen Downloads
            // (mit Platzhaltern fehlender Folge-Episoden); online kommt
            // die Liste wie bisher aus dem synchronisierten Cache-Stand.
            lists = services.GetRequiredService<MvuHost>().Model.IsOffline
                ? await services.GetRequiredService<OfflineContinueWatchingService>().LoadHomeListsAsync()
                : await services.GetRequiredService<HomeListService>().LoadHomeListsAsync();
        }
        catch (Exception ex)
        {
            // Fehler beim Cache-Lesen → leere Listen, die Startseite bleibt
            // anzeigbar.
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Startseiten-Listen laden fehlgeschlagen: {ex.Message}");
            lists = HomeLists.Empty;
        }

        await DispatchMessageAsync(new HomeListsLoadedMessage(lists));
    }

    private async Task LoadHeroBackgroundAsync(CancellationToken cancellationToken)
    {
        byte[]? imageBytes = null;
        try
        {
            // Der angemeldete Client liefert das servergenerierte Bild
            // (Bearer + stille Token-Erneuerung in der Sende-Pipeline).
            // Ohne Sitzung bzw. bei Fehlschlag bleibt das Bild aus — der
            // Kopfbereich zeigt dann nur Gradienten und Texte.
            var client = authSession.Client;
            if (client is not null)
            {
                var bytes = await client.GetHeroBackgroundAsync(cancellationToken);
                imageBytes = bytes.Length > 0 ? bytes : null;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Hero-Hintergrund laden fehlgeschlagen: {ex.Message}");
        }

        await DispatchMessageAsync(new HeroBackgroundLoadedMessage(imageBytes));
    }

    private async Task NavigateToMediaDetailsAsync(NavigateToMediaDetailsEffect details)
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            var route = $"mediadetails?mediaType={Uri.EscapeDataString(details.MediaType)}" +
                $"&id={details.MediaId}" +
                $"&title={Uri.EscapeDataString(details.Title ?? string.Empty)}";
            // Serienkontext-Hints (Episoden-/Staffel-Einträge) nur anhängen,
            // wenn belegt — sie lösen die Serienansicht mit Vorwahl auf.
            if (details.ShowId is > 0)
            {
                route += $"&show={details.ShowId}";
            }
            if (details.SeasonId is > 0)
            {
                route += $"&season={details.SeasonId}";
            }
            if (details.EpisodeId is > 0)
            {
                route += $"&episode={details.EpisodeId}";
            }
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Detail-Navigation fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task NavigateToSourceOverviewAsync(NavigateToSourceOverviewEffect overview)
    {
        if (Shell.Current is null)
        {
            return;
        }

        try
        {
            if (IsTopmostSourceOverview(overview.SourceId))
            {
                // Die Übersicht dieser Quelle liegt bereits oben auf dem
                // Stack — ein erneuter Push (Tap auf den gerade offenen
                // Menüeintrag) würde eine identische Seite schieben; die
                // Nachlade-Effekte der Auswahl laufen dennoch und
                // frischen den Stand der geöffneten Seite auf.
                return;
            }

            var route = $"sourceoverview?id={overview.SourceId}" +
                $"&name={Uri.EscapeDataString(overview.SourceName ?? string.Empty)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Quellen-Navigation fehlgeschlagen: {ex.Message}");
        }
    }

    /// <summary>Prüft, ob die oberste Shell-Route bereits die Quellenübersicht der angegebenen Quelle ist.</summary>
    /// <param name="sourceId">Die Quellen-Id des anzusteuernden Ziels.</param>
    /// <returns><c>true</c>, wenn oben bereits „sourceoverview" mit derselben <c>id</c> liegt.</returns>
    private static bool IsTopmostSourceOverview(long sourceId)
    {
        var location = Shell.Current?.CurrentState?.Location?.OriginalString;
        if (string.IsNullOrEmpty(location))
        {
            return false;
        }

        // Die Location trägt den ganzen Stack (z. B.
        // „//home/sourceoverview?id=5") — nur das letzte Pfadsegment
        // entscheidet, welche Seite oben liegt.
        var queryIndex = location.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex >= 0 ? location[..queryIndex] : location;
        var topmost = path.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (!string.Equals(topmost, "sourceoverview", StringComparison.OrdinalIgnoreCase)
            || queryIndex < 0)
        {
            return false;
        }

        foreach (var pair in location[(queryIndex + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2
                && string.Equals(parts[0], "id", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(Uri.UnescapeDataString(parts[1]), out var id)
                    && id == sourceId;
            }
        }

        return false;
    }

    private async Task LoadSourcesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<SourceItem> sources = [];
        string? error = null;
        try
        {
            sources = await services.GetRequiredService<SourceBrowserService>()
                .LoadSourcesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Quellen laden fehlgeschlagen: {ex.Message}");
            error = "Die Quellen konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new SourcesLoadedMessage(sources, error));
    }

    private async Task LoadSourceGenresAsync(LoadSourceGenresEffect genres, CancellationToken cancellationToken)
    {
        IReadOnlyList<SourceGenreItem> items;
        try
        {
            items = await services.GetRequiredService<SourceBrowserService>()
                .LoadGenresAsync(genres.SourceId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Genre-Ladefehler sind nicht fatal — die Leiste zeigt dann
            // nur „Alle" (kein Filter).
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Genres laden fehlgeschlagen: {ex.Message}");
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
            page = await services.GetRequiredService<SourceBrowserService>()
                .LoadItemsAsync(items.SourceId, items.Page, items.SearchText, items.GenreId, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Quelleneinträge laden fehlgeschlagen: {ex.Message}");
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
            details = await services.GetRequiredService<MediaDetailsService>()
                .LoadAsync(
                    loadDetails.MediaType, loadDetails.MediaId, loadDetails.Title,
                    loadDetails.ShowId, loadDetails.SeasonId, loadDetails.EpisodeId,
                    cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Detail-Ladung fehlgeschlagen: {ex.Message}");
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
            // Episoden-Still-Endpunkt bzw. aufgelöste Bildquelle des
            // Web-Layouts (Bearer + stille Token-Erneuerung in der
            // Sende-Pipeline). Ohne Sitzung bzw. bei Fehlschlag bleibt
            // das Bild aus — der Kopfbereich zeigt dann nur Gradienten
            // und Texte.
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Detail-Kopfbild laden fehlgeschlagen: {ex.Message}");
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
            isFavorite = await services.GetRequiredService<FavoritesService>()
                .ToggleAsync(toggle.MediaType, toggle.MediaId, toggle.Title, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Favoriten-Umschaltung fehlgeschlagen: {ex.Message}");
        }

        await DispatchMessageAsync(new FavoriteToggleCompletedMessage(
            isFavorite, toggle.MediaType, toggle.MediaId));

        if (isFavorite is not null)
        {
            // Die lokale Favoriten-Kopie wurde aktualisiert — die
            // Startseiten-Liste lädt den neuen Stand ohne manuelle
            // Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task LoadFavoritesPageAsync(bool refreshFromServer, CancellationToken cancellationToken)
    {
        IReadOnlyList<FavoriteListItem> items = [];
        string? error = null;
        try
        {
            // Online ist der Server führend: erst die lokale Kopie vom
            // Serverstand erneuern, dann den Cache lesen. Schlägt der
            // Sync fehl, bleibt der bisherige Cache-Stand anzeigbar.
            // Offline wird direkt der Cache-Stand geliefert — ohne
            // vergeblichen Server-Aufruf.
            if (refreshFromServer && authSession.Client is not null)
            {
                await services.GetRequiredService<ServerSyncService>()
                    .SyncFavoritesAsync(cancellationToken);
            }

            items = await services.GetRequiredService<HomeListService>()
                .LoadFavoriteItemsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Favoriten laden fehlgeschlagen: {ex.Message}");
            error = "Die Favoriten konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new FavoritesPageLoadedMessage(items, error));
    }

    private async Task RemoveFavoriteAsync(long favoriteId, CancellationToken cancellationToken)
    {
        var success = false;
        try
        {
            success = await services.GetRequiredService<FavoritesService>()
                .RemoveFavoriteAsync(favoriteId, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Favorit entfernen fehlgeschlagen: {ex.Message}");
        }

        await DispatchMessageAsync(new FavoriteRemoveCompletedMessage(favoriteId, success));

        if (success)
        {
            // Die lokale Favoriten-Kopie wurde aktualisiert — die
            // Startseiten-Favoritenliste lädt den neuen Stand ohne
            // manuelle Aktualisierung nach.
            await DispatchMessageAsync(new CachedListsChangedMessage());
        }
    }

    private async Task LoadProfileInfoAsync(CancellationToken cancellationToken)
    {
        ProfileInfo info;
        try
        {
            info = await services.GetRequiredService<ProfileInfoService>()
                .LoadAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Ohne lesbare Ablage bleibt die Seite mit Platzhaltern
            // anzeigbar (Benutzername/E-Mail „—").
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Profilinformationen laden fehlgeschlagen: {ex.Message}");
            info = new ProfileInfo(null, null, null);
        }

        await DispatchMessageAsync(new ProfileInfoLoadedMessage(info));
    }

    private async Task LogoutAsync()
    {
        try
        {
            await authSession.LogoutAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Abmeldung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task LoadDownloadsAsync(AppSettings settings)
    {
        IReadOnlyList<DownloadListItem> items = [];
        long usedBytes = 0;
        string? error = null;
        try
        {
            items = await downloads.LoadItemsAsync();
            usedBytes = downloads.GetUsedBytes();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Download-Liste laden fehlgeschlagen: {ex.Message}");
            error = "Die Downloads konnten nicht geladen werden.";
        }

        await DispatchMessageAsync(new DownloadsLoadedMessage(items, usedBytes, settings.DownloadQuotaGb, error));
    }

    private async Task EnqueueDownloadAsync(EnqueueDownloadEffect enqueue, CancellationToken cancellationToken)
    {
        DownloadEnqueueResult result;
        try
        {
            var seriesContext = enqueue.ShowId is null && enqueue.SeasonNumber is null && enqueue.EpisodeNumber is null
                ? null
                : new DownloadSeriesContext(enqueue.ShowId, enqueue.ShowTitle, enqueue.SeasonNumber, enqueue.EpisodeNumber);
            result = await downloads.EnqueueAsync(
                enqueue.MediaType, enqueue.MediaId, enqueue.Title, enqueue.Settings, seriesContext, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Download einreihen fehlgeschlagen: {ex.Message}");
            result = new DownloadEnqueueResult
            {
                Item = null,
                Notice = "Der Download konnte nicht eingeplant werden.",
                IsError = true
            };
        }

        await DispatchMessageAsync(new DownloadEnqueueCompletedMessage(
            result.Item, result.Notice, result.IsError,
            MediaTypeNames.Normalize(enqueue.MediaType), enqueue.MediaId));
    }

    private async Task DeleteDownloadAsync(
        string mediaType,
        long mediaId,
        CancellationToken cancellationToken,
        bool completedOnly = false)
    {
        try
        {
            // Das Entfernen wird über das ItemRemoved-Ereignis des
            // Dienstes als DownloadDeletedMessage in den MVU-Zyklus
            // gemeldet — keine Rückmeldung nötig.
            if (completedOnly)
            {
                await downloads.DeleteIfCompletedAsync(mediaType, mediaId, cancellationToken);
            }
            else
            {
                await downloads.DeleteAsync(mediaType, mediaId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Download löschen fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task ResumeDownloadsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await downloads.ResumeInterruptedAsync(settings, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Download-Wiederaufnahme fehlgeschlagen: {ex.Message}");
        }
    }

    private void OnDownloadItemChanged(object? sender, DownloadItemEventArgs e) =>
        _ = DispatchMessageAsync(new DownloadItemChangedMessage(e.Item));

    private void OnDownloadItemRemoved(object? sender, DownloadRemovedEventArgs e) =>
        _ = DispatchMessageAsync(new DownloadDeletedMessage(e.MediaType, e.MediaId));

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
            var host = services.GetRequiredService<MvuHost>();
            await MainThread.InvokeOnMainThreadAsync(() => host.DispatchAsync(message));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Dispatch von {message.GetType().Name} fehlgeschlagen: {ex.Message}");
        }
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Automatische Anmeldung fehlgeschlagen: {ex.Message}");
            attempt = new AuthAttempt(AuthAttemptStatus.Failed);
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new AutoLoginCompletedMessage(attempt.Status, attempt.Username)));
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Anmeldung fehlgeschlagen: {ex.Message}");
            attempt = new AuthAttempt(AuthAttemptStatus.Failed);
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new LoginCompletedMessage(attempt.Status)));
    }

    private async Task CheckOfflineDataAsync()
    {
        // Jeder Fehler der Prüfung gilt als „keine Offline-Daten" — die App
        // zeigt dann die Serverauswahl statt im Ladezustand zu verharren.
        var hasOfflineData = false;
        try
        {
            hasOfflineData = await services.GetRequiredService<CacheDatabase>().HasOfflineDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Offline-Daten-Prüfung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new OfflineDataCheckCompletedMessage(hasOfflineData)));
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        // Der API-Client läuft auf beliebigen Threads — die Message muss über
        // den MainThread in den MVU-Zyklus.
        _ = DispatchSessionExpiredAsync();
    }

    private async Task DispatchSessionExpiredAsync()
    {
        try
        {
            var host = services.GetRequiredService<MvuHost>();
            await MainThread.InvokeOnMainThreadAsync(
                () => host.DispatchAsync(new SessionExpiredMessage()));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] SessionExpired-Dispatch fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task InitializeStorageAsync()
    {
        // Fehler der Ablage-Initialisierung dürfen den Start nicht blockieren —
        // die App läuft mit Standardwerten weiter. Beschädigte Datenbanken
        // werden neu angelegt (alte Datei als *.corrupt-* beiseitegelegt)
        // und der Nutzer darüber informiert.
        var recovered = false;
        var failed = false;
        try
        {
            // Die Auflösung des Layouts legt die Verzeichnisse an und wendet den
            // plattformseitigen Backup-Ausschluss an (AppStorageLayoutProvider).
            _ = services.GetRequiredService<AppStorageLayout>();
            recovered |= await services.GetRequiredService<BackupDatabase>().InitializeWithRecoveryAsync();
        }
        catch (Exception ex)
        {
            failed = true;
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Backup-Ablage-Initialisierung fehlgeschlagen: {ex.Message}");
        }

        try
        {
            recovered |= await services.GetRequiredService<CacheDatabase>().InitializeWithRecoveryAsync();
        }
        catch (Exception ex)
        {
            failed = true;
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Cache-Ablage-Initialisierung fehlgeschlagen: {ex.Message}");
        }

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
            settings = services.GetRequiredService<AppSettingsService>().Load();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Einstellungen laden fehlgeschlagen: {ex.Message}");
            settings = new AppSettings();
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new SettingsLoadedMessage(settings)));
    }

    private void SaveSettings(AppSettings settings)
    {
        try
        {
            services.GetRequiredService<AppSettingsService>().Save(settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Einstellungen speichern fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task LoadServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<KnownServer> servers = [];
        string? activeAddress = null;
        try
        {
            var knownServers = services.GetRequiredService<KnownServersService>();
            await knownServers.MigrateLegacyServerAddressAsync();
            servers = await knownServers.GetAllAsync();

            var configured = await services
                .GetRequiredService<IServerAddressProvider>()
                .GetConfiguredServerAddressAsync(cancellationToken);
            activeAddress = configured?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Serververwaltung laden fehlgeschlagen: {ex.Message}");
        }

        // Geräte-Token des aktiven Servers laden und den Gate-Provider
        // auf dessen Server-Kontext einstellen; ohne Token fällt die
        // Auflösung auf die statischen Quellen zurück.
        await deviceTokens.LoadForServerAsync(activeAddress, cancellationToken);
        var pairingRequired = await IsPairingRequiredAsync(activeAddress);

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServersLoadedMessage(servers, activeAddress, pairingRequired)));
    }

    private async Task DiscoverServersAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DiscoveredServer> found = [];
        try
        {
            found = await services
                .GetRequiredService<ServerDiscoveryService>()
                .DiscoverAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Server-Erkennung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerDiscoveryCompletedMessage(found)));
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
                // Das Gate hat geantwortet — der Server ist erreichbar.
                // Die Probe löst den Gate-Schlüssel adressspezifisch auf:
                // ein 401 beweist die Ungültigkeit des *hinterlegten*
                // Geräte-Tokens dieses Kandidaten — es wird gezielt
                // gelöscht, auch wenn der Server nicht aktiv ist (Muster:
                // AuthSessionService.HandleGateRejectionAsync). Sonst
                // würde IsPairingRequiredAsync fälschlich „kein Pairing
                // nötig" melden und ein aussichtsloser Auto-Login starten.
                try
                {
                    await secrets.ClearDeviceTokenAsync(address);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Geräte-Token des Kandidaten konnte nicht verworfen werden: {ex.Message}");
                }

                // Provider-Rücksetzung nur, wenn der Kandidat der
                // aktuell bediente Server ist.
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Kandidatenprüfung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerCheckCompletedMessage(address, displayName, success, pairingRequired)));
    }

    private async Task PersistActiveServerAsync(string address, string? displayName)
    {
        try
        {
            await services.GetRequiredService<KnownServersService>()
                .SetActiveServerAsync(address, displayName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Server speichern fehlgeschlagen: {ex.Message}");
        }

        // Provider auf das Geräte-Token des neuen Servers umstellen, bevor
        // der nachfolgende Auto-Login-Effekt läuft (Effekte laufen
        // sequenziell).
        await deviceTokens.LoadForServerAsync(address);
    }

    private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
    {
        // Jeder Fehler der Prüfung führt zu „nicht erreichbar" — die App darf
        // nicht auf der Ladeanzeige hängen bleiben.
        var result = HealthCheckResult.Unreachable;
        try
        {
            result = await healthProbe.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Erreichbarkeitsprüfung fehlgeschlagen: {ex.Message}");
        }

        if (result == HealthCheckResult.GateRejected)
        {
            // Das bereitgestellte Geräte-Token wurde verworfen — zuerst
            // entfernen und auf die statische Auflösung zurückfallen,
            // dann einmalig erneut prüfen: ein funktionierender
            // statischer Schlüssel wird sonst vom veralteten Token
            // verdeckt (nahtlose Migration).
            await deviceTokens.DiscardCurrentAsync();
            try
            {
                result = await healthProbe.CheckAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Erreichbarkeits-Nachprüfung fehlgeschlagen: {ex.Message}");
                result = HealthCheckResult.Unreachable;
            }

            if (result == HealthCheckResult.GateRejected)
            {
                // Es gibt keinen nutzbaren Schlüssel — Re-Pairing
                // einfordern (Ereignis → DeviceTokenRevokedMessage).
                deviceTokens.NotifyRejected();
                return;
            }
        }

        var pairingRequired = result == HealthCheckResult.Reachable
            && await IsPairingRequiredAsync(services.GetRequiredService<MvuHost>().Model.ActiveServerAddress);

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerHealthCheckCompletedMessage(
                result == HealthCheckResult.Reachable, pairingRequired)));
    }

    /// <summary>
    /// Ermittelt, ob für den Server ein Geräte-Pairing erforderlich ist:
    /// kein hinterlegtes Geräte-Token UND keine statische Gate-Quelle
    /// konfiguriert (<see cref="ClientGate.ResolveStatic"/> liefert den
    /// Platzhalter).
    /// </summary>
    /// <param name="serverAddress">Die zu prüfende Serveradresse.</param>
    /// <returns><c>true</c>, wenn ein Pairing eingefordert werden muss.</returns>
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Pairing-Erforderlichkeit fehlgeschlagen: {ex.Message}");
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

            // Provider-/Client-Aktivierung nur, wenn der Pairing-Kontext
            // noch aktuell ist — dieselbe Bedingung wie der Stale-Guard in
            // UpdatePairingCompleted (IsPairingRunning + aktiver Server):
            // ein zwischenzeitlich gestarteter Servercheck (ConnectButton/
            // Serverliste bleiben aktiv) hat den Provider über
            // PersistActiveServerAsync → LoadForServerAsync bereits auf den
            // neuen Server umgestellt — ein verspätetes Ergebnis des alten
            // Servers darf ihn nicht zurücküberschreiben (der Stale-Guard
            // verwirft nur die Nachricht, nicht die Seiteneffekte).
            var model = services.GetRequiredService<MvuHost>().Model;
            var pairingCurrent = model.IsPairingRunning
                && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
            if (pairingCurrent)
            {
                // Atomarer Stale-Guard: persistiert + aktiviert das Token
                // nur, wenn die Adresse noch der aktuell bediente Server
                // ist (unter Lock geprüft — ein paralleler Serverwechsel
                // über LoadForServerAsync kann nicht dazwischenfunken);
                // andernfalls bleibt das Token nur unter seiner Adresse
                // hinterlegt. Bei Aktivierung erhält der gehaltene
                // API-Client den Schlüssel direkt.
                var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                    serverAddress, token, cancellationToken);
                if (activated)
                {
                    authSession.Client?.SetApiKey(token);
                }
            }
            else
            {
                // Veralteter Kontext: das gültige Token bleibt unter seiner
                // eigenen Serveradresse hinterlegt (ein späteres Pairing zu
                // diesem Server entfällt), ohne den aktiven Provider oder
                // den gehaltenen API-Client anzufassen.
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Pairing-Austausch fehlgeschlagen: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Ungültige/nicht entschlüsselbare Antwort oder sonstiger
            // Fehler → allgemeiner Pairing-Fehler.
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Pairing-Austausch fehlgeschlagen: {ex.Message}");
            status = PairingStatus.Failed;
        }

        await DispatchMessageAsync(new PairingCompletedMessage(status));
    }

    /// <summary>
    /// Prüft die Erreichbarkeit des Servers aus einer Ticket-Adresse
    /// (QR-/Ticket-Onboarding): erst bei Erfolg übernimmt das Update den
    /// Server und startet den Bootstrap-Austausch. Eine Gate-Ablehnung
    /// zählt als „erreichbar" — der Server hat geantwortet; das Bootstrap
    /// liefert ohnehin ein frisches Geräte-Token.
    /// </summary>
    /// <param name="address">Adresse aus der Ticket-Adresse.</param>
    /// <param name="Ticket">Das einzulösende Ticket (wird an die Ergebnis-Nachricht weitergereicht).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    private async Task CheckBootstrapServerAsync(string address, string Ticket, CancellationToken cancellationToken)
    {
        var success = false;
        try
        {
            var result = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
            success = result != HealthCheckResult.Unreachable;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Bootstrap-Serverprüfung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new BootstrapServerCheckedMessage(address, success, Ticket)));
    }

    /// <summary>
    /// Löst das Ticket gegen das verschlüsselte Session-Paket ein
    /// (<c>api/pairing/bootstrap</c>) und persistiert Geräte-Token,
    /// Anmelde-Token und Refresh-Token — nur wenn der Bootstrap-Kontext
    /// noch aktuell ist (Staler-Guard wie <see cref="ExchangePairingCodeAsync"/>).
    /// </summary>
    /// <param name="serverAddress">Adresse des Bootstrap-Servers.</param>
    /// <param name="ticket">Das einzulösende Ticket bzw. der Kurzcode.</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    private async Task ExchangeBootstrapTicketAsync(string serverAddress, string ticket, CancellationToken cancellationToken)
    {
        BootstrapStatus status;
        try
        {
            var payload = await pairing.BootstrapAsync(
                new Uri(serverAddress), ticket, GetDeviceName(), cancellationToken);

            // Provider-/Session-Aktivierung nur, wenn der Bootstrap-Kontext
            // noch aktuell ist — ein zwischenzeitlich gestarteter
            // Servercheck hat den Provider bereits umgestellt; die Session
            // (JWT + Refresh-Token) gehört zudem zum Server des Tickets und
            // darf bei veraltetem Kontext nicht global hinterlegt werden.
            var model = services.GetRequiredService<MvuHost>().Model;
            var bootstrapCurrent = model.IsBootstrapRunning
                && ServerAddress.TryNormalize(model.ActiveServerAddress, out var activeAddress)
                && ServerAddress.TryNormalize(serverAddress, out var pairedAddress)
                && string.Equals(activeAddress, pairedAddress, StringComparison.OrdinalIgnoreCase);
            if (bootstrapCurrent)
            {
                // Atomarer Stale-Guard: persistiert + aktiviert das
                // Geräte-Token nur, wenn die Adresse noch der aktuell
                // bediente Server ist (Muster: ExchangePairingCodeAsync).
                var activated = await deviceTokens.SetPairedTokenIfCurrentAsync(
                    serverAddress, payload.DeviceToken, cancellationToken);
                if (activated)
                {
                    authSession.Client?.SetApiKey(payload.DeviceToken);
                }

                // Sitzung des Bootstrap-Pakets persistieren — die
                // anschließende automatische Anmeldung validiert das
                // Token und führt direkt zur Startseite.
                await secrets.SetAuthTokenAsync(payload.Token);
                await secrets.SetRefreshTokenAsync(payload.RefreshToken);
            }
            else
            {
                // Veralteter Kontext: das Geräte-Token bleibt unter seiner
                // eigenen Adresse hinterlegt; die Session wird verworfen,
                // weil sie dem Ticket-Server gehört, nicht dem aktiven.
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
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Bootstrap-Austausch fehlgeschlagen: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Ungültige/nicht entschlüsselbare Antwort oder sonstiger
            // Fehler → allgemeiner Bootstrap-Fehler.
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Bootstrap-Austausch fehlgeschlagen: {ex.Message}");
            status = BootstrapStatus.Failed;
        }

        await DispatchMessageAsync(new BootstrapCompletedMessage(status));
    }

    /// <summary>
    /// Anzeigename des Geräts für das Pairing (<see cref="DeviceInfo.Name"/>,
    /// Fallback Hersteller + Modell; <c>null</c>, wenn nichts ermittelbar —
    /// der Server nutzt dann seinen Fallback). Auf 200 Zeichen gekürzt.
    /// </summary>
    /// <returns>Der Gerätename oder <c>null</c>.</returns>
    private static string? GetDeviceName()
    {
        var name = DeviceInfo.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}".Trim();
        }

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length > 200 ? trimmed[..200] : trimmed;
    }

    private void OnDeviceTokenRejected(object? sender, EventArgs e) =>
        _ = DispatchMessageAsync(new DeviceTokenRevokedMessage());

    private async Task NavigateAsync(AppPage page)
    {
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window is null)
        {
            return;
        }

        // Die Server-Einrichtung/-verwaltung hat keinen Menüeintrag: beim
        // Erststart wird sie als Window-Root gesetzt (die Shell existiert noch
        // nicht), aus den Einstellungen heraus als geschobene Shell-Route.
        if (page == AppPage.ServerSetup)
        {
            if (window.Page is AppShell && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("serversetup");
            }
            else if (window.Page is not Pages.ServerSetupPage)
            {
                window.Page = services.GetRequiredService<Pages.ServerSetupPage>();
            }

            return;
        }

        // Die Anmeldemaske hat keinen Menüeintrag: beim Start wird sie als
        // Window-Root gesetzt (die Shell existiert noch nicht), bei
        // Sitzungsablauf als geschobene Shell-Route.
        if (page == AppPage.Login)
        {
            if (window.Page is AppShell && Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("login");
            }
            else if (window.Page is not Pages.LoginPage)
            {
                window.Page = services.GetRequiredService<Pages.LoginPage>();
            }

            return;
        }

        // Übergang von der Start-Ladeanzeige zur Hauptnavigation.
        if (window.Page is not AppShell)
        {
            window.Page = services.GetRequiredService<AppShell>();

            // Shell.Current wird erst beim Anhängen der Shell gesetzt.
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (Shell.Current is null && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50);
            }
        }

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync($"//{AppRoutes.ToRoute(page)}");
        }
    }
}
