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
    private byte[]? appliedHeaderBytes;

    /// <summary>Initialisiert eine neue Instanz der <see cref="MediaDetailsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; liefert den Detail-Zustand und nimmt die Benutzer-Nachrichten entgegen.</param>
    public MediaDetailsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
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
        // Nur einmal pro Seiteninstanz anfragen — Details laden bei jeder
        // Rückkehr neu anzufordern würde Staffel-Vorwahl und Favoriten-
        // Anzeige zurücksetzen.
        if (!requested && mediaId > 0 && !string.IsNullOrWhiteSpace(mediaType))
        {
            requested = true;
            _ = mvuHost.DispatchAsync(new MediaDetailsRequestedMessage(
                mediaType, mediaId, mediaTitle, showId, seasonId, episodeId));
        }
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(model));
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

        UpdateSeasonSection(details);
        UpdateCollectionSection(details);
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
            return;
        }

        updatingSeasonPicker = true;
        try
        {
            SeasonPicker.ItemsSource = details.Seasons.ToList();
            SeasonPicker.SelectedItem = details.SelectedSeason;
            EpisodesCollection.ItemsSource = details.SelectedSeasonEpisodes;
        }
        finally
        {
            updatingSeasonPicker = false;
        }
    }

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

        if (SeasonPicker.SelectedItem is SeasonItem season)
        {
            _ = mvuHost.DispatchAsync(new SeasonSelectedMessage(season.Id));
        }
    }

    private void OnEpisodeSelected(object? sender, SelectionChangedEventArgs e)
    {
        var episode = e.CurrentSelection.FirstOrDefault() as EpisodeItem;
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
        if (episode is null || episode.IsPlaceholder)
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
