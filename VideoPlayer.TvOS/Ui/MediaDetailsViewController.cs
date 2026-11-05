using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Detailseite eines Mediums — tvOS-Pendant der
    /// MAUI-<c>MediaDetailsPage</c>: Kopfbereich mit Bildmaterial
    /// (<see cref="MediaDetailsState.HeaderImageBytes"/>), Titel,
    /// Genre-/Jahres-Metazeile, Handlung, „Gesehen"- und
    /// lokale-Datei-Kennzeichnung sowie den Aktionszeilen
    /// „Abspielen"/„Herunterladen"/„Favorit". Im Serienkontext folgen
    /// fokussierbare Staffel-Chips und die Episodenreihe
    /// (<see cref="EpisodeItemCell"/>); Filmsammlungen zeigen die
    /// enthaltenen Filme als Karussell. Die Seite wird modal gestapelt
    /// (Detail → Detail wie der Shell-Navigationsstack), rendert
    /// <see cref="AppModel.MediaDetails"/> identitätsgeprüft und
    /// meldet ihre Identität beim Erscheinen als
    /// <see cref="MediaDetailsRequestedMessage"/> zurück in den
    /// MVU-Zyklus — die Menü-Taste der Siri Remote schließt die Seite.
    /// </summary>
    public sealed class MediaDetailsViewController : UIViewController, IModelRenderable
    {
        private static readonly CGSize EpisodeItemSize = new(480f, 330f);
        private static readonly CGSize SeasonChipSize = new(300f, 64f);
        private static readonly CGSize MovieItemSize = new(300f, 514f);

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;
        private readonly Action<MediaDetailsViewController> onDismissed;

        private string mediaType = string.Empty;
        private long mediaId;
        private string? mediaTitle;
        private long? showId;
        private long? seasonId;
        private long? episodeId;
        private bool requested;

        private UIImageView? headerImage;
        private UILabel? titleLabel;
        private UILabel? metaLabel;
        private UILabel? watchedBadge;
        private UILabel? localBadge;
        private UILabel? offlineBadge;
        private UILabel? plotLabel;
        private UIButton? playButton;
        private UIButton? downloadButton;
        private UIButton? favoriteButton;
        private UILabel? favoriteErrorLabel;
        private UILabel? downloadNoticeLabel;
        private UICollectionView? seasonCollection;
        private UICollectionView? episodesCollection;
        private UILabel? episodesHeading;
        private UIView? episodesHeadingWrap;
        private HomeCarouselView? moviesCarousel;
        private UIStackView? contentView;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;

        private SeasonSource? seasonSource;
        private EpisodeSource? episodeSource;
        private IReadOnlyList<SourceGenreItem> appliedSeasons = [];
        private IReadOnlyList<EpisodeItem> appliedEpisodes = [];
        private byte[]? appliedHeaderBytes;
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="MediaDetailsViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-/Still-Ladedienst der Karten.</param>
        /// <param name="onDismissed">Freigabe-Callback des Navigators beim Schließen der Seite.</param>
        public MediaDetailsViewController(
            Func<AppMessage, Task> dispatch,
            TvosPosterImageService posters,
            Action<MediaDetailsViewController> onDismissed)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
            this.onDismissed = onDismissed ?? throw new ArgumentNullException(nameof(onDismissed));
        }

        /// <summary>Setzt die Medium-Identität der Seite (Aufruf des Navigators vor der Präsentation, Muster <c>SetSource</c>).</summary>
        /// <param name="mediaType">Medientyp (CLR-Konvention).</param>
        /// <param name="mediaId">Id des Mediums.</param>
        /// <param name="title">Fallback-Titel bis zum Laden der Details.</param>
        /// <param name="showId">Optionaler Hint: Id der zugehörigen Serie.</param>
        /// <param name="seasonId">Optionaler Hint: Id der vorzuwählenden Staffel.</param>
        /// <param name="episodeId">Optionaler Hint: Id der hervorzuhebenden Episode.</param>
        public void SetMedium(
            string mediaType,
            long mediaId,
            string? title,
            long? showId,
            long? seasonId,
            long? episodeId)
        {
            this.mediaType = mediaType ?? string.Empty;
            this.mediaId = mediaId;
            mediaTitle = title;
            this.showId = showId;
            this.seasonId = seasonId;
            this.episodeId = episodeId;
            Title = title;
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var scroll = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(scroll);

            var contentStack = new UIStackView
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Fill,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 34,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            contentView = contentStack;
            scroll.AddSubview(contentStack);

            BuildHeader(contentStack);
            BuildSeasonSection(contentStack);
            BuildEpisodesSection(contentStack);
            BuildMoviesSection(contentStack);

            // Lade-/Fehlerzustand über dem Scrollbereich — er bleibt
            // sichtbar, solange der Inhalt verborgen ist.
            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new MediaDetailsRequestedMessage(
                    mediaType, mediaId, mediaTitle, showId, seasonId, episodeId)));
            stateView = TvosControls.Section(stateLabel, retryButton);
            stateView.Alignment = UIStackViewAlignment.Center;
            stateView.Hidden = true;
            View.AddSubview(stateView);

            NSLayoutConstraint.ActivateConstraints(
            [
                scroll.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                scroll.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                scroll.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                scroll.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),

                contentStack.LeadingAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.LeadingAnchor),
                contentStack.TrailingAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.TrailingAnchor),
                contentStack.TopAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.TopAnchor),
                contentStack.BottomAnchor.ConstraintEqualTo(scroll.ContentLayoutGuide.BottomAnchor),
                contentStack.WidthAnchor.ConstraintEqualTo(scroll.FrameLayoutGuide.WidthAnchor),

                stateView.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                stateView.CenterYAnchor.ConstraintEqualTo(View.CenterYAnchor),
                stateView.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.6f)
            ]);

            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        private void BuildHeader(UIStackView content)
        {
            var header = new UIView
            {
                ClipsToBounds = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            header.HeightAnchor.ConstraintEqualTo(620).Active = true;

            headerImage = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            header.AddSubview(headerImage);

            // Leseflügel über dem Bild — dunkle Fläche statt Layer-
            // Gradient (kein Layout-Abgleich nötig).
            var scrim = new UIView
            {
                BackgroundColor = UIColor.FromWhiteAlpha(0f, 0.55f),
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            header.AddSubview(scrim);

            titleLabel = TvosControls.Title(string.Empty);
            titleLabel.Font = UIFont.BoldSystemFontOfSize(52);

            metaLabel = TvosControls.Body();
            metaLabel.TextColor = UIColor.LightGray;

            watchedBadge = Badge("✓ Gesehen");
            localBadge = Badge("Auf diesem Gerät");
            offlineBadge = Badge("Offline-Stand");
            var badgeRow = new UIStackView([watchedBadge, localBadge, offlineBadge])
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Center,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 16,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            plotLabel = TvosControls.Body();
            plotLabel.Lines = 4;

            playButton = TvosControls.Button("▶ Abspielen", OnPlay);
            downloadButton = TvosControls.Button("Herunterladen", OnDownload);
            favoriteButton = TvosControls.Button("☆ Favorit", OnFavorite);
            var actions = new UIStackView([playButton, downloadButton, favoriteButton])
            {
                Axis = UILayoutConstraintAxis.Horizontal,
                Alignment = UIStackViewAlignment.Center,
                Distribution = UIStackViewDistribution.FillProportionally,
                Spacing = 24,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "DetailsActions"
            };

            favoriteErrorLabel = TvosControls.Error();
            downloadNoticeLabel = TvosControls.Status();

            var textStack = new UIStackView(
            [
                titleLabel,
                metaLabel,
                badgeRow,
                plotLabel,
                actions,
                favoriteErrorLabel,
                downloadNoticeLabel
            ])
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 14,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            header.AddSubview(textStack);

            NSLayoutConstraint.ActivateConstraints(
            [
                headerImage.LeadingAnchor.ConstraintEqualTo(header.LeadingAnchor),
                headerImage.TrailingAnchor.ConstraintEqualTo(header.TrailingAnchor),
                headerImage.TopAnchor.ConstraintEqualTo(header.TopAnchor),
                headerImage.BottomAnchor.ConstraintEqualTo(header.BottomAnchor),

                scrim.LeadingAnchor.ConstraintEqualTo(header.LeadingAnchor),
                scrim.TrailingAnchor.ConstraintEqualTo(header.TrailingAnchor),
                scrim.TopAnchor.ConstraintEqualTo(header.TopAnchor),
                scrim.BottomAnchor.ConstraintEqualTo(header.BottomAnchor),

                textStack.LeadingAnchor.ConstraintEqualTo(header.LeadingAnchor, 90),
                textStack.TrailingAnchor.ConstraintEqualTo(header.TrailingAnchor, -90),
                textStack.BottomAnchor.ConstraintEqualTo(header.BottomAnchor, -30),
                textStack.WidthAnchor.ConstraintLessThanOrEqualTo(1200)
            ]);

            content.AddArrangedSubview(header);
        }

        private static UILabel Badge(string text)
        {
            var label = new UILabel
            {
                Text = text,
                TextColor = UIColor.Black,
                Font = UIFont.BoldSystemFontOfSize(22),
                TextAlignment = UITextAlignment.Center,
                BackgroundColor = UIColor.FromWhiteAlpha(1f, 0.85f),
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            label.Layer.CornerRadius = 10;
            label.ClipsToBounds = true;
            label.HeightAnchor.ConstraintEqualTo(40).Active = true;
            label.WidthAnchor.ConstraintGreaterThanOrEqualTo(170).Active = true;
            return label;
        }

        private void BuildSeasonSection(UIStackView content)
        {
            var flow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Horizontal,
                ItemSize = SeasonChipSize,
                MinimumInteritemSpacing = 24,
                SectionInset = new UIEdgeInsets(12, 90, 12, 90)
            };
            seasonCollection = new UICollectionView(CGRect.Empty, flow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsHorizontalScrollIndicator = false,
                RemembersLastFocusedIndexPath = true,
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "DetailsSeasonBar"
            };
            seasonCollection.HeightAnchor.ConstraintEqualTo(88).Active = true;
            seasonCollection.RegisterClassForCell(typeof(SourceGenreCell), SourceGenreCell.ReuseId);
            seasonSource = new SeasonSource(
                season => _ = dispatch(new SeasonSelectedMessage(season.Id)));
            seasonCollection.Source = seasonSource;
            content.AddArrangedSubview(seasonCollection);
        }

        private void BuildEpisodesSection(UIStackView content)
        {
            episodesHeading = TvosControls.Heading("Episoden");
            episodesHeading.Hidden = true;
            var headingWrap = new UIView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true
            };
            headingWrap.AddSubview(episodesHeading);
            NSLayoutConstraint.ActivateConstraints(
            [
                episodesHeading.LeadingAnchor.ConstraintEqualTo(headingWrap.LeadingAnchor, 90),
                episodesHeading.TopAnchor.ConstraintEqualTo(headingWrap.TopAnchor),
                episodesHeading.BottomAnchor.ConstraintEqualTo(headingWrap.BottomAnchor)
            ]);

            var flow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Horizontal,
                ItemSize = EpisodeItemSize,
                MinimumInteritemSpacing = 40,
                SectionInset = new UIEdgeInsets(12, 90, 12, 90)
            };
            episodesCollection = new UICollectionView(CGRect.Empty, flow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsHorizontalScrollIndicator = false,
                RemembersLastFocusedIndexPath = true,
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "DetailsEpisodesRow"
            };
            episodesCollection.HeightAnchor.ConstraintEqualTo(360).Active = true;
            episodesCollection.RegisterClassForCell(typeof(EpisodeItemCell), EpisodeItemCell.ReuseId);
            episodeSource = new EpisodeSource(posters, OnEpisodeSelected);
            episodesCollection.Source = episodeSource;

            content.AddArrangedSubview(headingWrap);
            content.AddArrangedSubview(episodesCollection);
            episodesHeadingWrap = headingWrap;
        }

        private void BuildMoviesSection(UIStackView content)
        {
            moviesCarousel = new HomeCarouselView(
                "Filme der Sammlung",
                MovieItemSize,
                posters,
                item => _ = dispatch(new MediaItemSelectedMessage(item)),
                "DetailsMoviesRow")
            {
                Hidden = true
            };
            moviesCarousel.HeightAnchor.ConstraintEqualTo(640).Active = true;
            content.AddArrangedSubview(moviesCarousel);
        }

        /// <inheritdoc/>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            // Einmal je Instanz — und erneut, wenn der globale
            // Detail-Slot inzwischen eine andere Identität beschreibt
            // (Rückkehr von einer geschobenen Detailseite, MAUI-Muster).
            var state = lastModel?.MediaDetails;
            var ownType = MediaTypeNames.Normalize(mediaType);
            var identityMatches = string.Equals(state?.MediaType, ownType, StringComparison.Ordinal)
                && state?.MediaId == mediaId;
            if ((!requested || !identityMatches) && mediaId > 0 && !string.IsNullOrWhiteSpace(mediaType))
            {
                requested = true;
                _ = dispatch(new MediaDetailsRequestedMessage(
                    mediaType, mediaId, mediaTitle, showId, seasonId, episodeId));
            }
        }

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var menuPressed = presses.ToArray<UIPress>()
                .Any(press => press.Type == UIPressType.Menu);
            if (menuPressed)
            {
                // Menü-Taste der Siri Remote: eine modale Ebene zurück —
                // darunterliegende Detailseiten fordern ihre Identität
                // beim nächsten ViewWillAppear erneut an.
                DismissViewController(true, () => onDismissed(this));
                return;
            }

            base.PressesBegan(presses, evt);
        }

        /// <inheritdoc/>
        public void Render(AppModel model)
        {
            lastModel = model;
            if (!IsViewLoaded)
            {
                return;
            }

            ApplyRender(model);
        }

        private void ApplyRender(AppModel model)
        {
            var state = model.MediaDetails;
            var isRequested = string.Equals(
                    state.MediaType, MediaTypeNames.Normalize(mediaType), StringComparison.Ordinal)
                && state.MediaId == mediaId;
            var details = isRequested ? state.Details : null;

            ApplyState(state, isRequested);
            if (details is null)
            {
                // Lade-, Fehler- oder Fremd-Identität — kein Inhalt.
                contentView!.Hidden = true;
                return;
            }

            contentView!.Hidden = false;

            // Kopfbereich: bei ausgewählter Episode deren Angaben
            // (Still + Texte folgen dem MVU-View), sonst die des Mediums.
            titleLabel!.Text = details.HeaderTitle;
            metaLabel!.Text = details.HeaderMeta ?? string.Empty;
            metaLabel.Hidden = string.IsNullOrWhiteSpace(details.HeaderMeta);
            plotLabel!.Text = details.HeaderPlot ?? string.Empty;
            plotLabel.Hidden = string.IsNullOrWhiteSpace(details.HeaderPlot);

            var isWatched = details.SelectedEpisode?.IsWatched ?? details.IsWatched;
            watchedBadge!.Hidden = !isWatched;
            localBadge!.Hidden = !details.HasLocalFile;
            offlineBadge!.Hidden = !details.IsOfflineSnapshot;

            ApplyHeaderImage(state);

            playButton!.Hidden = !details.IsPlayable
                || (details.IsOfflineSnapshot && !details.HasLocalFile);
            downloadButton!.Hidden = !details.IsPlayable || details.IsOfflineSnapshot;
            favoriteButton!.SetTitle($"{details.FavoriteStarText} Favorit", UIControlState.Normal);
            favoriteButton.Enabled = !state.IsFavoriteUpdating && !details.IsOfflineSnapshot;

            favoriteErrorLabel!.Text = state.FavoriteError ?? string.Empty;
            downloadNoticeLabel!.Text = state.DownloadNotice ?? string.Empty;
            downloadNoticeLabel.TextColor = state.DownloadNoticeIsError
                ? UIColor.SystemRed
                : UIColor.LightGray;

            ApplySeasons(details);
            ApplyEpisodes(details);
            ApplyMovies(details);
        }

        // Lade-/Fehler-/Identitäts-Zustände: ohne eigene Identität bleibt
        // die Seite neutral (sie liegt unter einer anderen Detailseite),
        // bei Fehler bietet sie die Wiederholmöglichkeit an.
        private void ApplyState(MediaDetailsState state, bool isRequested)
        {
            if (!isRequested)
            {
                stateLabel!.Text = "Details werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            if (state.HasError)
            {
                stateLabel!.Text = state.Error ?? "Die Details konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (state.IsLoading || !state.HasDetails)
            {
                stateLabel!.Text = "Details werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            stateView!.Hidden = true;
        }

        private void ApplyHeaderImage(MediaDetailsState state)
        {
            if (ReferenceEquals(appliedHeaderBytes, state.HeaderImageBytes))
            {
                return;
            }

            appliedHeaderBytes = state.HeaderImageBytes;
            headerImage!.Image = appliedHeaderBytes is { Length: > 0 } bytes
                ? UIImage.LoadFromData(NSData.FromArray(bytes))
                : null;
        }

        private void ApplySeasons(MediaDetailsView details)
        {
            seasonCollection!.Hidden = !details.HasSeasons;
            if (!details.HasSeasons)
            {
                appliedSeasons = [];
                return;
            }

            var selectedId = details.SelectedSeason?.Id;
            var seasons = details.Seasons
                .Select(s => new SourceGenreItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    IsSelected = s.Id == selectedId
                })
                .ToList();
            if (appliedSeasons.SequenceEqual(seasons))
            {
                return;
            }

            appliedSeasons = seasons;
            seasonSource!.SetItems(seasons);
            seasonCollection.ReloadData();
        }

        private void ApplyEpisodes(MediaDetailsView details)
        {
            var visible = details.HasSeasons;
            episodesHeading!.Hidden = !visible;
            episodesHeadingWrap!.Hidden = !visible;
            episodesCollection!.Hidden = !visible;
            if (!visible)
            {
                appliedEpisodes = [];
                return;
            }

            var episodes = details.SelectedSeasonEpisodes;
            if (appliedEpisodes.SequenceEqual(episodes))
            {
                return;
            }

            appliedEpisodes = episodes;
            episodeSource!.SetItems(episodes);
            episodesCollection.ReloadData();
        }

        private void ApplyMovies(MediaDetailsView details)
        {
            moviesCarousel!.Hidden = !details.HasMovies;
            if (!details.HasMovies)
            {
                return;
            }

            moviesCarousel.SetItems(
                details.Movies
                    .Select(m => new HomeMediaItem
                    {
                        Id = m.Id,
                        MediaType = "movie",
                        Title = m.Title,
                        PosterPictureId = m.PosterPictureId
                    })
                    .ToList());
        }

        private void OnPlay()
        {
            var details = lastModel?.MediaDetails.Details;
            if (details is null || !details.IsPlayable)
            {
                return;
            }

            // Bei einer navigierten Episode ist der Episodentitel
            // aussagekräftiger als der Serienname (MAUI-Muster).
            _ = dispatch(new PlayRequestedMessage(
                details.MediaType, details.MediaId,
                details.HighlightedEpisodeTitle ?? details.Title));
        }

        private void OnDownload()
        {
            var details = lastModel?.MediaDetails.Details;
            if (details is null || !details.IsPlayable)
            {
                return;
            }

            // Serienkontext der Episoden-Anforderung wie in MAUI — der
            // tvOS-Handler meldet den vorbereiteten Download als Hinweis.
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
                    EpisodeNumber = episodeNumber,
                    PictureId = details.SelectedEpisode?.PosterPictureId
                        ?? details.HeaderPictureId
                        ?? details.PosterPictureId
                };
            }
            else
            {
                message = message with { PictureId = details.PosterPictureId };
            }

            _ = dispatch(message);
        }

        private void OnFavorite() => _ = dispatch(new FavoriteToggleRequestedMessage());

        private void OnEpisodeSelected(EpisodeItem episode)
        {
            var details = lastModel?.MediaDetails.Details;
            _ = dispatch(new MediaItemSelectedMessage(new HomeMediaItem
            {
                Id = episode.Id,
                MediaType = "tvshowepisode",
                Title = episode.Title,
                PosterPictureId = episode.PosterPictureId,
                IsPlaceholder = episode.IsPlaceholder,
                ShowId = details?.ShowId,
                SeasonId = details?.SelectedSeason?.Id,
                EpisodeId = episode.Id
            }));
        }

        // Staffel-/Episodennummer einer Episode aus dem Serienkontext
        // (Pendant zu FindEpisodeNumbers der MAUI-Seite).
        private static (int? SeasonNumber, int? EpisodeNumber) FindEpisodeNumbers(
            MediaDetailsView details, long episodeId)
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

        // Datenquelle/Delegat der Staffel-Chips: Auswahl meldet die
        // Staffel-Id in den MVU-Zyklus (SeasonSelectedMessage).
        private sealed class SeasonSource : UICollectionViewSource
        {
            private readonly Action<SourceGenreItem> onSeasonSelected;
            private IReadOnlyList<SourceGenreItem> items = [];

            internal SeasonSource(Action<SourceGenreItem> onSeasonSelected) =>
                this.onSeasonSelected = onSeasonSelected;

            internal void SetItems(IReadOnlyList<SourceGenreItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (SourceGenreCell)collectionView.DequeueReusableCell(
                    SourceGenreCell.ReuseId, indexPath);
                cell.Update(items[(int)indexPath.Item]);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onSeasonSelected(items[(int)indexPath.Item]);
        }

        // Datenquelle/Delegat der Episodenreihe: liefert die
        // EpisodeItemCell-Karten und meldet die Auswahl an die Seite.
        private sealed class EpisodeSource : UICollectionViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<EpisodeItem> onEpisodeSelected;
            private IReadOnlyList<EpisodeItem> items = [];

            internal EpisodeSource(
                TvosPosterImageService posters,
                Action<EpisodeItem> onEpisodeSelected)
            {
                this.posters = posters;
                this.onEpisodeSelected = onEpisodeSelected;
            }

            internal void SetItems(IReadOnlyList<EpisodeItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (EpisodeItemCell)collectionView.DequeueReusableCell(
                    EpisodeItemCell.ReuseId, indexPath);
                cell.Update(items[(int)indexPath.Item], posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onEpisodeSelected(items[(int)indexPath.Item]);
        }
    }
}
