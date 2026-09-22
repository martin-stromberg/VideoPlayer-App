using VideoPlayer.ApiClient.Discovery;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.Maui.Services;

/// <summary>
/// Führt MVU-Effekte aus: Navigation über die Shell, Server-Erreichbarkeits-
/// prüfung über <see cref="IServerHealthProbe"/>, Ablage-Initialisierung und
/// Einstellungs-Persistenz über <see cref="VideoPlayer.Storage"/>, die
/// Serververwaltung (Laden, Discovery, Kandidaten-Prüfung, Speichern) sowie
/// den Anmelde-Workflow über <see cref="AuthSessionService"/>.
/// </summary>
public sealed class ShellAppEffectHandler : IAppEffectHandler
{
    private readonly IServiceProvider services;
    private readonly IServerHealthProbe healthProbe;
    private readonly AuthSessionService authSession;
    private readonly INetworkStatusMonitor networkMonitor;
    private readonly ServerEventsService serverEvents;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ShellAppEffectHandler"/>-Klasse.</summary>
    /// <param name="services">Dienstanbieter zum Auflösen der am Effekt beteiligten Dienste (inkl. <see cref="MvuHost"/>).</param>
    /// <param name="healthProbe">Erreichbarkeitsprüfung für den Server.</param>
    /// <param name="authSession">Sitzungsdienst für den Anmelde-Workflow.</param>
    /// <param name="networkMonitor">Netzwerkstatus-Erkennung; meldet Wechsel in den MVU-Zyklus.</param>
    /// <param name="serverEvents">Server-Ereignisverbindung; meldet Ereignisse und Trennung in den MVU-Zyklus.</param>
    public ShellAppEffectHandler(
        IServiceProvider services,
        IServerHealthProbe healthProbe,
        AuthSessionService authSession,
        INetworkStatusMonitor networkMonitor,
        ServerEventsService serverEvents)
    {
        this.services = services;
        this.healthProbe = healthProbe;
        this.authSession = authSession;
        this.networkMonitor = networkMonitor;
        this.serverEvents = serverEvents;

        // Endgültig abgelehnte stille Neuanmeldung (401 ohne gültige
        // Zugangsdaten) → die App wechselt in den Anmeldezustand.
        this.authSession.SessionExpired += OnSessionExpired;

        // Netzwerkwechsel und Server-Ereignisse laufen auf beliebigen
        // Threads — die Messages gehen über den MainThread in den
        // MVU-Zyklus.
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
            case SyncFavoritesEffect:
                await RunPartialSyncAsync(s => s.SyncFavoritesAsync(cancellationToken));
                break;
            case SyncMetadataEffect:
                await RunPartialSyncAsync(s => s.SyncMetadataAsync(cancellationToken));
                break;
            case LoadHomeListsEffect:
                await LoadHomeListsAsync();
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
            case ReportPlaybackProgressEffect report:
                await ReportPlaybackProgressAsync(report, cancellationToken);
                break;
            case TogglePictureInPictureEffect:
                await TogglePictureInPictureAsync();
                break;
            case LogoutEffect:
                await LogoutAsync();
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
            source = await services.GetRequiredService<PlaybackSourceService>()
                .ResolveAsync(resolve.MediaType, resolve.MediaId, cancellationToken);
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

    private async Task LoadHomeListsAsync()
    {
        HomeLists lists;
        try
        {
            lists = await services.GetRequiredService<HomeListService>().LoadHomeListsAsync();
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
            var route = $"sourceoverview?id={overview.SourceId}" +
                $"&name={Uri.EscapeDataString(overview.SourceName ?? string.Empty)}";
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Quellen-Navigation fehlgeschlagen: {ex.Message}");
        }
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
        // die App läuft mit Standardwerten weiter.
        try
        {
            // Die Auflösung des Layouts legt die Verzeichnisse an und wendet den
            // plattformseitigen Backup-Ausschluss an (AppStorageLayoutProvider).
            _ = services.GetRequiredService<AppStorageLayout>();
            await services.GetRequiredService<BackupDatabase>().InitializeAsync();
            await services.GetRequiredService<CacheDatabase>().InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Ablage-Initialisierung fehlgeschlagen: {ex.Message}");
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

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServersLoadedMessage(servers, activeAddress)));
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
        try
        {
            success = await healthProbe.CheckAsync(new Uri(address), cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Kandidatenprüfung fehlgeschlagen: {ex.Message}");
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerCheckCompletedMessage(address, displayName, success)));
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
    }

    private async Task RunHealthCheckAsync(CancellationToken cancellationToken)
    {
        // Jeder Fehler der Prüfung führt zu „nicht erreichbar" — die App darf
        // nicht auf der Ladeanzeige hängen bleiben.
        bool reachable;
        try
        {
            reachable = await healthProbe.CheckAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ShellAppEffectHandler] Erreichbarkeitsprüfung fehlgeschlagen: {ex.Message}");
            reachable = false;
        }

        var host = services.GetRequiredService<MvuHost>();
        await MainThread.InvokeOnMainThreadAsync(
            () => host.DispatchAsync(new ServerHealthCheckCompletedMessage(reachable)));
    }

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
