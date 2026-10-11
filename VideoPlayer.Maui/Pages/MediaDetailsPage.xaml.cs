using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Detailseite eines Mediums (Shell-Route
/// „mediadetails?mediaType=…&amp;id=…&amp;title=…[&amp;show=…&amp;season=…&amp;episode=…]").
/// Zeigt je nach Medientyp Basisdetails (Film/Episode), die Serienansicht
/// mit Staffel-Picker und Episodenliste oder die Kollektionsansicht mit
/// enthaltenen Filmen — wie die Detailseiten der alten App.
/// </summary>
[QueryProperty(nameof(MediaType), "mediaType")]
[QueryProperty(nameof(MediaId), "id")]
[QueryProperty(nameof(MediaTitle), "title")]
[QueryProperty(nameof(ShowId), "show")]
[QueryProperty(nameof(SeasonId), "season")]
[QueryProperty(nameof(EpisodeId), "episode")]
public partial class MediaDetailsPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private string? mediaType;
    private long mediaId;
    private string? mediaTitle;
    private long? showId;
    private long? seasonId;
    private long? episodeId;
    private bool requested;
    private bool updatingSeasonPicker;
    private List<SeasonItem>? appliedSeasonItems;
    private IReadOnlyList<EpisodeItem>? appliedEpisodes;
    private byte[]? appliedHeaderBytes;
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="MediaDetailsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Detail-Zustand und nimmt die Benutzer-Nachrichten entgegen.</param>
    public MediaDetailsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        SizeChanged += OnSizeChanged;
        UpdateUi(mvuHost.Model);
    }

    /// <summary>Medientyp aus dem Navigationspfad (CLR-Konvention, z. B. „tvshowepisode").</summary>
    public string MediaType
    {
        set => mediaType = Uri.UnescapeDataString(value ?? string.Empty);
    }

    /// <summary>Medien-Id aus dem Navigationspfad.</summary>
    public string MediaId
    {
        set => mediaId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) ? id : 0;
    }

    /// <summary>Titel-Hint aus dem Navigationspfad (Fallback-Anzeige).</summary>
    public string MediaTitle
    {
        set => mediaTitle = Uri.UnescapeDataString(value ?? string.Empty);
    }

    /// <summary>Serienkontext-Hint (Episoden-/Staffel-Einträge).</summary>
    public string ShowId
    {
        set => showId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) && id > 0 ? id : null;
    }

    /// <summary>Staffel-Vorwahl-Hint.</summary>
    public string SeasonId
    {
        set => seasonId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) && id > 0 ? id : null;
    }

    /// <summary>Episoden-Vorwahl-Hint.</summary>
    public string EpisodeId
    {
        set => episodeId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) && id > 0 ? id : null;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Das ModelChanged-Abo gilt nur, solange die Seite sichtbar ist
        // (Muster: SettingsPage) — verlassene Instanzen dürfen nicht
        // weiter am MvuHost hängen.
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
        // Anfrage nur einmal pro Seiteninstanz — Details bei jeder
        // Rückkehr neu zu laden würde Staffel-Vorwahl und Favoriten-
        // Anzeige zurücksetzen. Ausnahme: der globale Detail-Slot
        // beschreibt inzwischen ein anderes Medium — die Rückkehr von
        // einer geschobenen Detailseite (z. B. Episode → Serie) muss
        // die eigene Identität wieder anfordern, sonst bliebe die
        // Seite leer.
        var state = mvuHost.Model.MediaDetails;
        var ownType = MediaTypeNames.Normalize(mediaType);
        var identityMatches = string.Equals(state.MediaType, ownType, StringComparison.Ordinal)
            && state.MediaId == mediaId;
        if ((!requested || !identityMatches) && mediaId > 0 && !string.IsNullOrWhiteSpace(mediaType))
        {
            requested = true;
            _ = mvuHost.DispatchAsync(new MediaDetailsRequestedMessage(
                mediaType, mediaId, mediaTitle, showId, seasonId, episodeId));
        }
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        subscribed = false;
        mvuHost.ModelChanged -= OnModelChanged;
        base.OnDisappearing();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateUi(model);
            }
        });
    }

    private void UpdateUi(AppModel model)
    {
        var state = model.MediaDetails;
        // Nur die Anzeige der angefragten Identität aktualisieren —
        // Details/Fehler eines zuvor angezeigten Mediums nicht übernehmen.
        var details = state.Details;
        var isRequested = string.Equals(
            state.MediaType, MediaTypeNames.Normalize(mediaType), StringComparison.Ordinal) &&
            state.MediaId == mediaId;
        var isCurrent = isRequested && details is not null;

        DetailsScroll.IsVisible = isCurrent;
        // Lade- oder Favoriten-Fehler der eigenen Identität anzeigen —
        // der Favoriten-Fehler erscheint auch bei sichtbaren Details.
        ErrorPanel.IsVisible = isRequested && (state.HasError || state.FavoriteError is not null);
        ErrorLabel.Text = state.FavoriteError ?? state.Error;
        if (!isCurrent || details is null)
        {
            return;
        }

        // Kopfbereich (Web-Vorbild .tvshow-header): Titel, Metazeile und
        // Handlung folgen dem MVU-View — bei ausgewählter Episode deren
        // Angaben, sonst die des Mediums.
        TitleLabel.Text = details.HeaderTitle;
        MetaLabel.Text = details.HeaderMeta ?? string.Empty;
        MetaLabel.IsVisible = !string.IsNullOrWhiteSpace(details.HeaderMeta);
        PlotLabel.Text = details.HeaderPlot ?? string.Empty;
        PlotLabel.IsVisible = !string.IsNullOrWhiteSpace(details.HeaderPlot);
        UpdateHeaderImage(state);

        FavoriteButton.Text = details.FavoriteStarText;
        // Die Favoriten-Umschaltung braucht den Server — im Offline-
        // Snapshot wird der Stern deaktiviert statt zu fehlschlagen.
        FavoriteButton.IsEnabled = !state.IsFavoriteUpdating && !details.IsOfflineSnapshot;

        // „Abspielen" und „Herunterladen" nur für direkt abspielbare
        // Medientypen (Film, Episode) — Serien/Staffeln/Kollektionen
        // nutzen ihre Episoden- bzw. Film-Einträge. Offline ist
        // „Abspielen" nur bei vorhandener lokaler Datei verfügbar und
        // „Herunterladen" gar nicht.
        PlayButton.IsVisible = details.IsPlayable && (!details.IsOfflineSnapshot || details.HasLocalFile);
        DownloadButton.IsVisible = details.IsPlayable && !details.IsOfflineSnapshot;

        // Hinweis der letzten Download-Anforderung dieser Identität.
        DownloadNoticeLabel.IsVisible = isRequested && state.DownloadNotice is not null;
        DownloadNoticeLabel.Text = state.DownloadNotice;
        // Farben kommen aus den zentralen Design-Tokens (Vp*), nicht aus
        // hartcodierten Werten — die Schlüssel sind per Contract-Test gepinnt.
        var resources = Application.Current!.Resources;
        DownloadNoticeLabel.TextColor = state.DownloadNoticeIsError
            ? (Color)resources["VpDanger"]
            : (Color)resources["VpSecondary"];

        // „Zu Playlist" ist ein Online-Feature — im Offline-Snapshot
        // entfällt der Schalter wie der Download-Schalter.
        AddToPlaylistButton.IsVisible = !details.IsOfflineSnapshot;
        AddToPlaylistButton.IsEnabled = !state.IsAddingToPlaylist;
        AddToPlaylistNoticeLabel.IsVisible = isRequested && state.AddToPlaylistNotice is not null;
        AddToPlaylistNoticeLabel.Text = state.AddToPlaylistNotice;
        AddToPlaylistNoticeLabel.TextColor = state.AddToPlaylistNoticeIsError
            ? (Color)resources["VpDanger"]
            : (Color)resources["VpSecondary"];

        UpdateSeasonSection(details);
        UpdateCollectionSection(details);
        MaybeShowPlaylistPicker(state);
    }

    /// <summary>
    /// Übernimmt die Kopfbereichs-Bilddaten des Models in die Bildebene.
    /// Die Umwandlung in eine <see cref="ImageSource"/> erfolgt nur bei
    /// tatsächlich neuen Bytes (Referenzvergleich, Muster des
    /// Startseiten-Hero); ohne Bild bleibt die Ebene leer — die
    /// Gradienten und Texte tragen den Kopfbereich.
    /// </summary>
    /// <param name="state">Zustand der Detailseite.</param>
    private void UpdateHeaderImage(MediaDetailsState state)
    {
        if (ReferenceEquals(appliedHeaderBytes, state.HeaderImageBytes))
        {
            return;
        }

        appliedHeaderBytes = state.HeaderImageBytes;
        HeaderImage.Source = appliedHeaderBytes is { Length: > 0 } bytes
            ? ImageSource.FromStream(() => new MemoryStream(bytes))
            : null;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        // Responsive Höhe — App-Gegenstück zum Web-CSS
        // clamp(360px, 52vw, 620px) des Detail-Headers.
        if (Width > 0)
        {
            HeaderSection.HeightRequest = Math.Clamp(Width * 0.52, 360, 620);
        }
    }

    private void UpdateSeasonSection(MediaDetailsView details)
    {
        SeasonSection.IsVisible = details.HasSeasons;
        if (!details.HasSeasons)
        {
            appliedSeasonItems = null;
            appliedEpisodes = null;
            return;
        }

        updatingSeasonPicker = true;
        try
        {
            // ItemsSource nur bei geänderter Staffel-Liste neu setzen:
            // ein Picker-Neuaufbau je Model-Tick destabilisiert die
            // Anzeige (iOS: verzögerte SelectedIndexChanged-Echos
            // setzen die Auswahl zurück und ordnen Einträge neu).
            // Der Vergleich muss die Record-Gleichheit der Einträge
            // einbeziehen — ein In-place-Neubau der Staffelliste mit
            // gleichen Ids (z. B. MarkEpisodeWatchedInDetails oder der
            // Reload nach der Rückkehr von der Episoden-Detailseite)
            // muss die neuen Instanzen in den Picker bringen, sonst
            // verbliebe ItemsSource veraltet und SelectedItem löste
            // per IndexOf auf -1 auf (sichtbarer Auswahl-Desync).
            if (appliedSeasonItems is null || !SeasonsEqual(appliedSeasonItems, details.Seasons))
            {
                appliedSeasonItems = details.Seasons.ToList();
                SeasonPicker.ItemsSource = appliedSeasonItems;
            }

            if (!Equals(SeasonPicker.SelectedItem, details.SelectedSeason))
            {
                SeasonPicker.SelectedItem = details.SelectedSeason;
            }

            // Episodenliste nur bei neuer Quelle neu aufbauen — sonst
            // erstellt jeder Model-Tick (z. B. Kopfbild-Nachlauf) alle
            // Karten erneut.
            if (!ReferenceEquals(appliedEpisodes, details.SelectedSeasonEpisodes))
            {
                appliedEpisodes = details.SelectedSeasonEpisodes;
                BindableLayout.SetItemsSource(EpisodesList, appliedEpisodes);
            }
        }
        finally
        {
            updatingSeasonPicker = false;
        }
    }

    /// <summary>Vergleicht zwei Staffellisten elementweise per Record-Gleichheit
    /// (Id, Nummer, Name, Bild, Jahr und die Episoden-Listenreferenz) — nicht
    /// nur die Ids, damit In-place-Neubauten als Änderung erkannt werden.</summary>
    /// <param name="a">Die bisher dem Picker zugewiesene Staffelliste.</param>
    /// <param name="b">Die Staffelliste des aktuellen Modells.</param>
    /// <returns><c>true</c>, wenn beide Listen gleichwertige Staffeln in gleicher Reihenfolge enthalten.</returns>
    private static bool SeasonsEqual(IReadOnlyList<SeasonItem> a, IReadOnlyList<SeasonItem> b) =>
        a.SequenceEqual(b);

    private void UpdateCollectionSection(MediaDetailsView details)
    {
        CollectionSection.IsVisible = details.HasMovies;
        if (details.HasMovies)
        {
            CollectionCollection.ItemsSource = details.Movies;
        }
    }

    private void OnSeasonSelected(object? sender, EventArgs e)
    {
        // Programmatische Vorwahl (beim Laden) nicht als Benutzerwahl werten.
        if (updatingSeasonPicker)
        {
            return;
        }

        // Echo-Guard: verzögerte Selection-Events (iOS feuert
        // SelectedIndexChanged u. U. zeitversetzt nach programmatischen
        // Updates) dürfen die bereits angewendete Modell-Auswahl nicht
        // erneut als Benutzerwahl melden.
        if (SeasonPicker.SelectedItem is SeasonItem season
            && season.Id != mvuHost.Model.MediaDetails.Details?.SelectedSeason?.Id)
        {
            _ = mvuHost.DispatchAsync(new SeasonSelectedMessage(season.Id));
        }
    }

    private void OnEpisodeTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is not EpisodeItem episode || episode.IsPlaceholder)
        {
            // Platzhalter fehlender Offline-Episoden sind nicht auswählbar.
            return;
        }

        // Episoden-Antippen öffnet die Episoden-Detailseite im
        // Serienkontext (Show/Season/Episode-Hints aus dem geladenen
        // Anzeigemodell — kein zusätzlicher Server-Roundtrip).
        var details = mvuHost.Model.MediaDetails.Details;
        _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(new HomeMediaItem
        {
            Id = episode.Id,
            MediaType = "tvshowepisode",
            Title = episode.Title,
            ShowId = details?.ShowId,
            SeasonId = details?.SelectedSeason?.Id,
            EpisodeId = episode.Id
        }));
    }

    private void OnCollectionMovieSelected(object? sender, SelectionChangedEventArgs e)
    {
        var movie = e.CurrentSelection.FirstOrDefault() as CollectionMovieItem;
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
        if (movie is null)
        {
            return;
        }

        // Auswahl eines Films der Sammlung öffnet dessen Detailseite.
        _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(new HomeMediaItem
        {
            Id = movie.Id,
            MediaType = "movie",
            Title = movie.Title,
            PosterPictureId = movie.PosterPictureId
        }));
    }

    private void OnFavoriteClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new FavoriteToggleRequestedMessage());
    }

    /// <summary>
    /// Die Playlist-Auswahl öffnen („Zu Playlist"): lädt die eigenen
    /// Playlists als Auswahlziele und klappt das Aktionsblatt auf —
    /// die Playlist-Wahl erfolgt per Klartextname, niemals per Id-
    /// Eingabe.
    /// </summary>
    /// <param name="sender">Der auslösende Schalter.</param>
    /// <param name="e">Die Ereignisdaten.</param>
    private void OnAddToPlaylistClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistAddTargetsRequestedMessage());
    }

    /// <summary>Bereits geöffnetes Auswahlblatt (einmalig je Auswahl-Zustand).</summary>
    private bool playlistPickerShowing;

    /// <summary>
    /// Öffnet das Playlist-Auswahlblatt, sobald die Ziele vorliegen:
    /// Klartext-Namen der eigenen Playlists im Aktionsblatt — die Wahl
    /// läuft als <see cref="PlaylistPickerDismissedMessage"/> zurück in
    /// den MVU-Kreislauf (Abbruch mit <c>null</c>). Ohne eigene
    /// Playlist weist ein Hinweisdialog auf die Anlage hin.
    /// </summary>
    /// <param name="state">Zustand der Detailseite.</param>
    private void MaybeShowPlaylistPicker(MediaDetailsState state)
    {
        if (!state.PlaylistPickerPending
            || state.IsLoadingPlaylistTargets
            || state.PlaylistTargets is null
            || playlistPickerShowing)
        {
            return;
        }

        playlistPickerShowing = true;
        _ = ShowPlaylistPickerAsync(state.PlaylistTargets);
    }

    /// <summary>
    /// Zeigt das Aktionsblatt mit den Playlist-Namen und meldet die
    /// Entscheidung dem MVU-Kreislauf.
    /// </summary>
    /// <param name="targets">Die eigenen Playlists des Benutzers.</param>
    /// <returns>Eine abgeschlossene Aufgabe nach der Auswahl.</returns>
    private async Task ShowPlaylistPickerAsync(IReadOnlyList<PlaylistListItem> targets)
    {
        try
        {
            if (targets.Count == 0)
            {
                await DisplayAlertAsync(
                    "Zu Playlist hinzufügen",
                    AppUpdate.PlaylistNoTargetsNotice,
                    "OK");
                _ = mvuHost.DispatchAsync(new PlaylistPickerDismissedMessage(null));
                return;
            }

            var names = targets.Select(t => t.Name).ToArray();
            var choice = await DisplayActionSheetAsync(
                "Zu Playlist hinzufügen", "Abbrechen", null, names);
            var target = choice is null || choice == "Abbrechen"
                ? null
                : targets.FirstOrDefault(t => t.Name == choice);
            _ = mvuHost.DispatchAsync(new PlaylistPickerDismissedMessage(target?.PlaylistId));
        }
        finally
        {
            playlistPickerShowing = false;
        }
    }

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        var details = mvuHost.Model.MediaDetails.Details;
        if (details is null || !details.IsPlayable)
        {
            return;
        }

        // Bei einer navigierten Episode ist der Episodentitel
        // aussagekräftiger als der Serienname des Kopfbereichs.
        _ = mvuHost.DispatchAsync(new PlayRequestedMessage(
            details.MediaType, details.MediaId,
            details.HighlightedEpisodeTitle ?? details.Title));
    }

    private void OnEpisodePlayClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is not EpisodeItem episode || episode.IsPlaceholder)
        {
            return;
        }

        // „Abspielen" direkt aus der Episodenliste — der Serienkontext
        // (Fortsetzungsliste) wird im Update aus den geladenen Details
        // gebildet.
        _ = mvuHost.DispatchAsync(new PlayRequestedMessage(
            "tvshowepisode", episode.Id, episode.Title));
    }

    private void OnDownloadClicked(object? sender, EventArgs e)
    {
        var details = mvuHost.Model.MediaDetails.Details;
        if (details is null || !details.IsPlayable)
        {
            return;
        }

        // Bei einer navigierten Episode ist der Episodentitel
        // aussagekräftiger als der Serienname des Kopfbereichs; der
        // Serienkontext geht ins Download-Manifest (Offline-Platzhalter).
        var message = new DownloadRequestedMessage(
            details.MediaType, details.MediaId,
            details.HighlightedEpisodeTitle ?? details.Title);
        if (MediaTypeNames.Normalize(details.MediaType) == "tvshowepisode")
        {
            var (seasonNumber, episodeNumber) = FindEpisodeNumbers(details, details.MediaId);
            message = message with
            {
                ShowId = details.ShowId,
                ShowTitle = details.Title,
                SeasonNumber = seasonNumber,
                EpisodeNumber = episodeNumber
            };
        }

        _ = mvuHost.DispatchAsync(message);
    }

    private void OnEpisodeDownloadClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is not EpisodeItem episode || episode.IsPlaceholder)
        {
            return;
        }

        // „Herunterladen" direkt aus der Episodenliste — Titel mit
        // Seriennamen für die Downloads-Liste; Serienkontext für das
        // Manifest (Offline-Platzhalter fehlender Folge-Episoden).
        var details = mvuHost.Model.MediaDetails.Details;
        var title = details is null || string.IsNullOrWhiteSpace(details.Title)
            ? episode.Title
            : $"{details.Title} – {episode.Title}";
        _ = mvuHost.DispatchAsync(new DownloadRequestedMessage(
            "tvshowepisode", episode.Id, title)
        {
            ShowId = details?.ShowId,
            ShowTitle = details?.Title,
            SeasonNumber = details?.SelectedSeason?.Number,
            EpisodeNumber = episode.Number
        });
    }

    /// <summary>
    /// Sucht Staffel- und Episodennummer einer Episode im geladenen
    /// Serienkontext der Detailansicht.
    /// </summary>
    /// <param name="details">Das geladene Detailmodell.</param>
    /// <param name="episodeId">Id der gesuchten Episode.</param>
    /// <returns>Tupel aus Staffel- und Episodennummer; beide Elemente <c>null</c>, wenn die Episode nicht gefunden wurde.</returns>
    private static (int?, int?) FindEpisodeNumbers(MediaDetailsView details, long episodeId)
    {
        foreach (var season in details.Seasons)
        {
            var episode = season.Episodes.FirstOrDefault(e => e.Id == episodeId);
            if (episode is not null)
            {
                return (season.Number, episode.Number);
            }
        }

        return (null, null);
    }
}
