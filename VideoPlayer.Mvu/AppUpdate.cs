using VideoPlayer.Storage;

namespace VideoPlayer.Mvu;

/// <summary>
/// Reine Update-Funktion des MVU-Zyklus: bildet <see cref="AppMessage"/> auf ein
/// neues <see cref="AppModel"/> plus auszuführende <see cref="AppEffect"/>s ab.
/// </summary>
public static class AppUpdate
{
    /// <summary>Fehlertext bei ungültiger manueller Adresseingabe.</summary>
    public const string InvalidAddressError = "Bitte eine gültige Serveradresse eingeben.";

    /// <summary>Fehlertext bei fehlgeschlagener Verbindungsprüfung.</summary>
    public const string UnreachableServerError = "Der Server ist nicht erreichbar. Bitte die Adresse prüfen.";

    /// <summary>Fehlertext der Anmeldemaske bei leeren Eingabefeldern.</summary>
    public const string CredentialsRequiredError = "Bitte Benutzername und Passwort eingeben.";

    /// <summary>Fehlertext der Anmeldemaske bei vom Server abgelehnten Zugangsdaten.</summary>
    public const string InvalidCredentialsError = "Anmeldung fehlgeschlagen. Bitte Benutzername und Passwort prüfen.";

    /// <summary>Fehlertext der Anmeldemaske bei nicht erreichbarem Server.</summary>
    public const string LoginUnreachableError = "Der Server ist nicht erreichbar. Bitte später erneut versuchen.";

    /// <summary>Fehlertext der Anmeldemaske bei sonstigen Anmeldefehlern.</summary>
    public const string LoginFailedError = "Anmeldung fehlgeschlagen. Bitte erneut versuchen.";

    /// <summary>Hinweistext der Anmeldemaske nach abgelaufener Sitzung.</summary>
    public const string SessionExpiredError = "Die Anmeldung ist abgelaufen. Bitte erneut anmelden.";

    /// <summary>
    /// Wendet eine Nachricht auf das übergebene Model an und liefert den
    /// Übergang: das neue Model plus die daraus resultierenden Effekte.
    /// Unbekannte Nachrichten lassen das Model unverändert.
    /// </summary>
    /// <param name="model">Aktuelles, unveränderliches App-Model.</param>
    /// <param name="message">Die anzuwendende Nachricht.</param>
    /// <returns>Neues Model samt auszuführender Effekte.</returns>
    public static AppTransition Update(AppModel model, AppMessage message)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(message);

        return message switch
        {
            StartupRequestedMessage => UpdateStartupRequested(model),
            ServerHealthCheckCompletedMessage completed => UpdateHealthCheckCompleted(model, completed),
            NavigateToMessage navigate => UpdateNavigateTo(model, navigate),
            NavigatedMessage navigated => UpdateNavigated(model, navigated),
            LoadSettingsRequestedMessage => new AppTransition(model, [new LoadSettingsEffect()]),
            SettingsLoadedMessage loaded => UpdateSettingsLoaded(model, loaded),
            SaveSettingsRequestedMessage save => UpdateSaveSettingsRequested(model, save),
            LoadServersRequestedMessage => new AppTransition(model, [new LoadServersEffect()]),
            ServersLoadedMessage loaded => UpdateServersLoaded(model, loaded),
            ServerDiscoveryRequestedMessage => UpdateDiscoveryRequested(model),
            ServerDiscoveryCompletedMessage discovered => UpdateDiscoveryCompleted(model, discovered),
            ServerAddressSubmittedMessage submitted => UpdateAddressSubmitted(model, submitted),
            DiscoveredServerSelectedMessage selected => UpdateServerCandidateSelected(model, selected.Address, selected.DisplayName),
            ServerSwitchRequestedMessage switchTo => UpdateServerCandidateSelected(model, switchTo.Address, switchTo.DisplayName),
            ServerCheckCompletedMessage check => UpdateServerCheckCompleted(model, check),
            AutoLoginCompletedMessage autoLogin => UpdateAutoLoginCompleted(model, autoLogin),
            LoginSubmittedMessage submitted => UpdateLoginSubmitted(model, submitted),
            LoginCompletedMessage completed => UpdateLoginCompleted(model, completed),
            OfflineDataCheckCompletedMessage check => UpdateOfflineDataCheckCompleted(model, check),
            SessionExpiredMessage => UpdateSessionExpired(model),
            NetworkAvailabilityChangedMessage network => UpdateNetworkAvailabilityChanged(model, network),
            ServerEventReceivedMessage received => UpdateServerEventReceived(model, received),
            ServerEventsConnectionClosedMessage => UpdateServerEventsConnectionClosed(model),
            SynchronizationCompletedMessage => new AppTransition(
                model with { IsSyncRunning = false },
                // Nach dem Voll-Sync die Startseiten-Listen aus dem
                // aktualisierten Cache neu laden.
                [new LoadHomeListsEffect()]),
            CachedListsChangedMessage => new AppTransition(model, [new LoadHomeListsEffect()]),
            HomeListsLoadedMessage loaded => UpdateHomeListsLoaded(model, loaded),
            MediaItemSelectedMessage selected => UpdateMediaItemSelected(model, selected),
            SourcesPageOpenedMessage => UpdateSourcesPageOpened(model),
            SourcesLoadedMessage loaded => UpdateSourcesLoaded(model, loaded),
            SourceSelectedMessage selected => UpdateSourceSelected(model, selected),
            SourceBrowserOpenedMessage opened => UpdateSourceBrowserOpened(model, opened),
            SourceGenresLoadedMessage loaded => UpdateSourceGenresLoaded(model, loaded),
            SourceItemsLoadedMessage loaded => UpdateSourceItemsLoaded(model, loaded),
            SourceGenreSelectedMessage selected => UpdateSourceGenreSelected(model, selected),
            SourceSearchSubmittedMessage submitted => UpdateSourceSearchSubmitted(model, submitted),
            SourceItemsNextPageRequestedMessage => UpdateSourceItemsNextPageRequested(model),
            SourceItemsRetryRequestedMessage => UpdateSourceItemsRetryRequested(model),
            SourceMediaItemSelectedMessage selected => UpdateSourceMediaItemSelected(model, selected),
            MediaDetailsRequestedMessage requested => UpdateMediaDetailsRequested(model, requested),
            MediaDetailsLoadedMessage loaded => UpdateMediaDetailsLoaded(model, loaded),
            SeasonSelectedMessage selected => UpdateSeasonSelected(model, selected),
            FavoriteToggleRequestedMessage => UpdateFavoriteToggleRequested(model),
            FavoriteToggleCompletedMessage completed => UpdateFavoriteToggleCompleted(model, completed),
            _ => AppTransition.Unchanged(model)
        };
    }

    private static AppTransition UpdateStartupRequested(AppModel model)
    {
        if (model.IsStartupRunning && model.ServerHealth == ServerHealthStatus.Checking)
        {
            return AppTransition.Unchanged(model);
        }

        // Der Health-Check folgt erst nach dem Laden der Serververwaltung —
        // ohne hinterlegten Server verzweigt der Start in die Einrichtung.
        // Der Netzwerkmonitor läuft ab Start mit, damit Online↔Offline-
        // Wechsel zur Laufzeit erkannt werden.
        return new AppTransition(
            model with
            {
                IsStartupRunning = true,
                ServerHealth = ServerHealthStatus.Checking,
                ConnectionState = ConnectionState.Connecting
            },
            // Die gecachten Startseiten-Listen werden direkt nach der
            // Ablage-Initialisierung gelesen (Effekte laufen sequenziell) —
            // Cache-First: die Startseite zeigt beim Öffnen sofort den
            // letzten Stand, auch ohne Serverantwort.
            [new InitializeStorageEffect(), new LoadSettingsEffect(), new LoadServersEffect(), new StartNetworkMonitoringEffect(), new LoadHomeListsEffect()]);
    }

    private static AppTransition UpdateSettingsLoaded(AppModel model, SettingsLoadedMessage message)
    {
        return new AppTransition(
            model with { Settings = message.Settings.Normalize() },
            []);
    }

    private static AppTransition UpdateSaveSettingsRequested(AppModel model, SaveSettingsRequestedMessage message)
    {
        var normalized = message.Settings.Normalize();
        return new AppTransition(
            model with { Settings = normalized },
            [new PersistSettingsEffect(normalized)]);
    }

    private static AppTransition UpdateServersLoaded(AppModel model, ServersLoadedMessage message)
    {
        var updated = model with
        {
            KnownServers = message.Servers,
            ActiveServerAddress = message.ActiveAddress
        };

        if (message.ActiveAddress is not null)
        {
            // Hinterlegter Server: beim Start Erreichbarkeit prüfen (Abschluss
            // navigiert zur Startseite). Beim Nachladen aus der geöffneten
            // Serververwaltung („Server wechseln") nur das Model aktualisieren.
            return model.IsStartupRunning
                ? new AppTransition(updated, [new CheckServerHealthEffect()])
                : new AppTransition(updated, []);
        }

        // Erststart ohne hinterlegten Server: Einrichtungsdialog anzeigen.
        updated = updated with
        {
            IsStartupRunning = false,
            ServerHealth = ServerHealthStatus.Unknown,
            ConnectionState = ConnectionState.NeedsServerSetup,
            CurrentPage = AppPage.ServerSetup
        };

        return model.CurrentPage == AppPage.ServerSetup
            ? new AppTransition(updated, [])
            : new AppTransition(updated, [new NavigateToPageEffect(AppPage.ServerSetup)]);
    }

    private static AppTransition UpdateDiscoveryRequested(AppModel model)
    {
        if (model.IsDiscoveryRunning)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { IsDiscoveryRunning = true },
            [new DiscoverServersEffect()]);
    }

    private static AppTransition UpdateDiscoveryCompleted(AppModel model, ServerDiscoveryCompletedMessage message)
    {
        return new AppTransition(
            model with
            {
                IsDiscoveryRunning = false,
                DiscoveryAttempted = true,
                DiscoveredServers = message.Servers
            },
            []);
    }

    private static AppTransition UpdateAddressSubmitted(AppModel model, ServerAddressSubmittedMessage message)
    {
        if (!ServerAddress.TryNormalize(message.Text, out var normalized))
        {
            return new AppTransition(
                model with { ServerSetupError = InvalidAddressError },
                []);
        }

        return BeginServerCheck(model, normalized, null);
    }

    private static AppTransition UpdateServerCandidateSelected(AppModel model, string address, string? displayName)
    {
        if (!ServerAddress.TryNormalize(address, out var normalized))
        {
            return new AppTransition(
                model with { ServerSetupError = InvalidAddressError },
                []);
        }

        return BeginServerCheck(model, normalized, displayName);
    }

    private static AppTransition BeginServerCheck(AppModel model, string normalizedAddress, string? displayName)
    {
        return new AppTransition(
            model with { IsServerCheckRunning = true, ServerSetupError = null },
            [new CheckCandidateServerEffect(normalizedAddress, displayName)]);
    }

    private static AppTransition UpdateServerCheckCompleted(AppModel model, ServerCheckCompletedMessage message)
    {
        if (!message.Success)
        {
            return new AppTransition(
                model with
                {
                    IsServerCheckRunning = false,
                    ServerSetupError = UnreachableServerError
                },
                []);
        }

        var normalized = ServerAddress.Normalize(message.Address);
        var server = new KnownServer
        {
            Address = normalized,
            DisplayName = message.DisplayName ?? string.Empty,
            LastUsedAtUtc = DateTime.UtcNow
        };

        var known = model.KnownServers
            .Where(s => !string.Equals(s.Address, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        known.Insert(0, server);

        // Der Server wird übernommen; die (automatische) Anmeldung am neuen
        // Server entscheidet über das Navigationsziel — „Home" wird nur aus
        // Connected/Offline erreicht. IsServerCheckRunning bleibt bis zum
        // Login-Abschluss gesetzt (Anzeige läuft weiter).
        return new AppTransition(
            model with
            {
                IsServerCheckRunning = true,
                ServerSetupError = null,
                ActiveServerAddress = normalized,
                KnownServers = known,
                ServerHealth = ServerHealthStatus.Reachable,
                ConnectionState = ConnectionState.Connecting
            },
            [new PersistActiveServerEffect(normalized, message.DisplayName), new AttemptAutoLoginEffect(normalized)]);
    }

    private static AppTransition UpdateHealthCheckCompleted(AppModel model, ServerHealthCheckCompletedMessage message)
    {
        if (message.IsReachable)
        {
            if (model.ActiveServerAddress is null)
            {
                // Defensiv: Health-Check ohne aktiven Server → Einrichtung.
                return GoToServerSetup(model, ServerHealthStatus.Unknown);
            }

            // Erreichbar → automatische Anmeldung mit gespeicherten Zugangsdaten.
            return new AppTransition(
                model with { ServerHealth = ServerHealthStatus.Reachable },
                [new AttemptAutoLoginEffect(model.ActiveServerAddress)]);
        }

        // Nicht erreichbar → Offline-Daten-Bestand entscheidet zwischen
        // Offline-Zustand und Serverauswahl.
        return new AppTransition(
            model with { ServerHealth = ServerHealthStatus.Unreachable },
            [new CheckOfflineDataEffect()]);
    }

    private static AppTransition UpdateOfflineDataCheckCompleted(AppModel model, OfflineDataCheckCompletedMessage message)
    {
        if (message.HasOfflineData)
        {
            // Offline-Betrieb: auf Workflow-Seiten (Start-Ladeanzeige) wird
            // zur Startseite navigiert; auf einer Menüseite bleibt der
            // Anwender bei einer Wiederverbindungs-Prüfung stehen.
            var withOfflineData = model with
            {
                IsStartupRunning = false,
                HasOfflineData = true,
                ConnectionState = ConnectionState.Offline
            };
            return IsWorkflowPage(model.CurrentPage)
                ? new AppTransition(withOfflineData with { CurrentPage = AppPage.Home }, [new NavigateToPageEffect(AppPage.Home)])
                : new AppTransition(withOfflineData, []);
        }

        // Keine Offline-Daten → Serverauswahl (bekannte Server, Erkennung,
        // manuelle Eingabe auf der Einrichtungsseite).
        var withSetup = model with
        {
            IsStartupRunning = false,
            HasOfflineData = false,
            ConnectionState = ConnectionState.NeedsServerSetup,
            CurrentPage = AppPage.ServerSetup
        };
        return model.CurrentPage == AppPage.ServerSetup
            ? new AppTransition(withSetup, [])
            : new AppTransition(withSetup, [new NavigateToPageEffect(AppPage.ServerSetup)]);
    }

    private static AppTransition UpdateAutoLoginCompleted(AppModel model, AutoLoginCompletedMessage message)
    {
        if (message.Status == AuthAttemptStatus.Succeeded)
        {
            return EnterConnected(model with { LoginUsername = message.Username ?? model.LoginUsername });
        }

        // Fehlende oder fehlgeschlagene Zugangsdaten → Anmeldemaske.
        var updated = model with
        {
            IsStartupRunning = false,
            IsServerCheckRunning = false,
            IsLoginRunning = false,
            LoginUsername = message.Username ?? model.LoginUsername,
            LoginError = LoginErrorFor(message.Status),
            ConnectionState = ConnectionState.NeedsLogin,
            CurrentPage = AppPage.Login
        };
        return model.CurrentPage == AppPage.Login
            ? new AppTransition(updated, [])
            : new AppTransition(updated, [new NavigateToPageEffect(AppPage.Login)]);
    }

    private static AppTransition UpdateLoginSubmitted(AppModel model, LoginSubmittedMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Username) || string.IsNullOrWhiteSpace(message.Password))
        {
            return new AppTransition(
                model with { LoginError = CredentialsRequiredError },
                []);
        }

        if (model.ActiveServerAddress is null)
        {
            // Defensiv: Anmeldung ohne aktiven Server → Einrichtung.
            return GoToServerSetup(model, model.ServerHealth);
        }

        return new AppTransition(
            model with { IsLoginRunning = true, LoginError = null },
            [new AttemptLoginEffect(model.ActiveServerAddress, message.Username.Trim(), message.Password)]);
    }

    private static AppTransition UpdateLoginCompleted(AppModel model, LoginCompletedMessage message)
    {
        if (message.Status == AuthAttemptStatus.Succeeded)
        {
            return EnterConnected(model);
        }

        return new AppTransition(
            model with
            {
                IsLoginRunning = false,
                LoginError = LoginErrorFor(message.Status) ?? LoginFailedError,
                ConnectionState = ConnectionState.NeedsLogin
            },
            []);
    }

    /// <summary>
    /// Übergang in den verbundenen Zustand: baut die Ereignisverbindung auf
    /// und startet die vollständige Synchronisation. Die Startseite wird
    /// nur von Workflow-Seiten aus angesteuert — bei einer Wiederverbindung
    /// zur Laufzeit bleibt der Anwender auf der aktuellen Menüseite.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <returns>Übergang in den verbundenen Zustand samt Effekten.</returns>
    private static AppTransition EnterConnected(AppModel model)
    {
        var updated = model with
        {
            IsStartupRunning = false,
            IsServerCheckRunning = false,
            IsLoginRunning = false,
            LoginError = null,
            ConnectionState = ConnectionState.Connected,
            // Eine erfolgreiche Anmeldung beweist Konnektivität — der
            // Netzwerk-Ist-Wert wird damit konsistent zum Zustand.
            IsOnline = true,
            IsSyncRunning = true
        };

        var effects = new List<AppEffect>();
        if (model.ActiveServerAddress is not null)
        {
            effects.Add(new ConnectServerEventsEffect(model.ActiveServerAddress));
        }

        effects.Add(new RunFullSyncEffect());

        if (IsWorkflowPage(model.CurrentPage))
        {
            updated = updated with { CurrentPage = AppPage.Home };
            effects.Add(new NavigateToPageEffect(AppPage.Home));
        }

        return new AppTransition(updated, effects);
    }

    /// <summary>Seiten ohne Menüeintrag (Start-Ladeanzeige, Anmeldung, Servereinrichtung).</summary>
    /// <param name="page">Die zu prüfende Seite.</param>
    /// <returns><c>true</c>, wenn die Seite eine Workflow-Seite ist.</returns>
    private static bool IsWorkflowPage(AppPage page)
    {
        return page is AppPage.Loading or AppPage.Login or AppPage.ServerSetup;
    }

    private static AppTransition UpdateNetworkAvailabilityChanged(AppModel model, NetworkAvailabilityChangedMessage message)
    {
        if (!message.IsOnline)
        {
            // Netzwerkverlust: der Offline-Übergang greift nur aus dem
            // verbundenen Zustand — in den Workflow-Zuständen (Anmeldung,
            // Servereinrichtung, laufender Start) entscheiden die
            // ohnehin laufenden Prüfungen über den Ausgang.
            var updated = model with { IsOnline = false };
            return model.ConnectionState == ConnectionState.Connected
                ? new AppTransition(
                    updated with { ConnectionState = ConnectionState.Offline },
                    [new DisconnectServerEventsEffect()])
                : new AppTransition(updated, []);
        }

        // Online-Rückkehr: aus dem Offline-Zustand läuft die normale
        // Wiederverbindungs-Kette (Erreichbarkeitsprüfung → automatische
        // Anmeldung → Ereignisverbindung + Synchronisation).
        if (model.ConnectionState == ConnectionState.Offline && model.ActiveServerAddress is not null)
        {
            return new AppTransition(
                model with
                {
                    IsOnline = true,
                    ConnectionState = ConnectionState.Connecting,
                    ServerHealth = ServerHealthStatus.Checking
                },
                [new CheckServerHealthEffect()]);
        }

        return new AppTransition(model with { IsOnline = true }, []);
    }

    private static AppTransition UpdateServerEventReceived(AppModel model, ServerEventReceivedMessage message)
    {
        if (model.ConnectionState != ConnectionState.Connected)
        {
            return AppTransition.Unchanged(model);
        }

        // Eingehende Server-Ereignisse lösen die gezielte Aktualisierung der
        // betroffenen Caches aus — die Anzeigen werden darüber ohne
        // manuelle Aktualisierung frisch.
        return message.Kind switch
        {
            ServerEventKind.ContinueWatchingUpdated => new AppTransition(model, [new SyncContinueWatchingEffect()]),
            ServerEventKind.FavoritesChanged => new AppTransition(model, [new SyncFavoritesEffect()]),
            ServerEventKind.NewVideosScanned => new AppTransition(model, [new SyncMetadataEffect()]),
            _ => AppTransition.Unchanged(model)
        };
    }

    private static AppTransition UpdateServerEventsConnectionClosed(AppModel model)
    {
        // Endgültig getrennt: im verbundenen Online-Zustand die
        // Ereignisverbindung erneut aufbauen (idempotent).
        return model.ConnectionState == ConnectionState.Connected
            && model.IsOnline
            && model.ActiveServerAddress is not null
                ? new AppTransition(model, [new ConnectServerEventsEffect(model.ActiveServerAddress)])
                : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateSessionExpired(AppModel model)
    {
        if (model.ConnectionState == ConnectionState.NeedsLogin)
        {
            return AppTransition.Unchanged(model);
        }

        var updated = model with
        {
            ConnectionState = ConnectionState.NeedsLogin,
            LoginError = SessionExpiredError,
            CurrentPage = AppPage.Login
        };
        // Zur Anmeldemaske — die Ereignisverbindung wird getrennt (das
        // abgelaufene Token kann ohnehin nicht mehr autorisiert werden).
        var effects = new List<AppEffect> { new DisconnectServerEventsEffect() };
        if (model.CurrentPage != AppPage.Login)
        {
            effects.Add(new NavigateToPageEffect(AppPage.Login));
        }

        return new AppTransition(updated, effects);
    }

    private static AppTransition GoToServerSetup(AppModel model, ServerHealthStatus health)
    {
        var updated = model with
        {
            IsStartupRunning = false,
            ServerHealth = health,
            ConnectionState = ConnectionState.NeedsServerSetup,
            CurrentPage = AppPage.ServerSetup
        };
        return model.CurrentPage == AppPage.ServerSetup
            ? new AppTransition(updated, [])
            : new AppTransition(updated, [new NavigateToPageEffect(AppPage.ServerSetup)]);
    }

    private static AppTransition UpdateHomeListsLoaded(AppModel model, HomeListsLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                ContinueWatching = message.Lists.ContinueWatching,
                Favorites = message.Lists.Favorites,
                RecentEntries = message.Lists.Recent
            },
            []);
    }

    private static AppTransition UpdateMediaItemSelected(AppModel model, MediaItemSelectedMessage message)
    {
        var item = message.Item;
        if (item.Id <= 0 || string.IsNullOrWhiteSpace(item.MediaType))
        {
            // Ohne identifizierbares Medium keine Navigation.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model,
            [new NavigateToMediaDetailsEffect(
                item.MediaType, item.Id, item.Title,
                item.ShowId, item.SeasonId, item.EpisodeId)]);
    }

    private static AppTransition UpdateSourcesPageOpened(AppModel model)
    {
        if (model.IsOffline)
        {
            // Offline gibt es keine Quellenliste — der Hinweis wird über
            // IsOffline gebunden; ein zuvor geladener Stand wird
            // zurückgesetzt, damit nichts Veraltetes als aktuell erscheint.
            return model.SourcesOverview == SourcesOverviewState.Initial
                ? AppTransition.Unchanged(model)
                : new AppTransition(model with { SourcesOverview = SourcesOverviewState.Initial }, []);
        }

        if (model.ConnectionState != ConnectionState.Connected || model.SourcesOverview.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { SourcesOverview = SourcesOverviewState.Initial with { IsLoading = true } },
            [new LoadSourcesEffect()]);
    }

    private static AppTransition UpdateSourcesLoaded(AppModel model, SourcesLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                SourcesOverview = new SourcesOverviewState
                {
                    Attempted = true,
                    Sources = message.Sources,
                    Error = message.Error
                }
            },
            []);
    }

    private static SourceBrowserState NewBrowser(long sourceId, string sourceName) => new()
    {
        SourceId = sourceId,
        SourceName = sourceName,
        IsLoading = true,
        HasMore = true
    };

    private static AppTransition UpdateSourceSelected(AppModel model, SourceSelectedMessage message)
    {
        if (message.Source.Id <= 0)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { SourceBrowser = NewBrowser(message.Source.Id, message.Source.Name) },
            [
                new NavigateToSourceOverviewEffect(message.Source.Id, message.Source.Name),
                new LoadSourceGenresEffect(message.Source.Id),
                new LoadSourceItemsEffect(message.Source.Id, 0, null, null, Append: false)
            ]);
    }

    private static AppTransition UpdateSourceBrowserOpened(AppModel model, SourceBrowserOpenedMessage message)
    {
        if (message.SourceId <= 0)
        {
            return AppTransition.Unchanged(model);
        }

        // Rückkehr auf die bereits geöffnete Quelle (z. B. zurück von der
        // Detailseite): Zustand und Listenposition bleiben erhalten.
        if (model.SourceBrowser.SourceId == message.SourceId)
        {
            return AppTransition.Unchanged(model);
        }

        // Wiederherstellungspfad: Seite ohne passenden Zustand geöffnet →
        // Browser initialisieren und laden (ohne erneute Navigation).
        return new AppTransition(
            model with { SourceBrowser = NewBrowser(message.SourceId, message.SourceName) },
            [
                new LoadSourceGenresEffect(message.SourceId),
                new LoadSourceItemsEffect(message.SourceId, 0, null, null, Append: false)
            ]);
    }

    private static AppTransition UpdateSourceGenresLoaded(AppModel model, SourceGenresLoadedMessage message)
    {
        var browser = model.SourceBrowser;
        if (!browser.IsActive || browser.SourceId != message.SourceId)
        {
            // Antwort einer inzwischen verlassenen Quelle verwerfen.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                SourceBrowser = browser with
                {
                    Genres = message.Genres
                        .Select(g => g with { IsSelected = g.Id == browser.SelectedGenreId })
                        .ToList()
                }
            },
            []);
    }

    private static AppTransition UpdateSourceItemsLoaded(AppModel model, SourceItemsLoadedMessage message)
    {
        var browser = model.SourceBrowser;
        if (!browser.IsActive || browser.SourceId != message.SourceId)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Error is not null)
        {
            // Fehler beim Anhängen lassen die bestehende Liste stehen
            // (HasMore bleibt — der nächste Scroll versucht es erneut);
            // Fehler des Ersetz-Laufs werden als Fehlerzustand gezeigt.
            var failed = browser with
            {
                IsLoading = false,
                IsLoadingMore = false,
                Attempted = true
            };
            return new AppTransition(
                model with
                {
                    SourceBrowser = message.Append ? failed : failed with { Error = message.Error }
                },
                []);
        }

        if (message.Append && message.Page != browser.NextPage)
        {
            // Veraltete Anhänge-Antwort (Filter/Suche wurde zwischenzeitlich
            // geändert) — nicht an die neue Liste hängen.
            return AppTransition.Unchanged(model);
        }

        var items = message.Append
            ? browser.Items.Concat(message.Items).ToList()
            : (IReadOnlyList<SourceMediaItem>)message.Items;

        return new AppTransition(
            model with
            {
                SourceBrowser = browser with
                {
                    Items = items,
                    NextPage = message.Page + 1,
                    HasMore = message.HasMore,
                    IsLoading = false,
                    IsLoadingMore = false,
                    Attempted = true,
                    Error = null
                }
            },
            []);
    }

    /// <summary>Browser-Zustand für einen Filter-/Suchwechsel: Liste zurücksetzen, ab Seite 0 laden.</summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="updated">Browser-Zustand mit dem neuen Filter bzw. Suchtext.</param>
    /// <returns>Übergang mit zurückgesetzter Liste und Lade-Effekt für die erste Seite.</returns>
    private static AppTransition ReloadBrowserItems(AppModel model, SourceBrowserState updated)
    {
        return new AppTransition(
            model with
            {
                SourceBrowser = updated with
                {
                    Items = [],
                    NextPage = 0,
                    HasMore = true,
                    IsLoading = true,
                    IsLoadingMore = false,
                    Attempted = true,
                    Error = null
                }
            },
            [new LoadSourceItemsEffect(
                updated.SourceId,
                0,
                string.IsNullOrWhiteSpace(updated.SearchText) ? null : updated.SearchText,
                updated.SelectedGenreId > 0 ? updated.SelectedGenreId : null,
                Append: false)]);
    }

    private static AppTransition UpdateSourceGenreSelected(AppModel model, SourceGenreSelectedMessage message)
    {
        var browser = model.SourceBrowser;
        if (!browser.IsActive || browser.SelectedGenreId == message.GenreId)
        {
            return AppTransition.Unchanged(model);
        }

        var updated = browser with
        {
            SelectedGenreId = message.GenreId,
            Genres = browser.Genres
                .Select(g => g with { IsSelected = g.Id == message.GenreId })
                .ToList()
        };
        return ReloadBrowserItems(model, updated);
    }

    private static AppTransition UpdateSourceSearchSubmitted(AppModel model, SourceSearchSubmittedMessage message)
    {
        var browser = model.SourceBrowser;
        var searchText = message.SearchText?.Trim() ?? string.Empty;
        if (!browser.IsActive || browser.SearchText == searchText)
        {
            return AppTransition.Unchanged(model);
        }

        return ReloadBrowserItems(model, browser with { SearchText = searchText });
    }

    private static AppTransition UpdateSourceItemsNextPageRequested(AppModel model)
    {
        var browser = model.SourceBrowser;
        if (!browser.IsActive || !browser.HasMore || browser.IsLoading || browser.IsLoadingMore)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { SourceBrowser = browser with { IsLoadingMore = true } },
            [new LoadSourceItemsEffect(
                browser.SourceId,
                browser.NextPage,
                string.IsNullOrWhiteSpace(browser.SearchText) ? null : browser.SearchText,
                browser.SelectedGenreId > 0 ? browser.SelectedGenreId : null,
                Append: true)]);
    }

    private static AppTransition UpdateSourceItemsRetryRequested(AppModel model)
    {
        var browser = model.SourceBrowser;
        if (!browser.IsActive || browser.IsLoading || browser.IsLoadingMore)
        {
            return AppTransition.Unchanged(model);
        }

        // Fehlerzustand einer leeren Liste → kompletter Neu-Lauf;
        // sonst die ausstehende Folgeseite erneut versuchen.
        return browser.HasItems
            ? UpdateSourceItemsNextPageRequested(model)
            : ReloadBrowserItems(model, browser);
    }

    private static AppTransition UpdateSourceMediaItemSelected(AppModel model, SourceMediaItemSelectedMessage message)
    {
        var item = message.Item;
        if (item.Id <= 0 || string.IsNullOrWhiteSpace(item.MediaType))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model,
            [new NavigateToMediaDetailsEffect(item.MediaType, item.Id, item.Title)]);
    }

    private static AppTransition UpdateMediaDetailsRequested(AppModel model, MediaDetailsRequestedMessage message)
    {
        var mediaType = MediaTypeNames.Normalize(message.MediaType);
        if (message.MediaId <= 0 || string.IsNullOrWhiteSpace(mediaType))
        {
            return new AppTransition(
                model with
                {
                    MediaDetails = new MediaDetailsState
                    {
                        MediaType = mediaType,
                        MediaId = message.MediaId,
                        Attempted = true,
                        Error = "Das Medium kann nicht angezeigt werden."
                    }
                },
                []);
        }

        var state = model.MediaDetails;
        if (state.MediaType == mediaType
            && state.MediaId == message.MediaId
            && (state.IsLoading || state.Details is not null))
        {
            // Gleiches Ziel bereits ladend oder geladen (z. B. erneutes
            // OnAppearing) — kein zweiter Lauf.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = new MediaDetailsState
                {
                    MediaType = mediaType,
                    MediaId = message.MediaId,
                    IsLoading = true,
                    Attempted = true
                }
            },
            [new LoadMediaDetailsEffect(
                mediaType, message.MediaId, message.Title,
                message.ShowId, message.SeasonId, message.EpisodeId)]);
    }

    private static AppTransition UpdateMediaDetailsLoaded(AppModel model, MediaDetailsLoadedMessage message)
    {
        var state = model.MediaDetails;
        if (state.MediaId != message.MediaId
            || state.MediaType != MediaTypeNames.Normalize(message.MediaType))
        {
            // Antwort einer veralteten Anfrage verwerfen.
            return AppTransition.Unchanged(model);
        }

        var error = message.Error ?? (message.Details is null ? "Die Details konnten nicht geladen werden." : null);
        return new AppTransition(
            model with
            {
                MediaDetails = state with
                {
                    IsLoading = false,
                    Attempted = true,
                    Details = message.Details,
                    Error = error
                }
            },
            []);
    }

    private static AppTransition UpdateSeasonSelected(AppModel model, SeasonSelectedMessage message)
    {
        var details = model.MediaDetails.Details;
        if (details is null
            || !details.HasSeasons
            || details.SelectedSeasonId == message.SeasonId)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = model.MediaDetails with
                {
                    Details = details with { SelectedSeasonId = message.SeasonId }
                }
            },
            []);
    }

    private static AppTransition UpdateFavoriteToggleRequested(AppModel model)
    {
        var state = model.MediaDetails;
        if (state.Details is null || state.IsFavoriteUpdating)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { MediaDetails = state with { IsFavoriteUpdating = true, FavoriteError = null } },
            [new ToggleFavoriteEffect(state.Details.MediaType, state.Details.MediaId, state.Details.Title)]);
    }

    private static AppTransition UpdateFavoriteToggleCompleted(AppModel model, FavoriteToggleCompletedMessage message)
    {
        var state = model.MediaDetails;
        var details = state.Details;
        if (details is null
            || details.MediaId != message.MediaId
            || details.MediaType != MediaTypeNames.Normalize(message.MediaType))
        {
            // Zwischenzeitlich wurde ein anderes Medium geladen — die
            // Rückmeldung gehört zur vorherigen Detailseite.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = state with
                {
                    IsFavoriteUpdating = false,
                    Details = message.IsFavorite is bool isFavorite
                        ? details with { IsFavorite = isFavorite }
                        : details,
                    FavoriteError = message.IsFavorite is null
                        ? "Die Favoriten-Markierung konnte nicht geändert werden."
                        : null
                }
            },
            []);
    }

    private static string? LoginErrorFor(AuthAttemptStatus status)
    {
        return status switch
        {
            AuthAttemptStatus.InvalidCredentials => InvalidCredentialsError,
            AuthAttemptStatus.ServerUnreachable => LoginUnreachableError,
            AuthAttemptStatus.Failed => LoginFailedError,
            _ => null
        };
    }

    private static AppTransition UpdateNavigateTo(AppModel model, NavigateToMessage message)
    {
        if (message.Page == AppPage.Loading || message.Page == model.CurrentPage)
        {
            return AppTransition.Unchanged(model);
        }

        // Zustandsgesteuerte Navigation: Solange die Anmeldung bzw. die
        // Servereinrichtung aussteht, sind Menüseiten nicht erreichbar —
        // erlaubt bleibt nur der Wechsel zwischen den Workflow-Seiten
        // (z. B. „Anderen Server einrichten" von der Anmeldemaske).
        if (model.ConnectionState is ConnectionState.NeedsLogin or ConnectionState.NeedsServerSetup
            && AppRoutes.MenuPages.Contains(message.Page))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { CurrentPage = message.Page },
            [new NavigateToPageEffect(message.Page)]);
    }

    private static AppTransition UpdateNavigated(AppModel model, NavigatedMessage message)
    {
        if (message.Page == AppPage.Loading || message.Page == model.CurrentPage)
        {
            return AppTransition.Unchanged(model);
        }

        // Flyout-Navigation läuft nativ über die Shell: Bei ausstehender
        // Anmeldung/Servereinrichtung wird auf die Workflow-Seite
        // zurücknavigiert statt den Modelzustand zu übernehmen.
        if (model.ConnectionState is ConnectionState.NeedsLogin or ConnectionState.NeedsServerSetup
            && AppRoutes.MenuPages.Contains(message.Page))
        {
            var target = model.ConnectionState == ConnectionState.NeedsLogin
                ? AppPage.Login
                : AppPage.ServerSetup;
            return new AppTransition(model, [new NavigateToPageEffect(target)]);
        }

        return new AppTransition(model with { CurrentPage = message.Page }, []);
    }
}
