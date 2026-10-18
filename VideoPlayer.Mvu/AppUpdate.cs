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
    public const string CredentialsRequiredError = "Bitte E-Mail-Adresse und Passwort eingeben.";

    /// <summary>Fehlertext der Anmeldemaske bei vom Server abgelehnten Zugangsdaten.</summary>
    public const string InvalidCredentialsError = "Anmeldung fehlgeschlagen. Bitte E-Mail-Adresse und Passwort prüfen.";

    /// <summary>Fehlertext der Anmeldemaske bei nicht erreichbarem Server.</summary>
    public const string LoginUnreachableError = "Der Server ist nicht erreichbar. Bitte später erneut versuchen.";

    /// <summary>Fehlertext der Anmeldemaske bei sonstigen Anmeldefehlern.</summary>
    public const string LoginFailedError = "Anmeldung fehlgeschlagen. Bitte erneut versuchen.";

    /// <summary>Hinweis nach einer Wiederherstellung: Geheimnisse wurden nicht gesichert — erneute Anmeldung erforderlich.</summary>
    public const string ReloginRequiredNotice = "Die gespeicherte Anmeldung ist nicht mehr verfügbar. Bitte melde dich erneut an.";

    /// <summary>Hinweistext der Anmeldemaske nach abgelaufener Sitzung.</summary>
    public const string SessionExpiredError = "Die Anmeldung ist abgelaufen. Bitte erneut anmelden.";

    /// <summary>Fehlertext der Pairing-Eingabe bei ungültigem/abgelaufenem/bereits verwendetem Einmal-Code (HTTP 401).</summary>
    public const string InvalidPairingCodeError = "Der Pairing-Code ist ungültig, abgelaufen oder bereits verwendet. Bitte den Code prüfen.";

    /// <summary>Hinweistext der Pairing-Eingabe bei fehlendem oder falsch formatiertem Code (lokale Formatprüfung).</summary>
    public const string PairingCodeFormatError = "Der Code ist nicht vollständig oder enthält ungültige Zeichen — er besteht aus Großbuchstaben und Ziffern, ohne 0, O, 1, I und L.";

    /// <summary>Fehlertext der Pairing-Eingabe bei serverseitiger Versuchssperre (HTTP 429).</summary>
    public const string PairingRateLimitedError = "Zu viele Pairing-Versuche. Bitte später erneut versuchen.";

    /// <summary>Fehlertext der Pairing-Eingabe bei Servern ohne Pairing-Endpunkt (HTTP 404).</summary>
    public const string PairingNotSupportedError = "Der Server unterstützt kein Geräte-Pairing — bitte den Server aktualisieren oder dich an die Person wenden, die den Server eingerichtet hat.";

    /// <summary>Fehlertext der Pairing-Eingabe bei sonstigen Pairing-Fehlern.</summary>
    public const string PairingFailedError = "Das Geräte-Pairing ist fehlgeschlagen. Bitte erneut versuchen.";

    /// <summary>Fehlertext der Pairing-Eingabe, nachdem der Server das Geräte-Token verworfen hat.</summary>
    public const string DeviceRevokedError = "Der Geräte-Zugang wurde vom Server abgelehnt. Bitte das Gerät erneut koppeln.";

    /// <summary>Hinweistext der Kopplungs-Eingabe bei leerer oder nicht als Ticket-Adresse/Kurzcode lesbarer Eingabe.</summary>
    public const string InvalidBootstrapInputError = "Bitte eine gültige Ticket-Adresse oder einen gültigen Kurzcode eingeben.";

    /// <summary>Hinweistext der Kopplungs-Eingabe, wenn ein Kurzcode ohne verbundenen Server eingegeben wird.</summary>
    public const string BootstrapNeedsServerError = "Für einen Kurzcode muss zuerst ein Server verbunden sein — oder die vollständige Ticket-Adresse eingeben.";

    /// <summary>Fehlertext der Kopplungs-Eingabe bei ungültigem/abgelaufenem Ticket (HTTP 400/401).</summary>
    public const string InvalidBootstrapTicketError = "Der Kopplungscode ist ungültig oder abgelaufen. Bitte in der Serververwaltung (Profilseite „Geräte“) einen neuen Code erstellen.";

    /// <summary>Fehlertext der Kopplungs-Eingabe bei serverseitiger Versuchssperre (HTTP 429).</summary>
    public const string BootstrapRateLimitedError = "Zu viele Kopplungsversuche. Bitte später erneut versuchen.";

    /// <summary>Fehlertext der Kopplungs-Eingabe bei Servern ohne Bootstrap-Endpunkt (HTTP 404).</summary>
    public const string BootstrapNotSupportedError = "Der Server unterstützt das Koppeln per Code nicht — bitte den Server aktualisieren oder dich an die Person wenden, die den Server eingerichtet hat.";

    /// <summary>Fehlertext der Kopplungs-Eingabe bei nicht erreichbarem Server aus der Ticket-Adresse.</summary>
    public const string BootstrapServerUnreachableError = "Der Server aus dem Kopplungscode ist nicht erreichbar. Bitte die Verbindung prüfen.";

    /// <summary>Fehlertext der Kopplungs-Eingabe bei sonstigen Bootstrap-Fehlern.</summary>
    public const string BootstrapFailedError = "Das Koppeln ist fehlgeschlagen. Bitte erneut versuchen.";

    /// <summary>Fehlertext der Wiedergabeseite bei nicht verfügbarer/abspielbarer Quelle.</summary>
    public const string PlaybackUnavailableError = "Das Video kann nicht abgespielt werden.";

    /// <summary>
    /// Toleranzfenster der Ende-Plausibilitätsprüfung in Sekunden: ein
    /// <c>MediaEnded</c>-Ereignis gilt nur als tatsächliches Wiedergabeende,
    /// wenn die Restlaufzeit (Dauer − Position) innerhalb dieses Fensters
    /// liegt. Der feste Wert orientiert sich am Standard des Angesehen-
    /// Endfensters (<see cref="AppSettings.WatchedEndWindowSeconds"/>,
    /// 300 s) — innerhalb dieses Fensters markiert der Fortschrittstakt
    /// das Medium ohnehin als angesehen bzw. greift die Auto-Fortsetzung
    /// bereits, sodass ein Ende-Ereignis dort zum Normalablauf konsistent
    /// ist. Die Schwelle ist bewusst eine Konstante, keine Einstellung.
    /// </summary>
    private const int PlaybackEndedToleranceSeconds = 300;

    /// <summary>Fehlertext des Protokollversands, wenn noch keine Protokolldatei vorhanden ist.</summary>
    public const string LogProtocolEmptyError = "Es ist noch kein Protokoll vorhanden.";

    /// <summary>Fehlertext des Protokollversands, wenn weder ein Mailentwurf geöffnet noch die Datei geteilt werden kann.</summary>
    public const string LogSendFailedError = "Der Protokollversand ist auf diesem Gerät nicht möglich — es konnte weder ein E-Mail-Entwurf geöffnet noch die Datei geteilt werden.";

    /// <summary>Fehlertext der Playlists-Seite ohne Serververbindung (reines Online-Feature, kein Cache-Stand).</summary>
    public const string PlaylistsOfflineError = "Playlists stehen nur bei bestehender Serververbindung zur Verfügung.";

    /// <summary>Fehlertext der Playlists-Seite bei fehlgeschlagenem Serverabruf.</summary>
    public const string PlaylistsLoadError = "Die Playlists konnten nicht geladen werden.";

    /// <summary>Fehlertext der Playlist-Detailseite bei fehlgeschlagenem Eintragsabruf.</summary>
    public const string PlaylistEntriesLoadError = "Die Einträge konnten nicht geladen werden.";

    /// <summary>Validierungstext des Playlist-Formulars bei leerem Namen.</summary>
    public const string PlaylistNameRequiredError = "Bitte einen Namen für die Playlist eingeben.";

    /// <summary>Fehlertext des Playlist-Formulars bei fehlgeschlagenem Speichern.</summary>
    public const string PlaylistSaveError = "Die Playlist konnte nicht gespeichert werden.";

    /// <summary>Fehlertext der Detailseite bei fehlgeschlagenem Löschen der Playlist.</summary>
    public const string PlaylistDeleteError = "Die Playlist konnte nicht gelöscht werden.";

    /// <summary>Fehlertext der Detailseite bei fehlgeschlagenem Entfernen eines Eintrags.</summary>
    public const string PlaylistEntryRemoveError = "Der Eintrag konnte nicht entfernt werden.";

    /// <summary>Fehlertext der Detailseite bei fehlgeschlagenem Verschieben eines Eintrags.</summary>
    public const string PlaylistEntryMoveError = "Der Eintrag konnte nicht verschoben werden.";

    /// <summary>Fehlertext der Detailseite bei fehlgeschlagenem Sortiermodus-Wechsel.</summary>
    public const string PlaylistSortModeError = "Der Sortiermodus konnte nicht geändert werden.";

    /// <summary>Fehlertext des Genre-Editors bei fehlgeschlagenem Laden oder Speichern.</summary>
    public const string PlaylistGenresError = "Die Genres konnten nicht gespeichert werden.";

    /// <summary>Fehlertext des Genre-Editors bei fehlgeschlagenem Optionsabruf.</summary>
    public const string PlaylistGenreOptionsLoadError = "Die Genres konnten nicht geladen werden.";

    /// <summary>Fehlertext der Detailseite bei fehlgeschlagener Öffentlich-Umschaltung (z. B. fehlende Administrator-Rechte).</summary>
    public const string PlaylistPublicError = "Das Öffentlich-Flag konnte nicht geändert werden.";

    /// <summary>Fehlertext der Medien-Detailseite bei fehlgeschlagenem Laden der Playlist-Auswahl.</summary>
    public const string PlaylistTargetsLoadError = "Die Playlists konnten nicht geladen werden.";

    /// <summary>Fehlertext der Medien-Detailseite bei fehlgeschlagenem Hinzufügen zu einer Playlist.</summary>
    public const string PlaylistEntryAddError = "Das Medium konnte nicht zur Playlist hinzugefügt werden.";

    /// <summary>Erfolgstext der Eintrags-Entfernung (Hinweiszeile der Detailseite).</summary>
    public const string PlaylistEntryRemovedNotice = "Der Eintrag wurde entfernt.";

    /// <summary>Erfolgstext eines Eintrags-Verschiebens (Hinweiszeile der Detailseite).</summary>
    public const string PlaylistEntryMovedNotice = "Der Eintrag wurde verschoben.";

    /// <summary>Erfolgstext der Genre-Aktualisierung (Hinweiszeile der Detailseite).</summary>
    public const string PlaylistGenresSavedNotice = "Die Genres wurden aktualisiert.";

    /// <summary>Erfolgstext der Öffentlich-Umschaltung (Hinweiszeile der Detailseite).</summary>
    public const string PlaylistPublicSavedNotice = "Die Sichtbarkeit wurde geändert.";

    /// <summary>Erfolgstext des Sortiermodus-Wechsels (Hinweiszeile der Detailseite).</summary>
    public const string PlaylistSortModeSavedNotice = "Der Sortiermodus wurde geändert.";

    /// <summary>Hinweistext der Playlist-Auswahl ohne eigene Playlist (Klartext-Alternative zur Auswahl).</summary>
    public const string PlaylistNoTargetsNotice = "Es gibt noch keine eigene Playlist — lege sie zuerst im Bereich „Playlists“ an.";

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
            PairingCodeSubmittedMessage submitted => UpdatePairingCodeSubmitted(model, submitted),
            PairingCompletedMessage completed => UpdatePairingCompleted(model, completed),
            DeviceTokenRevokedMessage => UpdateDeviceTokenRevoked(model),
            BootstrapTicketSubmittedMessage submitted => UpdateBootstrapTicketSubmitted(model, submitted),
            BootstrapServerCheckedMessage check => UpdateBootstrapServerChecked(model, check),
            BootstrapCompletedMessage completed => UpdateBootstrapCompleted(model, completed),
            AutoLoginCompletedMessage autoLogin => UpdateAutoLoginCompleted(model, autoLogin),
            LoginSubmittedMessage submitted => UpdateLoginSubmitted(model, submitted),
            LoginCompletedMessage completed => UpdateLoginCompleted(model, completed),
            OfflineDataCheckCompletedMessage check => UpdateOfflineDataCheckCompleted(model, check),
            SessionExpiredMessage => UpdateSessionExpired(model),
            NetworkAvailabilityChangedMessage network => UpdateNetworkAvailabilityChanged(model, network),
            ServerEventReceivedMessage received => UpdateServerEventReceived(model, received),
            ServerEventsConnectionClosedMessage => UpdateServerEventsConnectionClosed(model),
            SynchronizationCompletedMessage => UpdateSynchronizationCompleted(model),
            CachedListsChangedMessage => UpdateCachedListsChanged(model),
            HomeListsLoadedMessage loaded => UpdateHomeListsLoaded(model, loaded),
            HeroBackgroundLoadedMessage loaded => UpdateHeroBackgroundLoaded(model, loaded),
            StorageRecoveredMessage recovered => UpdateStorageRecovered(model, recovered),
            MediaItemSelectedMessage selected => UpdateMediaItemSelected(model, selected),
            ContinueWatchingItemSelectedMessage selected => UpdateContinueWatchingItemSelected(model, selected),
            ContinueWatchingHideRequestedMessage requested => UpdateContinueWatchingHideRequested(model, requested),
            ContinueWatchingSkipRequestedMessage requested => UpdateContinueWatchingSkipRequested(model, requested),
            UpcomingEpisodesLoadedMessage loaded => UpdateUpcomingEpisodesLoaded(model, loaded),
            SourcesLoadedMessage loaded => UpdateSourcesLoaded(model, loaded),
            SourcesReloadRequestedMessage => UpdateSourcesReloadRequested(model),
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
            MediaDetailsHeaderImageLoadedMessage headerImage => UpdateMediaDetailsHeaderImageLoaded(model, headerImage),
            SeasonSelectedMessage selected => UpdateSeasonSelected(model, selected),
            FavoriteToggleRequestedMessage => UpdateFavoriteToggleRequested(model),
            FavoriteToggleCompletedMessage completed => UpdateFavoriteToggleCompleted(model, completed),
            FavoritesPageOpenedMessage => UpdateFavoritesPageOpened(model),
            FavoritesPageLoadedMessage loaded => UpdateFavoritesPageLoaded(model, loaded),
            FavoriteRemoveRequestedMessage requested => UpdateFavoriteRemoveRequested(model, requested),
            FavoriteRemoveCompletedMessage completed => UpdateFavoriteRemoveCompleted(model, completed),
            PlaylistsPageOpenedMessage => UpdatePlaylistsPageOpened(model),
            PlaylistsLoadedMessage loaded => UpdatePlaylistsLoaded(model, loaded),
            PlaylistSelectedMessage selected => UpdatePlaylistSelected(model, selected),
            PlaylistDetailsOpenedMessage opened => UpdatePlaylistDetailsOpened(model, opened),
            PlaylistDetailsLoadedMessage loaded => UpdatePlaylistDetailsLoaded(model, loaded),
            PlaylistEntriesLoadedMessage loaded => UpdatePlaylistEntriesLoaded(model, loaded),
            PlaylistEntriesNextPageRequestedMessage => UpdatePlaylistEntriesNextPageRequested(model),
            PlaylistEntriesRetryRequestedMessage => UpdatePlaylistEntriesRetryRequested(model),
            PlaylistEntrySelectedMessage selected => UpdatePlaylistEntrySelected(model, selected),
            PlaylistPlaybackStartRequestedMessage requested => UpdatePlaylistPlaybackStartRequested(model, requested),
            PlaylistPlaybackStartedMessage started => UpdatePlaylistPlaybackStarted(model, started),
            PlaylistNextRequestedMessage => UpdatePlaylistNextRequested(model),
            PlaylistPreviousRequestedMessage => UpdatePlaylistPreviousRequested(model),
            PlaylistNavigationCompletedMessage completed => UpdatePlaylistNavigationCompleted(model, completed),
            PlaylistCreateRequestedMessage => UpdatePlaylistCreateRequested(model),
            PlaylistEditRequestedMessage => UpdatePlaylistEditRequested(model),
            PlaylistFormNameChangedMessage changed => UpdatePlaylistFormNameChanged(model, changed),
            PlaylistFormDescriptionChangedMessage changed => UpdatePlaylistFormDescriptionChanged(model, changed),
            PlaylistFormSortModeSelectedMessage selected => UpdatePlaylistFormSortModeSelected(model, selected),
            PlaylistFormCancelMessage => UpdatePlaylistFormCancel(model),
            PlaylistFormSaveRequestedMessage => UpdatePlaylistFormSaveRequested(model),
            PlaylistFormCompletedMessage completed => UpdatePlaylistFormCompleted(model, completed),
            PlaylistDeleteConfirmedMessage confirmed => UpdatePlaylistDeleteConfirmed(model, confirmed),
            PlaylistDeletedMessage deleted => UpdatePlaylistDeleted(model, deleted),
            PlaylistEntryRemoveRequestedMessage requested => UpdatePlaylistEntryRemoveRequested(model, requested),
            PlaylistEntryRemoveCompletedMessage completed => UpdatePlaylistEntryRemoveCompleted(model, completed),
            PlaylistEntryRemoveConfirmedMessage confirmed => UpdatePlaylistEntryRemoveConfirmed(model, confirmed),
            PlaylistEntryMoveToBeginningRequestedMessage requested => UpdatePlaylistEntryMoveToBeginningRequested(model, requested),
            PlaylistEntryMoveToEndRequestedMessage requested => UpdatePlaylistEntryMoveToEndRequested(model, requested),
            PlaylistEntryMoveBetweenRequestedMessage requested => UpdatePlaylistEntryMoveBetweenRequested(model, requested),
            PlaylistEntryMoveCompletedMessage completed => UpdatePlaylistEntryMoveCompleted(model, completed),
            PlaylistSortModeRequestedMessage requested => UpdatePlaylistSortModeRequested(model, requested),
            PlaylistSortModeCompletedMessage completed => UpdatePlaylistSortModeCompleted(model, completed),
            PlaylistSortModeConfirmedMessage confirmed => UpdatePlaylistSortModeConfirmed(model, confirmed),
            PlaylistGenreEditorRequestedMessage => UpdatePlaylistGenreEditorRequested(model),
            PlaylistGenreOptionsLoadedMessage loaded => UpdatePlaylistGenreOptionsLoaded(model, loaded),
            PlaylistGenreToggledMessage toggled => UpdatePlaylistGenreToggled(model, toggled),
            PlaylistGenresSaveRequestedMessage => UpdatePlaylistGenresSaveRequested(model),
            PlaylistGenresResetRequestedMessage => UpdatePlaylistGenresResetRequested(model),
            PlaylistGenresCompletedMessage completed => UpdatePlaylistGenresCompleted(model, completed),
            PlaylistGenreEditorCancelMessage => UpdatePlaylistGenreEditorCancel(model),
            PlaylistPublicToggledMessage toggled => UpdatePlaylistPublicToggled(model, toggled),
            PlaylistPublicCompletedMessage completed => UpdatePlaylistPublicCompleted(model, completed),
            PlaylistAddTargetsRequestedMessage => UpdatePlaylistAddTargetsRequested(model),
            PlaylistTargetsLoadedMessage loaded => UpdatePlaylistTargetsLoaded(model, loaded),
            PlaylistPickerDismissedMessage dismissed => UpdatePlaylistPickerDismissed(model, dismissed),
            PlaylistEntryAddedMessage added => UpdatePlaylistEntryAdded(model, added),
            ProfilePageOpenedMessage => UpdateProfilePageOpened(model),
            ProfileInfoLoadedMessage loaded => UpdateProfileInfoLoaded(model, loaded),
            PlayRequestedMessage play => UpdatePlayRequested(model, play),
            PlayerPageOpenedMessage => UpdatePlayerPageOpened(model),
            PlaybackSourceResolvedMessage resolved => UpdatePlaybackSourceResolved(model, resolved),
            PlaybackProgressMessage progress => UpdatePlaybackProgress(model, progress),
            PlaybackEndedMessage => UpdatePlaybackEnded(model),
            PlaybackFailedMessage => UpdatePlaybackFailed(model),
            PictureInPictureRequestedMessage => UpdatePictureInPictureRequested(model),
            PictureInPictureChangedMessage changed => UpdatePictureInPictureChanged(model, changed),
            PlayerClosedMessage => UpdatePlayerClosed(model),
            DownloadsPageOpenedMessage => UpdateDownloadsPageOpened(model),
            DownloadsLoadedMessage loaded => UpdateDownloadsLoaded(model, loaded),
            DownloadRequestedMessage requested => UpdateDownloadRequested(model, requested),
            DownloadEnqueueCompletedMessage completed => UpdateDownloadEnqueueCompleted(model, completed),
            DownloadItemChangedMessage changed => UpdateDownloadItemChanged(model, changed),
            DownloadDeletedMessage deleted => UpdateDownloadDeleted(model, deleted),
            DownloadCancelRequestedMessage requested => UpdateDownloadCancelRequested(model, requested),
            DownloadRetryRequestedMessage requested => UpdateDownloadRetryRequested(model, requested),
            DownloadDeleteRequestedMessage requested => UpdateDownloadDeleteRequested(model, requested),
            DownloadsDeleteAllRequestedMessage => UpdateDownloadsDeleteAllRequested(model),
            DownloadsPurgeOrphansRequestedMessage => UpdateDownloadsPurgeOrphansRequested(model),
            DownloadsDeleteFailedRequestedMessage => UpdateDownloadsDeleteFailedRequested(model),
            SendLogProtocolRequestedMessage => UpdateSendLogProtocolRequested(model),
            SendLogProtocolCompletedMessage completed => UpdateSendLogProtocolCompleted(model, completed),
            LogoutRequestedMessage => UpdateLogoutRequested(model),
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
        var effects = new List<AppEffect> { new PersistSettingsEffect(normalized) };
        // Beim Einschalten der automatischen Downloads läuft der Vorlauf
        // sofort an (Throttle umgangen) — nur bei bestehender Verbindung.
        if (normalized.AutoDownloadEnabled
            && !model.Settings.AutoDownloadEnabled
            && model.ConnectionState == ConnectionState.Connected)
        {
            effects.Add(new RunAutoDownloadsEffect(normalized, Force: true));
        }

        // Beim Einschalten des Kopfbereichs (bzw. bei jeder Speicherung,
        // falls nach einem Fehlschlag noch kein Bild vorliegt) wird das
        // Hero-Bild nachgeladen — nur bei bestehender Verbindung.
        var updated = WithHeroLoad(model with { Settings = normalized }, effects);
        return new AppTransition(updated, effects);
    }

    private static AppTransition UpdateSendLogProtocolRequested(AppModel model)
    {
        // Der Versand ist nur bei aktivierter Protokollierung zulässig —
        // der Button ist dann auch nur sichtbar; der Guard fängt
        // veraltete/externe Dispatches ab.
        if (!model.Settings.LoggingEnabled)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { LogSendError = null },
            [new SendLogProtocolEffect()]);
    }

    private static AppTransition UpdateSendLogProtocolCompleted(AppModel model, SendLogProtocolCompletedMessage message)
    {
        return new AppTransition(
            model with { LogSendError = message.Error },
            []);
    }

    private static AppTransition UpdateServersLoaded(AppModel model, ServersLoadedMessage message)
    {
        var updated = model with
        {
            KnownServers = message.Servers,
            ActiveServerAddress = message.ActiveAddress,
            // Die Pairing-Erforderlichkeit wird am App-Rand ermittelt und
            // über die Nachricht übernommen; ein angezeigter Pairing-Fehler
            // (z. B. DeviceRevokedError) bleibt beim Nachladen bestehen.
            // Ein aktiver Re-Pairing-Zustand (Gate-Ablehnung → Einrichtungs-
            // seite) darf dabei nicht verloren gehen: die erneute Ermittlung
            // würde einen vorhandenen, aber vom Gate abgelehnten statischen
            // Schlüssel wieder als ausreichend werten und die Pairing-
            // Eingabe mitsamt der Fehlermeldung ausblenden.
            IsPairingRequired = message.PairingRequired
                || (model.IsPairingRequired
                    && model.ConnectionState == ConnectionState.NeedsServerSetup)
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
        // Eine neue Serverprüfung ersetzt den Pairing-/Bootstrap-Kontext —
        // ein alter Fehler oder ein hängender Zustand darf nicht über den
        // nächsten Check hinweg sichtbar bleiben.
        return new AppTransition(
            model with
            {
                IsServerCheckRunning = true,
                ServerSetupError = null,
                IsPairingRequired = false,
                IsPairingRunning = false,
                PairingError = null,
                IsBootstrapRunning = false,
                BootstrapError = null
            },
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
                    ServerSetupError = UnreachableServerError,
                    IsPairingRequired = false,
                    IsPairingRunning = false,
                    PairingError = null,
                    IsBootstrapRunning = false,
                    BootstrapError = null
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

        if (message.PairingRequired)
        {
            // Der Server ist erreichbar und geprüft, verlangt aber ein
            // Geräte-Pairing (kein Geräte-Token und kein statischer
            // Gate-Schlüssel): die Pairing-Eingabe der Einrichtungsseite
            // wird sichtbar — ohne automatische Anmeldung. Der geprüfte
            // Server wird dennoch persistiert.
            var updated = model with
            {
                IsServerCheckRunning = false,
                ServerSetupError = null,
                ActiveServerAddress = normalized,
                KnownServers = known,
                ServerHealth = ServerHealthStatus.Reachable,
                IsPairingRequired = true,
                IsPairingRunning = false,
                PairingError = null,
                IsBootstrapRunning = false,
                BootstrapError = null,
                ConnectionState = ConnectionState.NeedsServerSetup,
                CurrentPage = AppPage.ServerSetup
            };
            var effects = new List<AppEffect> { new PersistActiveServerEffect(normalized, message.DisplayName) };
            if (model.CurrentPage != AppPage.ServerSetup)
            {
                effects.Add(new NavigateToPageEffect(AppPage.ServerSetup));
            }

            return new AppTransition(updated, effects);
        }

        // Der Server wird übernommen; die (automatische) Anmeldung am neuen
        // Server entscheidet über das Navigationsziel — „Home" wird nur aus
        // Connected/Offline erreicht. IsServerCheckRunning bleibt bis zum
        // Login-Abschluss gesetzt (Anzeige läuft weiter). Quellen- und
        // Browsing-Zustand des bisherigen Servers verlieren ihre Gültigkeit —
        // EnterConnected lädt den neuen Stand.
        return new AppTransition(
            model with
            {
                IsServerCheckRunning = true,
                ServerSetupError = null,
                ActiveServerAddress = normalized,
                KnownServers = known,
                ServerHealth = ServerHealthStatus.Reachable,
                ConnectionState = ConnectionState.Connecting,
                IsPairingRequired = false,
                IsPairingRunning = false,
                PairingError = null,
                IsBootstrapRunning = false,
                BootstrapError = null,
                SourcesOverview = SourcesOverviewState.Initial,
                SourceBrowser = SourceBrowserState.Empty,
                // Auch das servergenerierte Hero-Bild gehört zum
                // bisherigen Server — der neue Stand folgt über
                // EnterConnected.
                HomeHero = HomeHeroState.Empty
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

            if (message.PairingRequired)
            {
                // Erreichbar, aber ohne nutzbaren Gate-Schlüssel →
                // Pairing-Eingabe auf der Einrichtungsseite.
                var updated = model with
                {
                    IsStartupRunning = false,
                    ServerHealth = ServerHealthStatus.Reachable,
                    IsPairingRequired = true,
                    PairingError = null,
                    ConnectionState = ConnectionState.NeedsServerSetup,
                    CurrentPage = AppPage.ServerSetup
                };
                return model.CurrentPage == AppPage.ServerSetup
                    ? new AppTransition(updated, [])
                    : new AppTransition(updated, [new NavigateToPageEffect(AppPage.ServerSetup)]);
            }

            // Erreichbar → automatische Anmeldung mit gespeicherten Zugangsdaten.
            return new AppTransition(
                model with
                {
                    ServerHealth = ServerHealthStatus.Reachable,
                    IsPairingRequired = false,
                    PairingError = null
                },
                [new AttemptAutoLoginEffect(model.ActiveServerAddress)]);
        }

        // Nicht erreichbar → Offline-Daten-Bestand entscheidet zwischen
        // Offline-Zustand und Serverauswahl.
        return new AppTransition(
            model with
            {
                ServerHealth = ServerHealthStatus.Unreachable,
                IsPairingRequired = false,
                IsPairingRunning = false,
                PairingError = null,
                IsBootstrapRunning = false,
                BootstrapError = null
            },
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

    private static AppTransition UpdatePairingCodeSubmitted(AppModel model, PairingCodeSubmittedMessage message)
    {
        if (model.IsPairingRunning)
        {
            // Doppel-Submit bei laufendem Austausch ignorieren — ein zweiter
            // ExchangePairingCodeEffect würde die Seiteneffekte des ersten
            // (Provider-Aktivierung, API-Client-Schlüssel) überholen.
            return AppTransition.Unchanged(model);
        }

        if (!PairingCode.TryNormalize(message.Code, out var code))
        {
            // Lokale Formatprüfung: kein Server-Kontakt — die Eingabe ist
            // unvollständig oder falsch formatiert (nicht „abgelaufen").
            return new AppTransition(
                model with { PairingError = PairingCodeFormatError },
                []);
        }

        if (model.ActiveServerAddress is null)
        {
            // Defensiv: Pairing ohne aktiven Server → Einrichtung.
            return GoToServerSetup(model, model.ServerHealth);
        }

        return new AppTransition(
            model with { IsPairingRunning = true, PairingError = null },
            [new ExchangePairingCodeEffect(model.ActiveServerAddress, code)]);
    }

    private static AppTransition UpdatePairingCompleted(AppModel model, PairingCompletedMessage message)
    {
        if (!model.IsPairingRunning)
        {
            // Verspätetes Ergebnis nach Serverwechsel/neuem Check —
            // der Pairing-Kontext wurde bereits ersetzt.
            return AppTransition.Unchanged(model);
        }

        if (message.Status == PairingStatus.Succeeded)
        {
            var updated = model with
            {
                IsPairingRunning = false,
                IsPairingRequired = false,
                PairingError = null,
                ConnectionState = ConnectionState.Connecting
            };
            return model.ActiveServerAddress is null
                ? new AppTransition(updated, [])
                : new AppTransition(updated, [new AttemptAutoLoginEffect(model.ActiveServerAddress)]);
        }

        // Fehler bleiben inline auf der Pairing-Eingabe — der Benutzer
        // kann den Code korrigiert erneut senden. Es gibt bewusst keinen
        // Fallback auf einen statischen Token (auch nicht bei einem
        // Server ohne Pairing-Endpunkt).
        return new AppTransition(
            model with
            {
                IsPairingRunning = false,
                PairingError = PairingErrorFor(message.Status)
            },
            []);
    }

    private static string PairingErrorFor(PairingStatus status) => status switch
    {
        PairingStatus.InvalidCode => InvalidPairingCodeError,
        PairingStatus.RateLimited => PairingRateLimitedError,
        PairingStatus.NotSupported => PairingNotSupportedError,
        PairingStatus.ServerUnreachable => LoginUnreachableError,
        _ => PairingFailedError
    };

    private static AppTransition UpdateBootstrapTicketSubmitted(AppModel model, BootstrapTicketSubmittedMessage message)
    {
        if (model.IsBootstrapRunning)
        {
            // Doppel-Submit bei laufendem Bootstrap ignorieren (Muster:
            // UpdatePairingCodeSubmitted).
            return AppTransition.Unchanged(model);
        }

        if (!BootstrapTicketParser.TryParse(message.Text, out var ticket) || ticket is null)
        {
            // Lokale Prüfung: kein Server-Kontakt — die Eingabe ist keine
            // lesbare Ticket-Adresse und kein Kurzcode.
            return new AppTransition(
                model with { BootstrapError = InvalidBootstrapInputError },
                []);
        }

        if (ticket.Kind == BootstrapTicketKind.Url)
        {
            // Die Ticket-Adresse trägt den Server selbst: zuerst die
            // Erreichbarkeit prüfen — erst danach wird der Server
            // übernommen und der Bootstrap-Austausch gestartet.
            return new AppTransition(
                model with { IsBootstrapRunning = true, BootstrapError = null },
                [new CheckBootstrapServerEffect(ticket.ServerAddress!, ticket.Ticket)]);
        }

        // Kurzcode: wird gegen den bereits verbundenen (geprüften)
        // Server eingelöst — ohne aktiven Server ist die Eingabe
        // unvollständig, nicht ungültig.
        if (model.ActiveServerAddress is null)
        {
            return new AppTransition(
                model with { BootstrapError = BootstrapNeedsServerError },
                []);
        }

        return new AppTransition(
            model with { IsBootstrapRunning = true, BootstrapError = null },
            [new ExchangeBootstrapTicketEffect(model.ActiveServerAddress, ticket.Ticket)]);
    }

    private static AppTransition UpdateBootstrapServerChecked(AppModel model, BootstrapServerCheckedMessage message)
    {
        if (!model.IsBootstrapRunning)
        {
            // Verspätetes Prüfergebnis — der Bootstrap-Kontext wurde
            // bereits ersetzt (Muster: UpdatePairingCompleted).
            return AppTransition.Unchanged(model);
        }

        if (!message.Success)
        {
            return new AppTransition(
                model with
                {
                    IsBootstrapRunning = false,
                    BootstrapError = BootstrapServerUnreachableError
                },
                []);
        }

        // Der Server aus der Ticket-Adresse ist erreichbar: als aktiven
        // Server übernehmen und in die bekannten Server aufnehmen
        // (PersistActiveServerEffect läuft vor dem Austausch — der
        // Gate-Provider steht dann bereit), dann erst das Ticket einlösen.
        var normalized = ServerAddress.Normalize(message.Address);
        var server = new KnownServer
        {
            Address = normalized,
            DisplayName = string.Empty,
            LastUsedAtUtc = DateTime.UtcNow
        };

        var known = model.KnownServers
            .Where(s => !string.Equals(s.Address, normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();
        known.Insert(0, server);

        return new AppTransition(
            model with
            {
                IsBootstrapRunning = true,
                BootstrapError = null,
                ActiveServerAddress = normalized,
                KnownServers = known,
                ServerHealth = ServerHealthStatus.Reachable
            },
            [new PersistActiveServerEffect(normalized, null), new ExchangeBootstrapTicketEffect(normalized, message.Ticket)]);
    }

    private static AppTransition UpdateBootstrapCompleted(AppModel model, BootstrapCompletedMessage message)
    {
        if (!model.IsBootstrapRunning)
        {
            // Verspätetes Ergebnis — der Bootstrap-Kontext wurde ersetzt.
            return AppTransition.Unchanged(model);
        }

        if (message.Status == BootstrapStatus.Succeeded)
        {
            // Das Session-Paket liegt bereits in der sicheren Ablage —
            // die automatische Anmeldung validiert das Token und führt
            // über EnterConnected direkt zur Startseite (keine Adress-/
            // Passworteingabe).
            var updated = model with
            {
                IsBootstrapRunning = false,
                IsPairingRequired = false,
                PairingError = null,
                BootstrapError = null,
                ConnectionState = ConnectionState.Connecting
            };
            return model.ActiveServerAddress is null
                ? new AppTransition(updated, [])
                : new AppTransition(updated, [new AttemptAutoLoginEffect(model.ActiveServerAddress)]);
        }

        // Fehler bleiben inline auf der Kopplungs-Eingabe — der Benutzer
        // kann den Code korrigiert erneut senden.
        return new AppTransition(
            model with
            {
                IsBootstrapRunning = false,
                BootstrapError = BootstrapErrorFor(message.Status)
            },
            []);
    }

    private static string BootstrapErrorFor(BootstrapStatus status) => status switch
    {
        BootstrapStatus.InvalidTicket => InvalidBootstrapTicketError,
        BootstrapStatus.InvalidRequest => InvalidBootstrapTicketError,
        BootstrapStatus.RateLimited => BootstrapRateLimitedError,
        BootstrapStatus.NotSupported => BootstrapNotSupportedError,
        BootstrapStatus.ServerUnreachable => BootstrapServerUnreachableError,
        _ => BootstrapFailedError
    };

    private static AppTransition UpdateDeviceTokenRevoked(AppModel model)
    {
        if (model.ConnectionState == ConnectionState.NeedsServerSetup && model.IsPairingRequired)
        {
            // Bereits im Re-Pairing-Zustand — Doppelauslösung ignorieren.
            return AppTransition.Unchanged(model);
        }

        var updated = model with
        {
            IsStartupRunning = false,
            IsServerCheckRunning = false,
            IsPairingRunning = false,
            IsBootstrapRunning = false,
            BootstrapError = null,
            IsLoginRunning = false,
            IsPairingRequired = true,
            // Das Gate hat geantwortet — der Server ist erreichbar.
            ServerHealth = ServerHealthStatus.Reachable,
            ServerSetupError = null,
            PairingError = DeviceRevokedError,
            ConnectionState = ConnectionState.NeedsServerSetup,
            CurrentPage = AppPage.ServerSetup,
            // Quellen- und Browsing-Zustand gehören zur abgelehnten
            // Sitzung — sie dürfen nicht sichtbar bleiben (Muster
            // UpdateSessionExpired).
            SourcesOverview = SourcesOverviewState.Initial,
            SourceBrowser = SourceBrowserState.Empty,
            HomeHero = HomeHeroState.Empty
        };
        var effects = new List<AppEffect> { new DisconnectServerEventsEffect() };
        if (model.CurrentPage != AppPage.ServerSetup)
        {
            effects.Add(new NavigateToPageEffect(AppPage.ServerSetup));
        }

        return new AppTransition(updated, effects);
    }

    private static AppTransition UpdateAutoLoginCompleted(AppModel model, AutoLoginCompletedMessage message)
    {
        if (message.Status == AuthAttemptStatus.Succeeded)
        {
            return EnterConnected(model with { LoginUsername = message.Username ?? model.LoginUsername });
        }

        if (message.Status == AuthAttemptStatus.GateRejected)
        {
            // Das Client-Gate hat den Schlüssel abgelehnt (widerrufenes
            // Geräte-Token). Die Ablehnung wurde am Rand bereits als
            // DeviceTokenRevokedMessage gemeldet — das verspätete
            // Auto-Login-Ergebnis darf den Re-Pairing-Zustand nicht auf
            // die Anmeldemaske übersteuern (dort scheitert jede Anmeldung
            // am Gate). Der Übergang ist idempotent zum Widerrufs-Übergang.
            return UpdateDeviceTokenRevoked(model);
        }

        // Fehlende oder fehlgeschlagene Zugangsdaten → Anmeldemaske.
        // Steht die Servereinrichtung im Fokus (der Nutzer hat gerade
        // verbunden), bleibt der Fluss auf derselben Seite — sie bietet
        // als zweite Stufe die Wahl zwischen Kurzcode und Zugangsdaten.
        var stayOnSetup = model.CurrentPage == AppPage.ServerSetup;
        var updated = model with
        {
            IsStartupRunning = false,
            IsServerCheckRunning = false,
            IsLoginRunning = false,
            LoginUsername = message.Username ?? model.LoginUsername,
            // Fehlende Zugangsdaten (z. B. nach einer Wiederherstellung
            // aus dem Cloud-Backup — Geheimnisse werden nicht gesichert)
            // erhalten einen erklärenden Hinweis statt eines Fehlers.
            LoginError = LoginErrorFor(message.Status) ?? ReloginRequiredNotice,
            ConnectionState = ConnectionState.NeedsLogin,
            CurrentPage = stayOnSetup ? AppPage.ServerSetup : AppPage.Login
        };
        return model.CurrentPage == AppPage.Login || stayOnSetup
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

        if (message.Status == AuthAttemptStatus.GateRejected)
        {
            // Gate-Ablehnung bei manueller Anmeldung (widerrufenes
            // Geräte-Token) → Re-Pairing-Eingabe statt Anmeldefehler.
            return UpdateDeviceTokenRevoked(model);
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
    /// Übergang in den verbundenen Zustand: lädt die Quellen des
    /// Navigationsmenüs, baut die Ereignisverbindung auf und startet die
    /// vollständige Synchronisation. Die Startseite wird nur von
    /// Workflow-Seiten aus angesteuert — bei einer Wiederverbindung zur
    /// Laufzeit bleibt der Anwender auf der aktuellen Menüseite.
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
            IsSyncRunning = true,
            // Die Quellen des Navigationsmenüs werden direkt mit dem
            // Verbindungsaufbau geladen — die bisherigen Einträge
            // bleiben während des Ladens sichtbar (kein Menü-Flackern).
            SourcesOverview = model.SourcesOverview with { IsLoading = true, Error = null }
        };

        var effects = new List<AppEffect> { new LoadSourcesEffect() };
        if (model.ActiveServerAddress is not null)
        {
            effects.Add(new ConnectServerEventsEffect(model.ActiveServerAddress));
        }

        effects.Add(new RunFullSyncEffect());

        // Unterbrochene Downloads (App-Neustart, Netzwerkverlust) werden
        // mit der Verbindung wieder aufgenommen — der Auftragsbestand
        // liegt persistent im Manifest.
        effects.Add(new ResumeDownloadsEffect(model.Settings));

        // Der Hero-Kopfbereich der Startseite lädt sein servergeneriertes
        // Hintergrundbild einmalig je Verbindung — sofern die Einstellung
        // aktiviert ist und noch kein Bild vorliegt.
        updated = WithHeroLoad(updated, effects);

        // Eine im Offline-Modus geöffnete Detailseite hatte keine
        // Bildquelle zum Abruf — mit der Verbindung wird das Kopfbild
        // nachgeladen, sofern noch keins vorliegt.
        updated = WithDetailsHeaderLoad(updated, effects);

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
            // Offline steht keine Quellenliste zur Verfügung — die
            // Menü-Einträge werden mit dem Übergang zurückgesetzt,
            // damit kein veralteter Stand als aktuell erscheint.
            return model.ConnectionState == ConnectionState.Connected
                ? new AppTransition(
                    updated with
                    {
                        ConnectionState = ConnectionState.Offline,
                        SourcesOverview = SourcesOverviewState.Initial
                    },
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

        // Bereits verbunden: die Online-Meldung kann auch ein Profil-
        // wechsel (Mobilfunk → WLAN) sein — wartende Downloads werden
        // bei erfüllter WLAN-Bedingung fortgesetzt.
        return model.ConnectionState == ConnectionState.Connected
            ? new AppTransition(
                model with { IsOnline = true },
                [new ResumeDownloadsEffect(model.Settings)])
            : new AppTransition(model with { IsOnline = true }, []);
    }

    private static AppTransition UpdateSynchronizationCompleted(AppModel model)
    {
        // Nach dem Voll-Sync die Startseiten-Listen aus dem aktualisierten
        // Cache neu laden; bei aktivierten automatischen Downloads läuft
        // der Vorlauf auf dem frischen Weiterschauen-Stand. Der Sync kann
        // Freigaben/Entzüge geändert haben — die Quellen des
        // Navigationsmenüs werden ebenfalls neu geladen (die
        // IsLoading-Sperre verhindert überlagerte Läufe).
        var updated = model with { IsSyncRunning = false };
        var effects = new List<AppEffect> { new LoadHomeListsEffect() };
        updated = WithSourcesReload(updated, effects);

        if (model.ConnectionState == ConnectionState.Connected && model.Settings.AutoDownloadEnabled)
        {
            effects.Add(new RunAutoDownloadsEffect(model.Settings, Force: false));
        }

        return new AppTransition(updated, effects);
    }

    private static AppTransition UpdateCachedListsChanged(AppModel model)
    {
        // Teil-Synchronisationen (Server-Ereignisse) ändern die
        // Weiterschauen-Liste — der Vorlauf folgt dem neuen Stand.
        // Aktualisierte Quellen-Freigaben werden fürs Navigationsmenü
        // nachgeladen (die IsLoading-Sperre verhindert überlagerte Läufe).
        var effects = new List<AppEffect> { new LoadHomeListsEffect() };
        var updated = WithSourcesReload(model, effects);

        if (model.ConnectionState == ConnectionState.Connected && model.Settings.AutoDownloadEnabled)
        {
            effects.Add(new RunAutoDownloadsEffect(model.Settings, Force: false));
        }

        return new AppTransition(updated, effects);
    }

    /// <summary>
    /// Reiht das (Neu-)Laden der Menü-Quellen ein, wenn die App verbunden
    /// ist und kein Ladevorgang läuft: <see cref="SourcesOverviewState"/>
    /// erhält <c>IsLoading = true</c> (ein gesetzter Fehler wird
    /// zurückgesetzt, der Neuversuch soll nicht als Fehlerstand
    /// erscheinen) und ein <see cref="LoadSourcesEffect"/> wird an
    /// <paramref name="effects"/> angehängt.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="effects">Effektliste des Übergangs, an die der Ladeeffekt angehängt wird.</param>
    /// <returns>Das ggf. aktualisierte Model; unverändert, wenn kein Ladevorgang angestoßen wird.</returns>
    private static AppModel WithSourcesReload(AppModel model, List<AppEffect> effects)
    {
        if (model.ConnectionState != ConnectionState.Connected || model.SourcesOverview.IsLoading)
        {
            return model;
        }

        effects.Add(new LoadSourcesEffect());
        return model with
        {
            SourcesOverview = model.SourcesOverview with { IsLoading = true, Error = null }
        };
    }

    /// <summary>
    /// Reiht das Laden des Hero-Hintergrundbilds ein, wenn die App
    /// verbunden ist, der Kopfbereich aktiviert ist und weder ein
    /// Ladevorgang läuft noch ein Bild vorliegt:
    /// <see cref="HomeHeroState"/> erhält <c>IsLoading = true</c> und ein
    /// <see cref="LoadHeroBackgroundEffect"/> wird an
    /// <paramref name="effects"/> angehängt. Nach einem Fehlschlag greift
    /// die Bedingung beim nächsten Verbindungsaufbau erneut.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="effects">Effektliste des Übergangs, an die der Ladeeffekt angehängt wird.</param>
    /// <returns>Das ggf. aktualisierte Model; unverändert, wenn kein Ladevorgang angestoßen wird.</returns>
    private static AppModel WithHeroLoad(AppModel model, List<AppEffect> effects)
    {
        if (model.ConnectionState != ConnectionState.Connected
            || !model.Settings.HomeHeroEnabled
            || model.HomeHero.IsLoading
            || model.HomeHero.HasImage)
        {
            return model;
        }

        effects.Add(new LoadHeroBackgroundEffect());
        return model with
        {
            HomeHero = model.HomeHero with { IsLoading = true }
        };
    }

    private static AppTransition UpdateHeroBackgroundLoaded(AppModel model, HeroBackgroundLoadedMessage message)
    {
        // Verspätete Antworten eines abgelösten Ladevorgangs (Abmeldung,
        // Sitzungsablauf, Serverwechsel — der Zustand wurde dort bereits
        // zurückgesetzt) dürfen kein sitzungsfremdes Bild hinterlassen.
        // Ohne laufenden Ladevorgang ist die Nachricht überholt; im
        // Offline-Übergang bleibt <c>IsLoading</c> bestehen, sodass ein
        // dort eintreffendes Ergebnis noch gilt.
        if (!model.HomeHero.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        // Ein Fehlschlag (null) löscht kein vorhandenes Bild — und ist
        // ohnehin kein Fehlerzustand: der Kopfbereich zeigt dann nur
        // Gradienten und Texte (Web-Verhalten).
        return new AppTransition(
            model with
            {
                HomeHero = model.HomeHero with
                {
                    IsLoading = false,
                    ImageBytes = message.ImageBytes is { Length: > 0 }
                        ? message.ImageBytes
                        : model.HomeHero.ImageBytes
                }
            },
            []);
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
            CurrentPage = AppPage.Login,
            // Quellen und Browsing-Zustand gehören zur abgelaufenen
            // Sitzung — sie dürfen für den nächsten Benutzer nicht
            // sichtbar bleiben. Das benutzerbezogen generierte
            // Hero-Bild ebenfalls nicht.
            SourcesOverview = SourcesOverviewState.Initial,
            SourceBrowser = SourceBrowserState.Empty,
            HomeHero = HomeHeroState.Empty
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
        var updated = model with
        {
            ContinueWatching = message.Lists.ContinueWatching,
            Favorites = message.Lists.Favorites,
            RecentEntries = message.Lists.Recent
        };

        // Verworfene (beschädigte) Caches werden bei bestehender
        // Verbindung neu synchronisiert — der Sync schreibt die
        // Cache-Schlüssel anschließend frisch.
        return message.Lists.DiscardedCaches.Count > 0
            && model.ConnectionState == ConnectionState.Connected
            && !model.IsSyncRunning
            ? new AppTransition(updated with { IsSyncRunning = true }, [new RunFullSyncEffect()])
            : new AppTransition(updated, []);
    }

    private static AppTransition UpdateStorageRecovered(AppModel model, StorageRecoveredMessage message)
    {
        return new AppTransition(model with { StorageNotice = message.Notice }, []);
    }

    private static AppTransition UpdateMediaItemSelected(AppModel model, MediaItemSelectedMessage message)
    {
        var item = message.Item;
        if (item.Id <= 0 || string.IsNullOrWhiteSpace(item.MediaType) || !item.IsSelectable)
        {
            // Ohne identifizierbares Medium keine Navigation; Platzhalter
            // fehlender Offline-Episoden sind nicht auswählbar.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model,
            [new NavigateToMediaDetailsEffect(
                item.MediaType, item.Id, item.Title,
                item.ShowId, item.SeasonId, item.EpisodeId)]);
    }

    /// <summary>
    /// Auswahl eines Weiterschauen-Eintrags: abspielbare Medientypen
    /// (<c>movie</c>/<c>tvshowepisode</c>) öffnen die Titel-Detailansicht
    /// und starten darüber die Wiedergabe — „Player schließen" kehrt zur
    /// Detailseite zurück statt zur Startliste (Parität zur
    /// Serveranwendung); bei einer Episode mit bekanntem Serienkontext
    /// wird die Fortsetzungsliste zusätzlich per Datenabruf nachgeladen.
    /// Ausnahme Offline: eine Episode ohne Serien-Kennung kann die
    /// Detailansicht nicht im Serienkontext aufbauen und behält die
    /// bisherige Direktwiedergabe. Alle übrigen Einträge öffnen weiterhin
    /// die Detailseite.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Auswahl-Nachricht.</param>
    /// <returns>Übergang zu Detailseite und Wiedergabestart bzw. nur zur Detailseite.</returns>
    private static AppTransition UpdateContinueWatchingItemSelected(AppModel model, ContinueWatchingItemSelectedMessage message)
    {
        var item = message.Item;
        if (!IsSelectableEntry(item))
        {
            // Ohne identifizierbares Medium keine Aktion; Platzhalter
            // fehlender Offline-Episoden sind nicht auswählbar.
            return AppTransition.Unchanged(model);
        }

        var mediaType = MediaTypeNames.Normalize(item.MediaType);
        if (mediaType is not ("movie" or "tvshowepisode"))
        {
            // Nicht direkt abspielbare Typen (Serie, Staffel, Kollektion)
            // behalten die Detail-Navigation.
            return new AppTransition(
                model,
                [new NavigateToMediaDetailsEffect(
                    item.MediaType, item.Id, item.Title,
                    item.ShowId, item.SeasonId, item.EpisodeId)]);
        }

        if (item.PlaylistId is > 0 && item.PlaylistEntryId is > 0)
        {
            // Playlist-gebundener Weiterschauen-Eintrag: Wiedergabe und
            // Folgevideo laufen über den serverseitigen Playlist-Kontext
            // (Reihenfolge der Playlist) statt über den Serienkontext.
            return new AppTransition(
                model,
                [new StartPlaylistPlaybackEffect(item.PlaylistId.Value, item.PlaylistEntryId.Value)]);
        }

        var transition = StartPlayback(model, mediaType, item.Id, item.Title);
        var effects = new List<AppEffect>();
        if (!model.IsOffline || mediaType != "tvshowepisode" || item.ShowId is > 0)
        {
            // Detailansicht unter die Wiedergabe legen — Effekte laufen
            // sequenziell, die Detailseite wird daher vor dem Player auf
            // den Stack geschoben. Offline nur mit nutzbarem Kontext:
            // eine Episode ohne Serien-Kennung kann die Offline-
            // Detailansicht nicht aufbauen und behält die Direktwiedergabe
            // (Filme sind über den Manifesteintrag selbst ihr Kontext).
            effects.Add(new NavigateToMediaDetailsEffect(
                item.MediaType, item.Id, item.Title,
                item.ShowId, item.SeasonId, item.EpisodeId));
        }

        effects.AddRange(transition.Effects);
        if (mediaType == "tvshowepisode" && item.ShowId is > 0)
        {
            // Der Serienkontext für die Folgevideo-Funktion liegt zum
            // Startzeitpunkt noch nicht im Model — er wird unabhängig per
            // Datenabruf geladen.
            effects.Add(new LoadUpcomingEpisodesEffect(item.Id, item.ShowId.Value));
        }

        return transition with { Effects = effects };
    }

    /// <summary>
    /// Kontextmenü „Ausblenden" auf einem Weiterschauen-Eintrag: der
    /// Eintrag wird über den Mutations-Effekt ausgeblendet (online
    /// Server-Endpunkt + Cache-Erneuerung, offline lokales Entfernen) —
    /// dieselben Guards wie die Eintragsauswahl.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Ausblenden-Anforderung.</param>
    /// <returns>Übergang mit dem Hide-Effekt bzw. unverändertem Model.</returns>
    private static AppTransition UpdateContinueWatchingHideRequested(AppModel model, ContinueWatchingHideRequestedMessage message)
    {
        var item = message.Item;
        if (!IsSelectableEntry(item))
        {
            // Ohne identifizierbares Medium keine Aktion; Platzhalter
            // fehlender Offline-Episoden sind nicht ausblendbar.
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model,
            [new HideContinueWatchingEntryEffect(item.MediaType, item.Id)]);
    }

    /// <summary>
    /// Kontextmenü „Überspringen" auf einem Weiterschauen-Eintrag: der
    /// Eintrag wird über den Mutations-Effekt übersprungen (online
    /// Server-Endpunkt + Cache-Erneuerung, offline lokales Entfernen) —
    /// dieselben Guards wie die Eintragsauswahl.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Überspringen-Anforderung.</param>
    /// <returns>Übergang mit dem Skip-Effekt bzw. unverändertem Model.</returns>
    private static AppTransition UpdateContinueWatchingSkipRequested(AppModel model, ContinueWatchingSkipRequestedMessage message)
    {
        var item = message.Item;
        if (!IsSelectableEntry(item))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model,
            [new SkipContinueWatchingEntryEffect(item.MediaType, item.Id)]);
    }

    /// <summary>
    /// Verwertbarer Weiterschauen-Eintrag für Auswahl und Kontextmenü-
    /// Mutationen: identifizierbares Medium (Id + Typ), kein Platzhalter
    /// fehlender Offline-Episoden.
    /// </summary>
    /// <param name="item">Der zu prüfende Eintrag.</param>
    /// <returns><c>true</c>, wenn der Eintrag eine Aktion zulässt.</returns>
    private static bool IsSelectableEntry(HomeMediaItem item) =>
        item.Id > 0 && !string.IsNullOrWhiteSpace(item.MediaType) && item.IsSelectable;

    /// <summary>
    /// Übernimmt die nachgeladenen Folge-Episoden in den Player-Zustand —
    /// nur wenn der Player noch dieselbe Episode spielt (Antworten
    /// abgelösender Identitäten werden wie bei
    /// <see cref="UpdatePlaybackSourceResolved"/> verworfen).
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Ergebnisnachricht der Serienkontext-Ladung.</param>
    /// <returns>Übergang mit befüllter Fortsetzungsliste bzw. unverändertem Model.</returns>
    private static AppTransition UpdateUpcomingEpisodesLoaded(AppModel model, UpcomingEpisodesLoadedMessage message)
    {
        var player = model.Player;
        if (!player.IsActive
            || player.MediaId != message.MediaId
            || player.MediaType != "tvshowepisode")
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { Player = player with { UpcomingEpisodes = message.Episodes } },
            []);
    }

    private static AppTransition UpdateSourcesLoaded(AppModel model, SourcesLoadedMessage message)
    {
        // Verspätete Antworten eines abgelösten Ladevorgangs (Offline-
        // Übergang, Abmeldung, Serverwechsel) dürfen die Quellenliste
        // nicht wieder befüllen — das Menü spiegelt nur den verbundenen
        // Zustand.
        if (model.ConnectionState != ConnectionState.Connected)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                SourcesOverview = new SourcesOverviewState
                {
                    Attempted = true,
                    // Bei Fehlschlag bleibt der bisherige Bestand sichtbar —
                    // der Effekt-Handler dispatcht im Fehlerfall eine leere
                    // Liste; das Menü zeigt die Retry-Zeile über den
                    // weiterhin antippbaren Einträgen statt sie abzuräumen.
                    Sources = message.Error is null
                        ? message.Sources
                        : model.SourcesOverview.Sources,
                    Error = message.Error
                }
            },
            []);
    }

    private static AppTransition UpdateSourcesReloadRequested(AppModel model)
    {
        // Manueller Neuversuch aus dem Navigationsmenü (Fehler-Eintrag):
        // außerhalb des verbundenen Zustands oder bei laufendem
        // Ladevorgang bleibt die Anforderung wirkungslos.
        var effects = new List<AppEffect>();
        var updated = WithSourcesReload(model, effects);
        return ReferenceEquals(updated, model)
            ? AppTransition.Unchanged(model)
            : new AppTransition(updated, effects);
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
        var effects = new List<AppEffect>();
        var updated = model with
        {
            MediaDetails = state with
            {
                IsLoading = false,
                Attempted = true,
                Details = message.Details,
                Error = error
            }
        };
        return new AppTransition(WithDetailsHeaderLoad(updated, effects), effects);
    }

    /// <summary>
    /// Stößt das Laden des Kopfbereichs-Bilds an, wenn die geladenen
    /// Details eine Bildquelle benennen (Episoden-Still oder Bild-Id),
    /// die Verbindung steht und weder ein Ladevorgang läuft noch ein
    /// Bild vorliegt. Ohne Quelle bzw. offline bleibt der Kopfbereich
    /// bei Gradienten und Texten (Web-Verhalten); nach einem Fehlschlag
    /// greift die Bedingung beim nächsten Verbindungsaufbau erneut.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="effects">Effektliste des Übergangs, an die der Ladeeffekt angehängt wird.</param>
    /// <returns>Das ggf. aktualisierte Model; unverändert, wenn kein Ladevorgang angestoßen wird.</returns>
    private static AppModel WithDetailsHeaderLoad(AppModel model, List<AppEffect> effects)
    {
        var state = model.MediaDetails;
        var details = state.Details;
        if (model.ConnectionState != ConnectionState.Connected
            || details is null
            || (details.HeaderEpisodeId is not > 0 && details.HeaderPictureId is not > 0)
            || state.IsHeaderImageLoading
            || state.HasHeaderImage)
        {
            return model;
        }

        effects.Add(new LoadMediaDetailsHeaderEffect(
            details.MediaType, details.MediaId,
            details.HeaderPictureId, details.HeaderEpisodeId));
        return model with
        {
            MediaDetails = state with { IsHeaderImageLoading = true }
        };
    }

    private static AppTransition UpdateMediaDetailsHeaderImageLoaded(AppModel model, MediaDetailsHeaderImageLoadedMessage message)
    {
        var state = model.MediaDetails;
        var details = state.Details;
        // Verworfen, wenn die Seite zwischenzeitlich ein anderes Medium
        // oder eine andere Bildquelle anzeigt (Staffelwechsel während
        // des Abrufs).
        if (details is null
            || details.MediaType != MediaTypeNames.Normalize(message.MediaType)
            || details.MediaId != message.MediaId
            || details.HeaderPictureId != message.HeaderPictureId
            || details.HeaderEpisodeId != message.HeaderEpisodeId)
        {
            return AppTransition.Unchanged(model);
        }

        // Ein Fehlschlag (null) löscht kein vorhandenes Bild — der
        // Kopfbereich zeigt dann nur Gradienten und Texte (Web-Verhalten).
        return new AppTransition(
            model with
            {
                MediaDetails = state with
                {
                    IsHeaderImageLoading = false,
                    HeaderImageBytes = message.ImageBytes is { Length: > 0 }
                        ? message.ImageBytes
                        : state.HeaderImageBytes
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

        // Web-Parität (SelectSeason): die Staffelwahl wählt die erste
        // Episode der Staffel — der Kopfbereich folgt der Auswahl und
        // zeigt deren Still bzw. ohne Episode das Staffel-/Serien-Banner.
        var season = details.Seasons.FirstOrDefault(s => s.Id == message.SeasonId);
        var firstEpisode = season?.Episodes.OrderBy(e => e.Number).FirstOrDefault();
        var updatedDetails = details with
        {
            SelectedSeasonId = message.SeasonId,
            SelectedEpisodeId = firstEpisode?.Id
        };

        var headerChanged =
            updatedDetails.HeaderEpisodeId != details.HeaderEpisodeId
            || updatedDetails.HeaderPictureId != details.HeaderPictureId;

        var effects = new List<AppEffect>();
        var updated = model with
        {
            MediaDetails = model.MediaDetails with
            {
                Details = updatedDetails,
                // Bei geänderter Bildquelle das alte Bild verwerfen — der
                // Kopfbereich zeigt sonst kurz ein fremdes Still.
                HeaderImageBytes = headerChanged ? null : model.MediaDetails.HeaderImageBytes,
                IsHeaderImageLoading = headerChanged ? false : model.MediaDetails.IsHeaderImageLoading
            }
        };
        if (headerChanged)
        {
            updated = WithDetailsHeaderLoad(updated, effects);
        }

        return new AppTransition(updated, effects);
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

    private static AppTransition UpdateFavoritesPageOpened(AppModel model)
    {
        var state = model.FavoritesPage;
        if (state.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        // Die Seite lädt nur in den Zuständen mit anzeigbarer
        // Favoriten-Kopie: online erneuert der Effekt-Handler zuerst den
        // Cache vom Serverstand (führendes System), offline wird der
        // bisherige Cache-Stand gezeigt — beides landet in derselben
        // Liste; der Offline-Hinweis bindet an IsOffline.
        if (model.ConnectionState is not (ConnectionState.Connected or ConnectionState.Offline))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                FavoritesPage = state with
                {
                    IsLoading = true,
                    Attempted = true,
                    Error = null
                }
            },
            [new LoadFavoritesPageEffect(model.ConnectionState == ConnectionState.Connected)]);
    }

    private static AppTransition UpdateFavoritesPageLoaded(AppModel model, FavoritesPageLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                FavoritesPage = new FavoritesPageState
                {
                    Attempted = true,
                    Items = message.Items,
                    Error = message.Error
                }
            },
            []);
    }

    private static AppTransition UpdateFavoriteRemoveRequested(AppModel model, FavoriteRemoveRequestedMessage message)
    {
        var state = model.FavoritesPage;
        var favoriteId = message.Item.FavoriteId;
        // Entfernen ist nur online möglich (der Server ist führend) und
        // nur für einen Eintrag der geladenen Liste — ohne gültige
        // Favoriten-Id oder bei laufendem Lauf kein zweiter Aufruf.
        if (model.ConnectionState != ConnectionState.Connected
            || favoriteId <= 0
            || state.Items.All(i => i.FavoriteId != favoriteId)
            || state.Items.Any(i => i.FavoriteId == favoriteId && i.IsRemoving))
        {
            return AppTransition.Unchanged(model);
        }

        var items = state.Items
            .Select(i => i.FavoriteId == favoriteId ? i with { IsRemoving = true } : i)
            .ToList();
        return new AppTransition(
            model with { FavoritesPage = state with { Items = items, Error = null } },
            [new RemoveFavoriteEffect(favoriteId)]);
    }

    private static AppTransition UpdateFavoriteRemoveCompleted(AppModel model, FavoriteRemoveCompletedMessage message)
    {
        var state = model.FavoritesPage;
        if (message.Success)
        {
            // Der Server hat den Eintrag entfernt und die lokale Kopie
            // wurde bereits erneuert — der Eintrag verschwindet aus der
            // Liste; die Startseiten-Favoriten folgen über die vom
            // Effekt-Handler gemeldete CachedListsChangedMessage.
            return new AppTransition(
                model with
                {
                    FavoritesPage = state with
                    {
                        Items = state.Items
                            .Where(i => i.FavoriteId != message.FavoriteId)
                            .ToList(),
                        Error = null
                    }
                },
                []);
        }

        // Fehlschlag: der Eintrag bleibt in der Liste, der Laufzustand
        // wird zurückgesetzt und der Fehler angezeigt.
        var restored = state.Items
            .Select(i => i.FavoriteId == message.FavoriteId ? i with { IsRemoving = false } : i)
            .ToList();
        return new AppTransition(
            model with
            {
                FavoritesPage = state with
                {
                    Items = restored,
                    Error = "Der Favorit konnte nicht entfernt werden."
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistsPageOpened(AppModel model)
    {
        var state = model.PlaylistsPage;
        if (state.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        // Reines Online-Feature: ohne Serververbindung gibt es keinen
        // anzeigbaren Stand — die Seite bekommt eine verständliche
        // Fehleranzeige (kein Cache, keine veraltete Liste).
        if (model.ConnectionState != ConnectionState.Connected)
        {
            return new AppTransition(
                model with
                {
                    PlaylistsPage = state with
                    {
                        Attempted = true,
                        Error = PlaylistsOfflineError
                    }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistsPage = state with
                {
                    IsLoading = true,
                    Attempted = true,
                    Error = null
                }
            },
            [new LoadPlaylistsEffect()]);
    }

    private static AppTransition UpdatePlaylistsLoaded(AppModel model, PlaylistsLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                PlaylistsPage = model.PlaylistsPage with
                {
                    Items = message.Error is null
                        ? PlaylistsPageState.BuildItems(message.Own, message.Public)
                        : model.PlaylistsPage.Items,
                    IsLoading = false,
                    Error = message.Error
                }
            },
            []);
    }

    private static PlaylistDetailsState NewPlaylistDetails(PlaylistListItem item) => new()
    {
        PlaylistId = item.PlaylistId,
        Name = item.Name,
        Description = item.Description,
        SortMode = item.SortMode,
        CoverPictureId = item.CoverPictureId,
        IsOwner = item.IsOwner,
        IsPublic = item.IsPublic,
        GenresManuallyOverridden = item.GenresManuallyOverridden,
        AllGenreIds = item.AllGenreIds,
        GenreText = item.GenreText,
        IsLoading = true,
        HasMore = true
    };

    private static AppTransition UpdatePlaylistSelected(AppModel model, PlaylistSelectedMessage message)
    {
        var item = message.Item;
        if (item.PlaylistId <= 0)
        {
            return AppTransition.Unchanged(model);
        }

        // Die Auswahl ist nur online wirksam — ohne Verbindung würde die
        // Detailseite sofort im Fehlerzustand landen.
        if (model.ConnectionState != ConnectionState.Connected)
        {
            return AppTransition.Unchanged(model);
        }

        // Der Kopf-Nachschlag läuft mit: er frischt Besitzer-/Genre-/
        // Öffentlich-Felder auf und liefert den IsAdmin-Claim — Grundlage
        // der Verwaltungs-Schalter.
        return new AppTransition(
            model with { PlaylistDetails = NewPlaylistDetails(item) },
            [
                new NavigateToPlaylistDetailsEffect(item.PlaylistId, item.Name),
                new LoadPlaylistDetailsEffect(item.PlaylistId),
                new LoadPlaylistEntriesEffect(item.PlaylistId, 1, Append: false)
            ]);
    }

    private static AppTransition UpdatePlaylistDetailsOpened(AppModel model, PlaylistDetailsOpenedMessage message)
    {
        if (message.PlaylistId <= 0)
        {
            return AppTransition.Unchanged(model);
        }

        // Rückkehr auf die bereits geöffnete Playlist (z. B. zurück von
        // der Medien-Detailseite): Zustand und Listenposition bleiben
        // erhalten.
        if (model.PlaylistDetails.PlaylistId == message.PlaylistId)
        {
            return AppTransition.Unchanged(model);
        }

        // Wiederherstellungspfad: Seite ohne passenden Zustand geöffnet →
        // Kopfdaten und erste Eintragsseite laden (ohne erneute
        // Navigation). Der Kopf-Nachschlag ist nicht fatal — schlägt er
        // fehl, bleibt der Pfad-Name als Titel stehen.
        return new AppTransition(
            model with
            {
                PlaylistDetails = new PlaylistDetailsState
                {
                    PlaylistId = message.PlaylistId,
                    Name = message.Name,
                    IsLoading = true,
                    HasMore = true
                }
            },
            [
                new LoadPlaylistDetailsEffect(message.PlaylistId),
                new LoadPlaylistEntriesEffect(message.PlaylistId, 1, Append: false)
            ]);
    }

    private static AppTransition UpdatePlaylistDetailsLoaded(AppModel model, PlaylistDetailsLoadedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive || details.PlaylistId != message.PlaylistId)
        {
            return AppTransition.Unchanged(model);
        }

        // Der Admin-Claim gehört zur Nachricht auch ohne Kopfdaten —
        // er steuert die Sichtbarkeit des Öffentlich-Schalters.
        if (message.Info is null)
        {
            return new AppTransition(
                model with { PlaylistDetails = details with { IsAdmin = message.IsAdmin } },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    Name = message.Info.Name,
                    Description = message.Info.Description,
                    SortMode = message.Info.SortMode,
                    CoverPictureId = message.Info.CoverPictureId,
                    IsOwner = message.Info.IsOwner,
                    IsPublic = message.Info.IsPublic,
                    IsAdmin = message.IsAdmin,
                    GenresManuallyOverridden = message.Info.GenresManuallyOverridden,
                    AllGenreIds = message.Info.AllGenreIds,
                    GenreText = message.Info.GenreText
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistEntriesLoaded(AppModel model, PlaylistEntriesLoadedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive || details.PlaylistId != message.PlaylistId)
        {
            // Antwort einer inzwischen verlassenen Playlist verwerfen.
            return AppTransition.Unchanged(model);
        }

        if (message.Error is not null)
        {
            // Fehler beim Anhängen lassen die bestehende Liste stehen
            // (HasMore bleibt — der nächste Scroll versucht es erneut);
            // Fehler des Ersetz-Laufs werden als Fehlerzustand gezeigt.
            var failed = details with
            {
                IsLoading = false,
                IsLoadingMore = false,
                Attempted = true
            };
            return new AppTransition(
                model with
                {
                    PlaylistDetails = message.Append ? failed : failed with { Error = message.Error }
                },
                []);
        }

        if (message.Append && message.Page != details.NextPage)
        {
            // Veraltete Anhänge-Antwort — nicht an die Liste hängen.
            return AppTransition.Unchanged(model);
        }

        var entries = message.Append
            ? details.Entries.Concat(message.Items).ToList()
            : (IReadOnlyList<PlaylistEntryItem>)message.Items;

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    Entries = entries,
                    NextPage = message.Page + 1,
                    HasMore = message.HasMore,
                    TotalCount = message.TotalCount,
                    IsLoading = false,
                    IsLoadingMore = false,
                    Attempted = true,
                    Error = null
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistEntriesNextPageRequested(AppModel model)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive || !details.HasMore || details.IsLoading || details.IsLoadingMore)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsLoadingMore = true } },
            [new LoadPlaylistEntriesEffect(details.PlaylistId, details.NextPage, Append: true)]);
    }

    private static AppTransition UpdatePlaylistEntriesRetryRequested(AppModel model)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive || details.IsLoading || details.IsLoadingMore)
        {
            return AppTransition.Unchanged(model);
        }

        // Fehlerzustand einer leeren Liste → kompletter Neu-Lauf;
        // sonst die ausstehende Folgeseite erneut versuchen.
        if (details.HasItems)
        {
            return UpdatePlaylistEntriesNextPageRequested(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    NextPage = 1,
                    HasMore = true,
                    IsLoading = true,
                    Error = null
                }
            },
            [new LoadPlaylistEntriesEffect(details.PlaylistId, 1, Append: false)]);
    }

    private static AppTransition UpdatePlaylistEntrySelected(AppModel model, PlaylistEntrySelectedMessage message)
    {
        var item = message.Item;
        if (item.MediaId <= 0 || string.IsNullOrWhiteSpace(item.MediaType))
        {
            return AppTransition.Unchanged(model);
        }

        // Abspielbare und zugängliche Einträge starten die Wiedergabe ab
        // dem gewählten Eintrag über den serverseitigen Playlist-Kontext;
        // alle übrigen Einträge (Serie, Staffel, Kollektion, nicht
        // zugängliche Medien) öffnen weiterhin die Medien-Detailseite.
        var mediaType = MediaTypeNames.Normalize(item.MediaType);
        if (item.IsAccessible
            && mediaType is "movie" or "tvshowepisode"
            && model.PlaylistDetails.PlaylistId > 0)
        {
            return new AppTransition(
                model,
                [new StartPlaylistPlaybackEffect(model.PlaylistDetails.PlaylistId, item.EntryId)]);
        }

        // Der Elternkontext wird als Show-/Season-Hint an die
        // Medien-Detailseite weitergereicht (Episode → Staffel bzw.
        // Serie, Staffel → Serie).
        var showId = item.ParentMediaType == "tvshow" ? item.ParentMediaId : null;
        var seasonId = item.ParentMediaType == "tvshowseason" ? item.ParentMediaId : null;
        return new AppTransition(
            model,
            [new NavigateToMediaDetailsEffect(
                item.MediaType, item.MediaId, item.Title,
                ShowId: showId, SeasonId: seasonId)]);
    }

    /// <summary>
    /// Wiedergabe-Anforderung einer Playlist (Kopf-Schaltfläche der
    /// Detailseite): der serverseitige Wiedergabestart löst den
    /// Starteintrag auf — das Ergebnis kommt als
    /// <see cref="PlaylistPlaybackStartedMessage"/> zurück.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Start-Anforderung.</param>
    /// <returns>Übergang mit dem Start-Effekt bzw. unverändertem Model.</returns>
    private static AppTransition UpdatePlaylistPlaybackStartRequested(AppModel model, PlaylistPlaybackStartRequestedMessage message)
    {
        return message.PlaylistId > 0
            ? new AppTransition(model, [new StartPlaylistPlaybackEffect(message.PlaylistId, message.EntryId)])
            : AppTransition.Unchanged(model);
    }

    /// <summary>
    /// Ergebnis des Playlist-Wiedergabestarts: bei Erfolg wird der
    /// Player-State mit dem aufgelösten Eintrag und dem Playlist-Kontext
    /// belegt (die Folgevideo-Ermittlung läuft dann über die
    /// Play-Endpunkte, nicht über die Serien-Fortsetzungsliste), zur
    /// Wiedergabeseite navigiert und die Quelle mit der serverseitigen
    /// Fortsetzungsposition aufgelöst; bei Fehlschlag zeigt die
    /// Wiedergabeseite den Fehler.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Das Ergebnis des Wiedergabestarts.</param>
    /// <returns>Übergang zum Player mit belegtem Zustand bzw. Fehlerzustand.</returns>
    private static AppTransition UpdatePlaylistPlaybackStarted(AppModel model, PlaylistPlaybackStartedMessage message)
    {
        if (message.Start is { } start)
        {
            var mediaType = MediaTypeNames.Normalize(start.MediaType);
            return new AppTransition(
                model with
                {
                    Player = new PlayerState
                    {
                        MediaType = mediaType,
                        MediaId = start.MediaId,
                        Title = start.Title,
                        PlaylistId = start.PlaylistId,
                        PlaylistName = start.PlaylistName,
                        PlaylistEntryId = start.CurrentEntryId,
                        PlaylistPosition = start.CurrentPosition,
                        PlaylistTotalCount = start.TotalCount,
                        IsResolving = true,
                        Attempted = true
                    }
                },
                [new NavigateToPlayerEffect(),
                 new ResolvePlaybackSourceEffect(mediaType, start.MediaId, start.StartPositionSeconds)]);
        }

        // Start fehlgeschlagen (z. B. kein abspielbarer Eintrag): die
        // Wiedergabeseite zeigt den Fehler im Fehlerbanner.
        return new AppTransition(
            model with
            {
                Player = new PlayerState { Error = message.Error ?? PlaybackUnavailableError }
            },
            [new NavigateToPlayerEffect()]);
    }

    /// <summary>
    /// Manuelle Vorwärts-Navigation der Playlist-Wiedergabe („Weiter"):
    /// der Server löst den nächsten abspielbaren Eintrag auf — das
    /// Ergebnis kommt als <see cref="PlaylistNavigationCompletedMessage"/>
    /// zurück. Ein ausstehender Lauf (<see cref="PlayerState.IsResolving"/>)
    /// unterbindet weitere Versendungen.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <returns>Übergang mit dem Navigations-Effekt bzw. unverändertem Model.</returns>
    private static AppTransition UpdatePlaylistNextRequested(AppModel model)
    {
        var player = model.Player;
        if (!player.IsPlaylistPlayback || player.IsResolving || player.PlaylistNextExhausted)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { Player = player with { IsResolving = true, Error = null } },
            [new LoadNextPlaylistEntryEffect(player.PlaylistId!.Value, player.PlaylistEntryId)]);
    }

    /// <summary>
    /// Manuelle Rückwärts-Navigation der Playlist-Wiedergabe („Zurück"):
    /// der Server löst den vorherigen abspielbaren Eintrag auf — das
    /// Ergebnis kommt als <see cref="PlaylistNavigationCompletedMessage"/>
    /// zurück.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <returns>Übergang mit dem Navigations-Effekt bzw. unverändertem Model.</returns>
    private static AppTransition UpdatePlaylistPreviousRequested(AppModel model)
    {
        var player = model.Player;
        if (!player.IsPlaylistPlayback || player.IsResolving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { Player = player with { IsResolving = true, Error = null } },
            [new LoadPreviousPlaylistEntryEffect(player.PlaylistId!.Value, player.PlaylistEntryId)]);
    }

    /// <summary>
    /// Übernimmt das Ergebnis einer Playlist-Navigation: der Player
    /// wechselt auf den aufgelösten Eintrag (Medium, Titel, Position)
    /// und löst dessen Quelle mit der serverseitigen Fortsetzungsposition
    /// auf. Ein leeres Ergebnis meldet „kein Eintrag in dieser Richtung"
    /// und erschöpft die Vorwärts-Versuche
    /// (<see cref="PlayerState.PlaylistNextExhausted"/>); Antworten
    /// abgelösender Identitäten werden verworfen.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Das Ergebnis der Navigation.</param>
    /// <returns>Übergang zum Folgeeintrag, zum Erschöpfungs-/Fehlerstand bzw. unverändertem Model.</returns>
    private static AppTransition UpdatePlaylistNavigationCompleted(AppModel model, PlaylistNavigationCompletedMessage message)
    {
        var player = model.Player;
        if (!player.IsPlaylistPlayback
            || player.PlaylistId != message.PlaylistId
            || player.PlaylistEntryId != message.CurrentEntryId)
        {
            // Antwort einer inzwischen abgelösten Navigation verwerfen.
            return AppTransition.Unchanged(model);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with { Player = player with { IsResolving = false, Error = error } },
                []);
        }

        if (message.Result is not { } next)
        {
            // Kein Eintrag in dieser Richtung (204): bei Vorwärts-
            // navigation weitere Versuche bis zum nächsten Medienwechsel
            // unterdrücken; die Wiedergabe läuft unverändert weiter.
            return new AppTransition(
                model with
                {
                    Player = player with
                    {
                        IsResolving = false,
                        PlaylistNextExhausted = message.Forward || player.PlaylistNextExhausted
                    }
                },
                []);
        }

        var mediaType = MediaTypeNames.Normalize(next.MediaType);
        return new AppTransition(
            model with
            {
                Player = player with
                {
                    MediaType = mediaType,
                    MediaId = next.MediaId,
                    Title = next.Title,
                    PlaylistEntryId = next.EntryId,
                    PlaylistPosition = next.Position,
                    PlaylistNextExhausted = false,
                    Source = null,
                    IsResolving = true,
                    IsPlaying = false,
                    IsBuffering = false,
                    IsWatched = false,
                    IsPictureInPicture = false,
                    Position = TimeSpan.Zero,
                    Duration = TimeSpan.Zero,
                    LastReportedPositionSeconds = -1
                }
            },
            [new ResolvePlaybackSourceEffect(mediaType, next.MediaId, next.StartPositionSeconds)]);
    }

    /// <summary>
    /// Wechsel zum nächsten Eintrag der Playlist (Auto-Fortsetzung und
    /// Fehler-Rückfall): <c>POST api/playlists/{id}/play/advance</c> wird
    /// angefordert; der Ladezustand zeigt die Auflösung an und ein
    /// etwaiger Fehlertext bleibt als Hinweis sichtbar — Gegenstück zu
    /// <see cref="AdvanceToNextEpisode"/> im Playlist-Kontext.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="player">Bisheriger Player-Zustand.</param>
    /// <param name="error">Fehlertext; <c>null</c> beim regulären Weiterschalten.</param>
    /// <returns>Übergang mit dem Advance-Effekt.</returns>
    private static AppTransition AdvancePlaylistEntry(AppModel model, PlayerState player, string? error)
    {
        return new AppTransition(
            model with
            {
                Player = player with
                {
                    Source = null,
                    IsResolving = true,
                    IsPlaying = false,
                    IsBuffering = false,
                    Error = error
                }
            },
            [new AdvancePlaylistEntryEffect(player.PlaylistId!.Value, player.PlaylistEntryId)]);
    }

    // --- Playlist-Verwaltung (Schritt 6) ---

    private static AppTransition UpdatePlaylistCreateRequested(AppModel model)
    {
        if (model.ConnectionState != ConnectionState.Connected || model.PlaylistForm.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistForm = new PlaylistFormState
                {
                    IsOpen = true,
                    EditingPlaylistId = null,
                    Name = string.Empty,
                    Description = null,
                    SortMode = ApiClient.Models.PlaylistSortModeValues.Manual
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistEditRequested(AppModel model)
    {
        var details = model.PlaylistDetails;
        if (!details.CanManage || model.PlaylistForm.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistForm = new PlaylistFormState
                {
                    IsOpen = true,
                    EditingPlaylistId = details.PlaylistId,
                    Name = details.Name,
                    Description = details.Description,
                    SortMode = details.SortMode
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistFormNameChanged(AppModel model, PlaylistFormNameChangedMessage message)
    {
        if (!model.PlaylistForm.IsOpen)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistForm = model.PlaylistForm with { Name = message.Name, Error = null } },
            []);
    }

    private static AppTransition UpdatePlaylistFormDescriptionChanged(AppModel model, PlaylistFormDescriptionChangedMessage message)
    {
        if (!model.PlaylistForm.IsOpen)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistForm = model.PlaylistForm with { Description = message.Description } },
            []);
    }

    private static AppTransition UpdatePlaylistFormSortModeSelected(AppModel model, PlaylistFormSortModeSelectedMessage message)
    {
        var form = model.PlaylistForm;
        // Der Sortiermodus ist wie in der Server-UI nur bei Neuanlage wählbar.
        if (!form.IsOpen || form.IsEditing || form.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistForm = form with { SortMode = message.SortMode } },
            []);
    }

    private static AppTransition UpdatePlaylistFormCancel(AppModel model)
    {
        if (!model.PlaylistForm.IsOpen || model.PlaylistForm.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistForm = PlaylistFormState.Closed },
            []);
    }

    private static AppTransition UpdatePlaylistFormSaveRequested(AppModel model)
    {
        var form = model.PlaylistForm;
        if (!form.IsOpen || form.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        // Name ist serverseitig Pflicht — leere Eingaben bleiben mit
        // Inline-Text im Formular (keine Serveranfrage).
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            return new AppTransition(
                model with { PlaylistForm = form with { Error = PlaylistNameRequiredError } },
                []);
        }

        if (model.ConnectionState != ConnectionState.Connected)
        {
            return new AppTransition(
                model with { PlaylistForm = form with { Error = PlaylistsOfflineError } },
                []);
        }

        return new AppTransition(
            model with { PlaylistForm = form with { IsSaving = true, Error = null } },
            [new SavePlaylistEffect(
                form.EditingPlaylistId,
                form.Name.Trim(),
                string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),
                form.SortMode)]);
    }

    private static AppTransition UpdatePlaylistFormCompleted(AppModel model, PlaylistFormCompletedMessage message)
    {
        if (message.Saved is not { } saved)
        {
            // Fehlschlag: das Formular bleibt mit dem Fehlertext geöffnet.
            return new AppTransition(
                model with
                {
                    PlaylistForm = model.PlaylistForm with
                    {
                        IsSaving = false,
                        Error = message.Error ?? PlaylistSaveError
                    }
                },
                []);
        }

        var effects = new List<AppEffect>();
        var details = model.PlaylistDetails;
        // Nach dem Speichern den aktuellen Kontext auffrischen: Übersicht
        // neu laden (Anlage erscheint in der Liste) und — wenn die
        // bearbeitete Playlist offen ist — ihre Kopfdaten übernehmen.
        effects.Add(new LoadPlaylistsEffect());
        if (details.IsActive && details.PlaylistId == saved.PlaylistId)
        {
            details = details with
            {
                Name = saved.Name,
                Description = saved.Description,
                SortMode = saved.SortMode,
                GenresManuallyOverridden = saved.GenresManuallyOverridden,
                AllGenreIds = saved.AllGenreIds,
                GenreText = saved.GenreText,
                ManageNotice = null,
                ManageNoticeIsError = false
            };
        }

        return new AppTransition(
            model with
            {
                PlaylistForm = PlaylistFormState.Closed,
                PlaylistDetails = details
            },
            effects);
    }

    private static AppTransition UpdatePlaylistDeleteConfirmed(AppModel model, PlaylistDeleteConfirmedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanManage
            || details.PlaylistId != message.PlaylistId
            || details.IsManaging)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new DeletePlaylistEffect(message.PlaylistId)]);
    }

    private static AppTransition UpdatePlaylistDeleted(AppModel model, PlaylistDeletedMessage message)
    {
        var details = model.PlaylistDetails;
        if (details.PlaylistId != message.PlaylistId)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        ManageNotice = error,
                        ManageNoticeIsError = true
                    }
                },
                []);
        }

        // Die Playlist existiert nicht mehr: Detail-Kontext leeren, zur
        // Übersicht zurückkehren und ihre Liste auffrischen.
        return new AppTransition(
            model with { PlaylistDetails = PlaylistDetailsState.Empty },
            [new NavigateBackEffect(), new LoadPlaylistsEffect()]);
    }

    /// <summary>Gemeinsame Wache der Eintrags-Verwaltung (Entfernen/Verschieben): eigene, aktive Playlist ohne laufende Operation.</summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="entry">Der betroffene Eintrag.</param>
    /// <returns><c>true</c>, wenn die Aktion erlaubt ist.</returns>
    private static bool CanManageEntry(AppModel model, PlaylistEntryItem entry)
    {
        var details = model.PlaylistDetails;
        return details.CanManage
            && !details.IsManaging
            && entry.EntryId > 0;
    }

    private static AppTransition UpdatePlaylistEntryRemoveRequested(AppModel model, PlaylistEntryRemoveRequestedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!CanManageEntry(model, message.Entry) || details.IndexOfEntry(message.Entry) < 0)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new RemovePlaylistEntryEffect(details.PlaylistId, message.Entry, ConfirmContinueWatchingRemoval: false)]);
    }

    private static AppTransition UpdatePlaylistEntryRemoveCompleted(AppModel model, PlaylistEntryRemoveCompletedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.RequiresConfirmation)
        {
            // Weiterschauen-Konflikt (409): der Eintrag bleibt vorerst —
            // die Seite öffnet einmalig den Bestätigungsdialog.
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        PendingRemoveConflict = message.Entry
                    }
                },
                []);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        ManageNotice = error,
                        ManageNoticeIsError = true
                    }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = false,
                    PendingRemoveConflict = null,
                    ManageNotice = PlaylistEntryRemovedNotice,
                    ManageNoticeIsError = false,
                    IsLoading = true
                }
            },
            [new LoadPlaylistEntriesEffect(details.PlaylistId, 1, Append: false)]);
    }

    private static AppTransition UpdatePlaylistEntryRemoveConfirmed(AppModel model, PlaylistEntryRemoveConfirmedMessage message)
    {
        var details = model.PlaylistDetails;
        var pending = details.PendingRemoveConflict;
        if (!details.IsActive || pending is null)
        {
            return AppTransition.Unchanged(model);
        }

        if (!message.Confirmed)
        {
            // Abbruch: der Konflikt löst sich ohne Serveraufruf auf.
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with { PendingRemoveConflict = null }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = true,
                    PendingRemoveConflict = null
                }
            },
            [new RemovePlaylistEntryEffect(details.PlaylistId, pending, ConfirmContinueWatchingRemoval: true)]);
    }

    private static AppTransition UpdatePlaylistEntryMoveToBeginningRequested(AppModel model, PlaylistEntryMoveToBeginningRequestedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanReorder || details.IsManaging || details.IsFirstEntry(message.Entry))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new MovePlaylistEntryToBeginningEffect(details.PlaylistId, message.Entry)]);
    }

    private static AppTransition UpdatePlaylistEntryMoveToEndRequested(AppModel model, PlaylistEntryMoveToEndRequestedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanReorder || details.IsManaging || details.IsLastEntry(message.Entry))
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new MovePlaylistEntryToEndEffect(details.PlaylistId, message.Entry)]);
    }

    private static AppTransition UpdatePlaylistEntryMoveBetweenRequested(AppModel model, PlaylistEntryMoveBetweenRequestedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanReorder || details.IsManaging || details.IndexOfEntry(message.Entry) < 0)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new MovePlaylistEntryBetweenEffect(details.PlaylistId, message.Entry, message.NewSortOrder)]);
    }

    private static AppTransition UpdatePlaylistEntryMoveCompleted(AppModel model, PlaylistEntryMoveCompletedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        ManageNotice = error,
                        ManageNoticeIsError = true
                    }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = false,
                    ManageNotice = PlaylistEntryMovedNotice,
                    ManageNoticeIsError = false,
                    IsLoading = true
                }
            },
            [new LoadPlaylistEntriesEffect(details.PlaylistId, 1, Append: false)]);
    }

    private static AppTransition UpdatePlaylistSortModeRequested(AppModel model, PlaylistSortModeRequestedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanManage
            || details.IsManaging
            || string.IsNullOrWhiteSpace(message.NewSortMode)
            || message.NewSortMode == details.SortMode)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new ChangePlaylistSortModeEffect(
                details.PlaylistId, message.NewSortMode, ConfirmLossOfManualOrder: false)]);
    }

    private static AppTransition UpdatePlaylistSortModeCompleted(AppModel model, PlaylistSortModeCompletedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.RequiresConfirmation)
        {
            // Verlust-Konflikt (409): die Seite öffnet einmalig den
            // Bestätigungsdialog zum Verwerfen der manuellen Reihenfolge.
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        PendingSortModeConflict = message.NewSortMode
                    }
                },
                []);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        ManageNotice = error,
                        ManageNoticeIsError = true
                    }
                },
                []);
        }

        if (message.Updated is null)
        {
            return AppTransition.Unchanged(model);
        }

        // Der Wechsel kann die Eintragsreihenfolge serverseitig ändern —
        // Kopf übernehmen und die Liste neu laden.
        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = false,
                    IsLoading = true,
                    SortMode = message.Updated.SortMode,
                    ManageNotice = PlaylistSortModeSavedNotice,
                    ManageNoticeIsError = false
                }
            },
            [new LoadPlaylistEntriesEffect(details.PlaylistId, 1, Append: false)]);
    }

    private static AppTransition UpdatePlaylistSortModeConfirmed(AppModel model, PlaylistSortModeConfirmedMessage message)
    {
        var details = model.PlaylistDetails;
        var pending = details.PendingSortModeConflict;
        if (!details.IsActive || pending is null)
        {
            return AppTransition.Unchanged(model);
        }

        if (!message.Confirmed)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with { PendingSortModeConflict = null }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = true,
                    PendingSortModeConflict = null
                }
            },
            [new ChangePlaylistSortModeEffect(
                details.PlaylistId, pending, ConfirmLossOfManualOrder: true)]);
    }

    private static AppTransition UpdatePlaylistGenreEditorRequested(AppModel model)
    {
        var details = model.PlaylistDetails;
        if (!details.CanManage || details.IsManaging || model.PlaylistGenreEditor.IsOpen)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistGenreEditor = new PlaylistGenreEditorState
                {
                    IsOpen = true,
                    PlaylistId = details.PlaylistId,
                    IsLoading = true
                }
            },
            [new LoadGenreOptionsEffect(details.PlaylistId)]);
    }

    private static AppTransition UpdatePlaylistGenreOptionsLoaded(AppModel model, PlaylistGenreOptionsLoadedMessage message)
    {
        var editor = model.PlaylistGenreEditor;
        if (!editor.IsOpen)
        {
            return AppTransition.Unchanged(model);
        }

        // Vorauswahl = aktuelle serverseitige Genre-Ids der Playlist.
        var selected = model.PlaylistDetails.AllGenreIds.ToHashSet();
        var options = message.Options
            .Select(o => o with { IsSelected = selected.Contains(o.Id) })
            .ToList();

        return new AppTransition(
            model with
            {
                PlaylistGenreEditor = editor with
                {
                    Options = options,
                    IsLoading = false,
                    Error = message.Error
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistGenreToggled(AppModel model, PlaylistGenreToggledMessage message)
    {
        var editor = model.PlaylistGenreEditor;
        if (!editor.IsOpen || editor.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        var options = editor.Options
            .Select(o => o.Id == message.GenreId ? o with { IsSelected = !o.IsSelected } : o)
            .ToList();

        return new AppTransition(
            model with { PlaylistGenreEditor = editor with { Options = options } },
            []);
    }

    private static AppTransition UpdatePlaylistGenresSaveRequested(AppModel model)
    {
        var editor = model.PlaylistGenreEditor;
        var details = model.PlaylistDetails;
        if (!editor.IsOpen || editor.IsLoading || editor.IsSaving
            || !details.CanManage || details.IsManaging)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistGenreEditor = editor with { IsSaving = true, Error = null }
            },
            [new SetPlaylistGenresEffect(editor.PlaylistId, editor.SelectedIds)]);
    }

    private static AppTransition UpdatePlaylistGenresResetRequested(AppModel model)
    {
        var editor = model.PlaylistGenreEditor;
        var details = model.PlaylistDetails;
        if (!editor.IsOpen || editor.IsLoading || editor.IsSaving
            || !details.CanManage || details.IsManaging
            || !details.GenresManuallyOverridden)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with
            {
                PlaylistGenreEditor = editor with { IsSaving = true, Error = null }
            },
            [new ResetPlaylistGenresEffect(editor.PlaylistId)]);
    }

    private static AppTransition UpdatePlaylistGenresCompleted(AppModel model, PlaylistGenresCompletedMessage message)
    {
        var editor = model.PlaylistGenreEditor;
        var details = model.PlaylistDetails;
        if (!editor.IsOpen)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Updated is not { } updated)
        {
            return new AppTransition(
                model with
                {
                    PlaylistGenreEditor = editor with
                    {
                        IsSaving = false,
                        Error = message.Error ?? PlaylistGenresError
                    }
                },
                []);
        }

        // Erfolg: Editor schließen und die Kopfdaten der Detailseite
        // mit den neuen Genre-Feldern aktualisieren.
        return new AppTransition(
            model with
            {
                PlaylistGenreEditor = PlaylistGenreEditorState.Closed,
                PlaylistDetails = details with
                {
                    GenresManuallyOverridden = updated.GenresManuallyOverridden,
                    AllGenreIds = updated.AllGenreIds,
                    GenreText = updated.GenreText,
                    ManageNotice = PlaylistGenresSavedNotice,
                    ManageNoticeIsError = false
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistGenreEditorCancel(AppModel model)
    {
        if (!model.PlaylistGenreEditor.IsOpen || model.PlaylistGenreEditor.IsSaving)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistGenreEditor = PlaylistGenreEditorState.Closed },
            []);
    }

    private static AppTransition UpdatePlaylistPublicToggled(AppModel model, PlaylistPublicToggledMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.CanManage || !details.CanSetPublic || details.IsManaging
            || message.IsPublic == details.IsPublic)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { PlaylistDetails = details with { IsManaging = true } },
            [new SetPlaylistPublicEffect(details.PlaylistId, message.IsPublic)]);
    }

    private static AppTransition UpdatePlaylistPublicCompleted(AppModel model, PlaylistPublicCompletedMessage message)
    {
        var details = model.PlaylistDetails;
        if (!details.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Updated is not { } updated)
        {
            return new AppTransition(
                model with
                {
                    PlaylistDetails = details with
                    {
                        IsManaging = false,
                        ManageNotice = message.Error ?? PlaylistPublicError,
                        ManageNoticeIsError = true
                    }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                PlaylistDetails = details with
                {
                    IsManaging = false,
                    IsPublic = updated.IsPublic,
                    ManageNotice = PlaylistPublicSavedNotice,
                    ManageNoticeIsError = false
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistAddTargetsRequested(AppModel model)
    {
        var details = model.MediaDetails;
        if (details.Details is null
            || details.IsLoadingPlaylistTargets
            || details.IsAddingToPlaylist
            || model.ConnectionState != ConnectionState.Connected)
        {
            return AppTransition.Unchanged(model);
        }

        // Die Ziele werden nur beim ersten Öffnen geladen — ein
        // befüllter Stand wird wiederverwendet (eigene Playlists
        // ändern sich selten zwischen zwei Öffnungen).
        if (details.PlaylistTargets is not null)
        {
            return new AppTransition(
                model with
                {
                    MediaDetails = details with { PlaylistPickerPending = true }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = details with
                {
                    PlaylistPickerPending = true,
                    IsLoadingPlaylistTargets = true,
                    AddToPlaylistNotice = null,
                    AddToPlaylistNoticeIsError = false
                }
            },
            [new LoadPlaylistTargetsEffect()]);
    }

    private static AppTransition UpdatePlaylistTargetsLoaded(AppModel model, PlaylistTargetsLoadedMessage message)
    {
        var details = model.MediaDetails;
        if (!details.IsLoadingPlaylistTargets)
        {
            return AppTransition.Unchanged(model);
        }

        if (message.Error is { } error)
        {
            return new AppTransition(
                model with
                {
                    MediaDetails = details with
                    {
                        IsLoadingPlaylistTargets = false,
                        PlaylistPickerPending = false,
                        AddToPlaylistNotice = error,
                        AddToPlaylistNoticeIsError = true
                    }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = details with
                {
                    IsLoadingPlaylistTargets = false,
                    PlaylistTargets = message.Targets ?? []
                }
            },
            []);
    }

    private static AppTransition UpdatePlaylistPickerDismissed(AppModel model, PlaylistPickerDismissedMessage message)
    {
        var details = model.MediaDetails;
        if (!details.PlaylistPickerPending)
        {
            return AppTransition.Unchanged(model);
        }

        var target = details.PlaylistTargets
            ?.FirstOrDefault(p => p.PlaylistId == message.PlaylistId);
        var media = details.Details;
        if (target is null || media is null || media.MediaId <= 0)
        {
            // Abbruch oder ungültige Wahl: die Auswahl löst sich auf.
            return new AppTransition(
                model with
                {
                    MediaDetails = details with { PlaylistPickerPending = false }
                },
                []);
        }

        return new AppTransition(
            model with
            {
                MediaDetails = details with
                {
                    PlaylistPickerPending = false,
                    IsAddingToPlaylist = true,
                    AddToPlaylistNotice = null,
                    AddToPlaylistNoticeIsError = false
                }
            },
            [new AddPlaylistEntryEffect(
                target.PlaylistId, target.Name, media.MediaType, media.MediaId)]);
    }

    private static AppTransition UpdatePlaylistEntryAdded(AppModel model, PlaylistEntryAddedMessage message)
    {
        var details = model.MediaDetails;
        if (!details.IsAddingToPlaylist)
        {
            return AppTransition.Unchanged(model);
        }

        var notice = message.Error
            ?? message.ResultMessage
            ?? $"„{message.PlaylistName}“ wurde ergänzt.";

        return new AppTransition(
            model with
            {
                MediaDetails = details with
                {
                    IsAddingToPlaylist = false,
                    AddToPlaylistNotice = notice,
                    AddToPlaylistNoticeIsError = message.Error is not null
                }
            },
            []);
    }

    private static AppTransition UpdateProfilePageOpened(AppModel model)
    {
        if (model.IsOffline)
        {
            // Kein Profilabruf ohne Serververbindung — die Seite zeigt
            // den Offline-Hinweis (gebunden über IsOffline).
            return model.Profile.Attempted
                ? AppTransition.Unchanged(model)
                : new AppTransition(
                    model with { Profile = ProfilePageState.Initial with { Attempted = true } },
                    []);
        }

        if (model.Profile.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        return new AppTransition(
            model with { Profile = model.Profile with { IsLoading = true } },
            [new LoadProfileInfoEffect()]);
    }

    private static AppTransition UpdateProfileInfoLoaded(AppModel model, ProfileInfoLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                Profile = new ProfilePageState
                {
                    Attempted = true,
                    Username = message.Info.Username,
                    Email = message.Info.Email,
                    ProfileName = message.Info.ProfileName
                }
            },
            []);
    }

    private static AppTransition UpdateLogoutRequested(AppModel model)
    {
        // Die Abmeldung ist nur aus einem angemeldeten Zustand wirksam —
        // ohne Sitzung (Anmeldung/Einrichtung ausstehend) gibt es nichts
        // zu beenden.
        if (model.ConnectionState is not (ConnectionState.Connected or ConnectionState.Offline))
        {
            return AppTransition.Unchanged(model);
        }

        // Die Sitzung endet lokal (die API besitzt keinen Logout-
        // Endpunkt — JWT ist zustandslos): Ereignisverbindung trennen,
        // Client und gespeicherte Geheimnisse verwerfen und den
        // sitzungsbezogenen Anzeigezustand zurücksetzen — ein anderer
        // Benutzer darf keine Daten der beendeten Sitzung sehen.
        // Gerätebezogene Daten (Einstellungen, bekannte Server, aktive
        // Adresse, Offline-Daten-Bestand) bleiben erhalten; die lokalen
        // Caches werden beim nächsten Sync autoritativ ersetzt.
        var updated = model with
        {
            ConnectionState = ConnectionState.NeedsLogin,
            CurrentPage = AppPage.Login,
            IsStartupRunning = false,
            IsServerCheckRunning = false,
            IsLoginRunning = false,
            IsSyncRunning = false,
            LoginError = null,
            LoginUsername = null,
            ContinueWatching = [],
            Favorites = [],
            RecentEntries = [],
            SourcesOverview = SourcesOverviewState.Initial,
            SourceBrowser = SourceBrowserState.Empty,
            MediaDetails = MediaDetailsState.Empty,
            FavoritesPage = FavoritesPageState.Initial,
            Profile = ProfilePageState.Initial,
            Player = PlayerState.Empty,
            // Der Hero-Hintergrund wird benutzerbezogen generiert — er
            // gehört zur beendeten Sitzung.
            HomeHero = HomeHeroState.Empty
        };

        var effects = new List<AppEffect>
        {
            new DisconnectServerEventsEffect(),
            new LogoutEffect()
        };
        if (model.CurrentPage != AppPage.Login)
        {
            effects.Add(new NavigateToPageEffect(AppPage.Login));
        }

        return new AppTransition(updated, effects);
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

    /// <summary>
    /// Wiedergabe-Start („Abspielen"): Player-State mit dem Medium und
    /// der aus dem Serienkontext ermittelten Fortsetzungsliste belegen,
    /// zur Wiedergabeseite navigieren und die Quelle auflösen. Nur
    /// abspielbare Typen (<c>movie</c>/<c>tvshowepisode</c>) mit
    /// gültiger Id werden angenommen.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Die Wiedergabe-Anforderung.</param>
    /// <returns>Übergang mit belegtem Player-State, Navigations- und Auflösungs-Effekt.</returns>
    private static AppTransition UpdatePlayRequested(AppModel model, PlayRequestedMessage message)
    {
        var mediaType = MediaTypeNames.Normalize(message.MediaType);
        if (message.MediaId <= 0 || mediaType is not ("movie" or "tvshowepisode"))
        {
            return AppTransition.Unchanged(model);
        }

        return StartPlayback(model, mediaType, message.MediaId, message.Title);
    }

    /// <summary>
    /// Gemeinsamer Wiedergabe-Start der Aufrufe aus Detailseite und
    /// Weiterschauen-Liste: Player-State mit dem Medium und der aus dem
    /// Serienkontext ermittelten Fortsetzungsliste belegen, zur
    /// Wiedergabeseite navigieren und die Quelle auflösen (die
    /// Fortsetzungsposition ermittelt die Quellauflösung selbst).
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="mediaType">Medientyp (normalisiert, CLR-Konvention).</param>
    /// <param name="mediaId">Id des Mediums.</param>
    /// <param name="title">Anzeigetitel des Mediums.</param>
    /// <returns>Übergang mit belegtem Player-State, Navigations- und Auflösungs-Effekt.</returns>
    private static AppTransition StartPlayback(AppModel model, string mediaType, long mediaId, string? title)
    {
        return new AppTransition(
            model with
            {
                Player = new PlayerState
                {
                    MediaType = mediaType,
                    MediaId = mediaId,
                    Title = title,
                    IsResolving = true,
                    Attempted = true,
                    UpcomingEpisodes = NextEpisodeQueue(model, mediaId)
                }
            },
            [new NavigateToPlayerEffect(), new ResolvePlaybackSourceEffect(mediaType, mediaId)]);
    }

    /// <summary>
    /// Folge-Episoden des Mediums aus dem Serienkontext der Detailseite:
    /// alle Episoden nach <paramref name="mediaId"/>, staffelübergreifend
    /// nach Staffel-/Episodennummer sortiert. Ohne Serienkontext oder bei
    /// nicht enthaltener Episode bleibt die Liste leer (kein Rückfall).
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="mediaId">Id der gestarteten Episode.</param>
    /// <returns>Die geordnete Fortsetzungsliste (leer ohne Folge-Episoden).</returns>
    private static IReadOnlyList<PlayerEpisodeRef> NextEpisodeQueue(AppModel model, long mediaId)
    {
        var details = model.MediaDetails.Details;
        if (details?.HasSeasons != true)
        {
            return [];
        }

        var episodes = details.Seasons
            .OrderBy(s => s.Number)
            .SelectMany(s => s.Episodes.OrderBy(e => e.Number))
            .ToList();
        var index = episodes.FindIndex(e => e.Id == mediaId);
        if (index < 0)
        {
            return [];
        }

        return episodes
            .Skip(index + 1)
            .Select(e => new PlayerEpisodeRef { MediaId = e.Id, Title = e.Title })
            .ToList();
    }

    private static AppTransition UpdatePlayerPageOpened(AppModel model)
    {
        var player = model.Player;
        return player.IsActive && !player.Attempted
            ? new AppTransition(model with { Player = player with { Attempted = true } }, [])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdatePlaybackSourceResolved(AppModel model, PlaybackSourceResolvedMessage message)
    {
        var player = model.Player;
        if (!player.IsActive
            || player.MediaId != message.MediaId
            || player.MediaType != MediaTypeNames.Normalize(message.MediaType))
        {
            // Antwort einer inzwischen abgelösten Identität verwerfen.
            return AppTransition.Unchanged(model);
        }

        if (message.Source is { } source)
        {
            return new AppTransition(
                model with
                {
                    Player = player with
                    {
                        Source = source,
                        IsResolving = false,
                        Error = null
                    }
                },
                []);
        }

        // Quelle nicht verfügbar → Fehler anzeigen und auf den nächsten
        // Eintrag zurückfallen: im Playlist-Kontext über die serverseitige
        // Playlist-Reihenfolge, sonst über die Serien-Fortsetzungsliste.
        if (player.IsPlaylistPlayback && !player.PlaylistNextExhausted)
        {
            return AdvancePlaylistEntry(model, player, message.Error ?? PlaybackUnavailableError);
        }

        return AdvanceToNextEpisode(model, player, message.Error ?? PlaybackUnavailableError);
    }

    /// <summary>
    /// Wechsel zur nächsten Episode der Fortsetzungsliste (Fehler-Rückfall
    /// und Auto-Fortsetzung): der State übernimmt Medium/Titel der
    /// wartenden Episode, Wiedergabe-Werte werden zurückgesetzt und die
    /// Quelle neu aufgelöst; ein etwaiger Fehlertext bleibt als Hinweis
    /// sichtbar. Ohne Folge-Episode wird der Fehlerzustand gezeigt.
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="player">Bisheriger Player-Zustand.</param>
    /// <param name="error">Fehlertext; <c>null</c> beim regulären Fortsetzen.</param>
    /// <returns>Übergang zur nächsten Episode bzw. zum Fehlerzustand.</returns>
    private static AppTransition AdvanceToNextEpisode(AppModel model, PlayerState player, string? error)
    {
        if (!player.HasNextEpisode)
        {
            return new AppTransition(
                model with
                {
                    Player = player with
                    {
                        Source = null,
                        IsResolving = false,
                        IsPlaying = false,
                        IsBuffering = false,
                        Error = error ?? player.Error
                    }
                },
                []);
        }

        var next = player.UpcomingEpisodes[0];
        return new AppTransition(
            model with
            {
                Player = player with
                {
                    MediaType = "tvshowepisode",
                    MediaId = next.MediaId,
                    Title = next.Title,
                    Source = null,
                    IsResolving = true,
                    IsPlaying = false,
                    IsBuffering = false,
                    IsWatched = false,
                    IsPictureInPicture = false,
                    Position = TimeSpan.Zero,
                    Duration = TimeSpan.Zero,
                    LastReportedPositionSeconds = -1,
                    UpcomingEpisodes = player.UpcomingEpisodes.Skip(1).ToList(),
                    Error = error
                }
            },
            [new ResolvePlaybackSourceEffect("tvshowepisode", next.MediaId)]);
    }

    /// <summary>
    /// Positions-Takt der Wiedergabeseite: Position/Dauer/Laufzustand
    /// übernehmen, geänderte Position melden (Throttling über die zuletzt
    /// gemeldete Position), die „angesehen"-Schwelle
    /// (<see cref="AppSettings.WatchedEndWindowSeconds"/>) setzen und die
    /// Auto-Fortsetzung prüfen (Restlaufzeit &lt;
    /// <see cref="AppSettings.AutoResumeRemainingSeconds"/> → nächste
    /// Episode). Medien kürzer als das jeweilige Fenster greifen ab dem
    /// ersten Takt (Restlaufzeit liegt dann von Anfang an im Fenster).
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <param name="message">Der Wiedergabe-Takt der Seite.</param>
    /// <returns>Übergang mit aktualisiertem Player-State und Effekten.</returns>
    private static AppTransition UpdatePlaybackProgress(AppModel model, PlaybackProgressMessage message)
    {
        var player = model.Player;
        if (!player.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        var updated = player with
        {
            Position = message.Position,
            Duration = message.Duration,
            IsPlaying = message.IsPlaying,
            IsBuffering = message.IsBuffering
        };

        var effects = new List<AppEffect>();
        if (message.Duration > TimeSpan.Zero
            && message.Position >= TimeSpan.Zero
            && Math.Abs(message.Position.TotalSeconds - updated.LastReportedPositionSeconds) > 0.5)
        {
            // Fortschritt nur bei geänderter Position melden (Muster des
            // Altbestands: Schwellenwert 0,5 s gegen Doppelmeldungen).
            updated = updated with { LastReportedPositionSeconds = message.Position.TotalSeconds };
            effects.Add(new ReportPlaybackProgressEffect(
                updated.MediaType!, updated.MediaId, message.Position, message.Duration));
        }

        if (message.Duration > TimeSpan.Zero)
        {
            var remaining = message.Duration - message.Position;

            // „Angesehen": Ende abzüglich des eingestellten Fensters erreicht.
            if (!updated.IsWatched
                && remaining <= TimeSpan.FromSeconds(model.Settings.WatchedEndWindowSeconds))
            {
                updated = updated with { IsWatched = true };
                // Optionales Aufräumen: einen abgeschlossenen Download
                // der gesehenen Episode entfernen.
                if (model.Settings.AutoDeleteWatchedEpisodes && updated.MediaType == "tvshowepisode")
                {
                    effects.Add(new DeleteWatchedDownloadEffect(updated.MediaType, updated.MediaId));
                }
            }

            // Auto-Fortsetzung: Restlaufzeit unterschreitet das Fenster
            // → der nächste Titel startet (bei aktiviertem Schalter).
            // Nur bei laufender Wiedergabe — ein Takt im Pausenzustand
            // (StateChanged) darf nicht zum nächsten Titel weiterschalten.
            // Im Playlist-Kontext übernimmt der Server die Folge-Ermittlung
            // (Reihenfolge der Playlist); ein ausstehender Lauf
            // (IsResolving) bzw. ein erschöpftes Listenende
            // (PlaylistNextExhausted) unterbinden weitere Versendungen.
            if (model.Settings.AutoResumeEnabled
                && message.IsPlaying
                && updated.IsPlaylistPlayback
                && !updated.PlaylistNextExhausted
                && !updated.IsResolving
                && remaining <= TimeSpan.FromSeconds(model.Settings.AutoResumeRemainingSeconds))
            {
                var transition = AdvancePlaylistEntry(
                    MarkEpisodeWatchedInDetails(model, updated), updated, null);
                return new AppTransition(
                    transition.Model,
                    [.. effects, .. transition.Effects]);
            }

            if (model.Settings.AutoResumeEnabled
                && message.IsPlaying
                && updated.HasNextEpisode
                && remaining <= TimeSpan.FromSeconds(model.Settings.AutoResumeRemainingSeconds))
            {
                var transition = AdvanceToNextEpisode(
                    MarkEpisodeWatchedInDetails(model, updated), updated, null);
                return new AppTransition(
                    transition.Model,
                    [.. effects, .. transition.Effects]);
            }
        }

        return new AppTransition(
            MarkEpisodeWatchedInDetails(model, updated) with { Player = updated },
            effects);
    }

    /// <summary>
    /// Ende-Ereignis der Wiedergabeseite: wird nur als tatsächliches
    /// Wiedergabeende gewertet, wenn die gemeldete Position innerhalb von
    /// <see cref="PlaybackEndedToleranceSeconds"/> vor bzw. an der
    /// Gesamtdauer liegt (<see cref="IsPlaybackEndPlausible"/>). Verfrühte
    /// oder verspätete Ereignisse (Stream-Abbruch, Format-/Positionswechsel,
    /// nachgezogenes <c>MediaEnded</c> eines bereits gewechselten Mediums)
    /// werden verworfen — sie lösen weder die „angesehen"-Markierung und
    /// die End-Fortschrittsmeldung noch die Auto-Fortsetzung aus. Beim
    /// plausiblen Ende läuft der Abschluss unverändert: „angesehen"
    /// markieren, Endposition melden, optional den Download der gesehenen
    /// Episode entfernen und bei aktiviertem Schalter fortsetzen — im
    /// Playlist-Kontext über die serverseitige Playlist-Reihenfolge
    /// (<see cref="AdvancePlaylistEntry"/>), sonst über die Serien-
    /// Fortsetzungsliste (<see cref="AdvanceToNextEpisode"/>).
    /// </summary>
    /// <param name="model">Aktuelles App-Model.</param>
    /// <returns>Übergang mit dem Abschluss-Zustand bzw. unverändertem Modell bei unplausiblen Ereignissen.</returns>
    private static AppTransition UpdatePlaybackEnded(AppModel model)
    {
        var player = model.Player;
        if (!player.IsActive || !IsPlaybackEndPlausible(player))
        {
            return AppTransition.Unchanged(model);
        }

        // Abschluss: als „angesehen" markieren und die Endposition melden
        // (Position = Dauer — das Medium wurde vollständig gespielt; die
        // Plausibilitätsprüfung hat eine positive Dauer bereits sicher-
        // gestellt).
        var finalPosition = player.Duration;
        var updated = player with
        {
            IsPlaying = false,
            IsBuffering = false,
            IsWatched = true,
            Position = finalPosition,
            LastReportedPositionSeconds = finalPosition.TotalSeconds
        };

        var effects = new List<AppEffect>
        {
            new ReportPlaybackProgressEffect(
                updated.MediaType!, updated.MediaId, finalPosition, updated.Duration)
        };

        // Optionales Aufräumen: einen abgeschlossenen Download der
        // gesehenen Episode entfernen (idempotent — wirkt auch, wenn die
        // Schwelle bereits im Takt erreicht wurde).
        if (model.Settings.AutoDeleteWatchedEpisodes && updated.MediaType == "tvshowepisode")
        {
            effects.Add(new DeleteWatchedDownloadEffect(updated.MediaType, updated.MediaId));
        }

        // Am Ende liegt die Restlaufzeit bei 0 — die Auto-Fortsetzung
        // greift damit wie im Takt (Fenster ist stets > 0). Im
        // Playlist-Kontext übernimmt der Server die Folge-Ermittlung.
        if (model.Settings.AutoResumeEnabled
            && updated.IsPlaylistPlayback
            && !updated.PlaylistNextExhausted
            && !updated.IsResolving)
        {
            var transition = AdvancePlaylistEntry(
                MarkEpisodeWatchedInDetails(model, updated), updated, null);
            return new AppTransition(
                transition.Model,
                [.. effects, .. transition.Effects]);
        }

        if (model.Settings.AutoResumeEnabled && updated.HasNextEpisode)
        {
            var transition = AdvanceToNextEpisode(
                MarkEpisodeWatchedInDetails(model, updated), updated, null);
            return new AppTransition(
                transition.Model,
                [.. effects, .. transition.Effects]);
        }

        return new AppTransition(
            MarkEpisodeWatchedInDetails(model, updated) with { Player = updated },
            effects);
    }

    /// <summary>
    /// Plausibilitätsprüfung des Ende-Ereignisses gegen den gemeldeten
    /// Wiedergabestand: plausibel ist das Ende nur bei bekannter
    /// Gesamtdauer und einer Restlaufzeit innerhalb von
    /// <see cref="PlaybackEndedToleranceSeconds"/> (Position nahe/an/über
    /// dem Ende). Eine unbekannte Dauer (<c>&lt;= 0</c>) macht das
    /// Ereignis unplausibel — ohne Referenz kann nicht zwischen
    /// tatsächlichem Ende und verfrühtem Feuern unterschieden werden.
    /// </summary>
    /// <param name="player">Aktueller Player-Zustand (letzter gemeldeter Takt).</param>
    /// <returns><see langword="true"/>, wenn das Ende-Ereignis dem Wiedergabestand entspricht.</returns>
    private static bool IsPlaybackEndPlausible(PlayerState player)
    {
        return player.Duration > TimeSpan.Zero
            && player.Position >= TimeSpan.Zero
            && player.Duration - player.Position
                <= TimeSpan.FromSeconds(PlaybackEndedToleranceSeconds);
    }

    /// <summary>
    /// Überträgt den „angesehen"-Stand des Players auf die geladene
    /// Detailansicht: Die gespielte Episode erhält einen lokalen
    /// Gesehen-Zeitpunkt (<see cref="EpisodeItem.WatchedAt"/>), damit die
    /// Episodenliste ihr Angesehen-Badge ohne erneutes Laden zeigt.
    /// Greift nur bei Episoden-Wiedergabe mit gesetztem
    /// <see cref="PlayerState.IsWatched"/> und vorhandenem Serienkontext;
    /// bereits markierte oder nicht enthaltene Episoden bleiben
    /// unverändert.
    /// </summary>
    /// <param name="model">Aktuelles App-Model (Player-Stand wird separat gesetzt).</param>
    /// <param name="player">Der aktualisierte Player-Zustand mit der gespielten Identität.</param>
    /// <returns>Das ggf. aktualisierte Model; unverändert, wenn keine Markierung nötig ist.</returns>
    private static AppModel MarkEpisodeWatchedInDetails(AppModel model, PlayerState player)
    {
        var details = model.MediaDetails.Details;
        if (details is null
            || !player.IsWatched
            || MediaTypeNames.Normalize(player.MediaType) != "tvshowepisode"
            || !details.HasSeasons)
        {
            return model;
        }

        var target = details.Seasons
            .SelectMany(s => s.Episodes)
            .FirstOrDefault(e => e.Id == player.MediaId);
        if (target is null || target.WatchedAt is not null)
        {
            return model;
        }

        var seasons = details.Seasons
            .Select(s => s with
            {
                Episodes = s.Episodes
                    .Select(e => e.Id == player.MediaId
                        ? e with { WatchedAt = DateTime.UtcNow }
                        : e)
                    .ToList()
            })
            .ToList();

        return model with
        {
            MediaDetails = model.MediaDetails with
            {
                Details = details with { Seasons = seasons }
            }
        };
    }

    private static AppTransition UpdatePlaybackFailed(AppModel model)
    {
        var player = model.Player;
        if (!player.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        // Abspielfehler → Fehlermeldung und Rückfall auf den nächsten
        // Eintrag: im Playlist-Kontext über die serverseitige Playlist-
        // Reihenfolge, sonst über die Serien-Fortsetzungsliste.
        if (player.IsPlaylistPlayback && !player.PlaylistNextExhausted)
        {
            return AdvancePlaylistEntry(model, player, PlaybackUnavailableError);
        }

        return AdvanceToNextEpisode(model, player, PlaybackUnavailableError);
    }

    private static AppTransition UpdatePictureInPictureRequested(AppModel model)
    {
        return model.Player.IsActive
            ? new AppTransition(model, [new TogglePictureInPictureEffect()])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdatePictureInPictureChanged(AppModel model, PictureInPictureChangedMessage message)
    {
        var player = model.Player;
        return player.IsActive
            ? new AppTransition(model with { Player = player with { IsPictureInPicture = message.IsActive } }, [])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdatePlayerClosed(AppModel model)
    {
        var player = model.Player;
        if (!player.IsActive)
        {
            return AppTransition.Unchanged(model);
        }

        // Abschluss-Fortschritt melden und den Player-Zustand leeren;
        // ein aktives kompaktes Overlay wird vorher wieder verlassen.
        var effects = new List<AppEffect>();
        if (player.IsPictureInPicture)
        {
            effects.Add(new TogglePictureInPictureEffect());
        }
        if (player.Position > TimeSpan.Zero)
        {
            effects.Add(new ReportPlaybackProgressEffect(
                player.MediaType!, player.MediaId, player.Position, player.Duration));
        }

        return new AppTransition(model with { Player = PlayerState.Empty }, effects);
    }

    // --- Downloads (Schritt 11) -------------------------------------------

    private static AppTransition UpdateDownloadsPageOpened(AppModel model)
    {
        var state = model.Downloads;
        if (state.IsLoading)
        {
            return AppTransition.Unchanged(model);
        }

        // Die Liste liegt lokal vor (Manifest) — das Laden ist in jedem
        // Verbindungszustand möglich; laufende Transfers melden ihren
        // Fortschritt über DownloadItemChangedMessage nach.
        return new AppTransition(
            model with
            {
                Downloads = state with { IsLoading = true, Attempted = true, Error = null }
            },
            [new LoadDownloadsEffect(model.Settings)]);
    }

    private static AppTransition UpdateDownloadsLoaded(AppModel model, DownloadsLoadedMessage message)
    {
        return new AppTransition(
            model with
            {
                Downloads = new DownloadsPageState
                {
                    Attempted = true,
                    Items = message.Items,
                    UsedBytes = message.UsedBytes,
                    QuotaGb = message.QuotaGb,
                    OrphanedBytes = message.OrphanedBytes,
                    Error = message.Error
                }
            },
            []);
    }

    private static AppTransition UpdateDownloadRequested(AppModel model, DownloadRequestedMessage message)
    {
        var mediaType = MediaTypeNames.Normalize(message.MediaType);
        if (message.MediaId <= 0 || mediaType is not ("movie" or "tvshowepisode"))
        {
            return AppTransition.Unchanged(model);
        }

        if (model.ConnectionState != ConnectionState.Connected)
        {
            // Ohne Serververbindung kann kein Download beginnen — der
            // Hinweis erscheint auf der Detailseite.
            return new AppTransition(
                model with
                {
                    MediaDetails = model.MediaDetails with
                    {
                        DownloadNotice = "Keine Serververbindung — der Download ist derzeit nicht möglich.",
                        DownloadNoticeIsError = true
                    }
                },
                []);
        }

        return new AppTransition(
            model,
            [new EnqueueDownloadEffect(mediaType, message.MediaId, message.Title, model.Settings)
            {
                ShowId = message.ShowId,
                ShowTitle = message.ShowTitle,
                SeasonNumber = message.SeasonNumber,
                EpisodeNumber = message.EpisodeNumber
            }]);
    }

    private static AppTransition UpdateDownloadEnqueueCompleted(AppModel model, DownloadEnqueueCompletedMessage message)
    {
        var downloads = message.Item is { } item
            ? model.Downloads with { Items = UpsertItem(model.Downloads.Items, item) }
            : model.Downloads;

        var details = model.MediaDetails;
        if (message.Notice is not null
            && details.MediaType == MediaTypeNames.Normalize(message.MediaType)
            && details.MediaId == message.MediaId)
        {
            // Rückmeldung der angezeigten Detailseite — Hinweis einblenden.
            details = details with
            {
                DownloadNotice = message.Notice,
                DownloadNoticeIsError = message.IsError
            };
        }

        return new AppTransition(
            model with { Downloads = downloads, MediaDetails = details },
            []);
    }

    private static AppTransition UpdateDownloadItemChanged(AppModel model, DownloadItemChangedMessage message)
    {
        return new AppTransition(
            model with
            {
                Downloads = model.Downloads with
                {
                    Items = UpsertItem(model.Downloads.Items, message.Item)
                }
            },
            []);
    }

    private static AppTransition UpdateDownloadDeleted(AppModel model, DownloadDeletedMessage message)
    {
        var type = MediaTypeNames.Normalize(message.MediaType);
        return new AppTransition(
            model with
            {
                Downloads = model.Downloads with
                {
                    Items = model.Downloads.Items
                        .Where(i => !(i.MediaId == message.MediaId && i.MediaType == type))
                        .ToList()
                }
            },
            []);
    }

    private static AppTransition UpdateDownloadCancelRequested(AppModel model, DownloadCancelRequestedMessage message)
    {
        return message.Item.CanCancel
            ? new AppTransition(model, [new CancelDownloadEffect(message.Item.MediaType, message.Item.MediaId)])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateDownloadRetryRequested(AppModel model, DownloadRetryRequestedMessage message)
    {
        return message.Item.CanRetry
            ? new AppTransition(
                model,
                [new EnqueueDownloadEffect(message.Item.MediaType, message.Item.MediaId, message.Item.Title, model.Settings)])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateDownloadDeleteRequested(AppModel model, DownloadDeleteRequestedMessage message)
    {
        return message.Item.CanDelete
            ? new AppTransition(model, [new DeleteDownloadEffect(message.Item.MediaType, message.Item.MediaId)])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateDownloadsDeleteAllRequested(AppModel model)
    {
        // Die Rückfrage lief bereits im UI — hier nur der Effekt, wenn
        // überhaupt Downloads oder Restdaten vorhanden sind.
        return model.Downloads.HasItems || model.Downloads.OrphanedBytes > 0
            ? new AppTransition(model, [new DeleteAllDownloadsEffect(model.Settings)])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateDownloadsPurgeOrphansRequested(AppModel model)
    {
        return model.Downloads.OrphanedBytes > 0
            ? new AppTransition(model, [new PurgeOrphanedDownloadsEffect(model.Settings)])
            : AppTransition.Unchanged(model);
    }

    private static AppTransition UpdateDownloadsDeleteFailedRequested(AppModel model)
    {
        // Die Rückfrage lief bereits im UI — hier nur der Effekt, wenn
        // überhaupt fehlerhafte oder abgebrochene Einträge vorhanden sind.
        return model.Downloads.HasFailedItems
            ? new AppTransition(model, [new DeleteFailedDownloadsEffect(model.Settings)])
            : AppTransition.Unchanged(model);
    }

    /// <summary>
    /// Fügt einen geänderten Download-Eintrag in die Liste ein bzw.
    /// ersetzt ihn (Identität = Medientyp + Medien-Id, Sortierung nach
    /// Manifest-Id bleibt erhalten).
    /// </summary>
    /// <param name="items">Die bisherigen Einträge.</param>
    /// <param name="item">Der geänderte Eintrag.</param>
    /// <returns>Die aktualisierte Liste.</returns>
    private static IReadOnlyList<DownloadListItem> UpsertItem(
        IReadOnlyList<DownloadListItem> items,
        DownloadListItem item)
    {
        var list = items.ToList();
        var index = list.FindIndex(i => i.EntryId == item.EntryId
            || (i.MediaId == item.MediaId && i.MediaType == item.MediaType));
        if (index >= 0)
        {
            list[index] = item;
        }
        else
        {
            list.Add(item);
        }

        return list;
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
