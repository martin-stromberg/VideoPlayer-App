using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Detailseite einer Playlist — tvOS-Pendant der
    /// MAUI-<c>PlaylistDetailsPage</c> (rein lesend): Kopfbereich mit
    /// Cover, Name, Zusammenfassung (Eintragszahl und Sortiermodus),
    /// Genres und Beschreibung sowie der Aktion „Abspielen" (gesamte
    /// Playlist ab dem ersten abspielbaren Eintrag über den
    /// serverseitigen Wiedergabestart). Darunter das vertikal
    /// scrollende Poster-Raster der Einträge in serverseitiger
    /// Reihenfolge mit Titel, übergeordnetem Kontext und seitenweisem
    /// Nachladen (<c>api/playlists/{id}/entries/paged</c>). Die Auswahl
    /// eines Eintrags läuft über den MVU-Kreislauf — das Update
    /// entscheidet zwischen Playlist-Wiedergabe ab diesem Eintrag und
    /// der Medien-Detailseite. Die Seite wird modal vom
    /// <see cref="TvosNavigator"/> präsentiert, rendert
    /// <see cref="AppModel.PlaylistDetails"/> identitätsgeprüft und
    /// schließt per Menü-Taste der Siri Remote. Verwaltungsaktionen
    /// existieren auf tvOS bewusst nicht.
    /// </summary>
    public sealed class PlaylistDetailsViewController : UIViewController, IModelRenderable
    {
        // Kartengröße des Eintrags-Rasters (2:3-Poster plus
        // Titel-/Kontextbereich).
        private static readonly CGSize EntryItemSize = new(300f, 560f);

        // Anzahl Einträge vor dem Listenende, ab der die Folgeseite angefordert wird.
        private const int NextPageLookahead = 5;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;
        private readonly Action<PlaylistDetailsViewController> onDismissed;

        private long playlistId;
        private string playlistName = string.Empty;

        private UIImageView? coverView;
        private UILabel? titleLabel;
        private UILabel? summaryLabel;
        private UILabel? genreLabel;
        private UILabel? descriptionLabel;
        private UIButton? playButton;
        private UICollectionView? entriesCollection;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;
        private UILabel? loadingMoreLabel;

        private EntriesSource? entriesSource;
        private IReadOnlyList<PlaylistEntryItem> appliedEntries = [];
        private long? appliedCoverPlaylistId;
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistDetailsViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-/Cover-Ladedienst der Karten.</param>
        /// <param name="onDismissed">Freigabe-Callback des Navigators beim Schließen der Seite.</param>
        public PlaylistDetailsViewController(
            Func<AppMessage, Task> dispatch,
            TvosPosterImageService posters,
            Action<PlaylistDetailsViewController> onDismissed)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
            this.onDismissed = onDismissed ?? throw new ArgumentNullException(nameof(onDismissed));
        }

        /// <summary>Setzt die Playlist-Identität der Seite (Aufruf des Navigators vor der Präsentation, Muster <c>SetSource</c>).</summary>
        /// <param name="playlistId">Id der geöffneten Playlist.</param>
        /// <param name="name">Anzeigename der Playlist (Seitentitel-Fallback).</param>
        public void SetPlaylist(long playlistId, string name)
        {
            this.playlistId = playlistId;
            playlistName = name ?? string.Empty;
            Title = playlistName;
            if (IsViewLoaded)
            {
                titleLabel!.Text = playlistName;
            }
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            coverView = new UIImageView
            {
                ContentMode = UIViewContentMode.ScaleAspectFill,
                ClipsToBounds = true,
                BackgroundColor = UIColor.FromWhiteAlpha(0.14f, 1f),
                Hidden = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            coverView.Layer.CornerRadius = 14;
            View.AddSubview(coverView);

            titleLabel = TvosControls.Title(playlistName);
            titleLabel.Font = UIFont.BoldSystemFontOfSize(52);

            summaryLabel = TvosControls.Body();
            genreLabel = TvosControls.Body();
            descriptionLabel = TvosControls.Body();
            descriptionLabel.Lines = 3;

            playButton = TvosControls.Button("▶ Abspielen", OnPlay);

            var textStack = new UIStackView(
            [
                titleLabel,
                summaryLabel,
                genreLabel,
                descriptionLabel,
                playButton
            ])
            {
                Axis = UILayoutConstraintAxis.Vertical,
                Alignment = UIStackViewAlignment.Leading,
                Distribution = UIStackViewDistribution.Fill,
                Spacing = 14,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(textStack);

            var flow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Vertical,
                ItemSize = EntryItemSize,
                MinimumLineSpacing = 40,
                MinimumInteritemSpacing = 40,
                SectionInset = new UIEdgeInsets(20, 0, 30, 0)
            };
            entriesCollection = new UICollectionView(CGRect.Empty, flow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsVerticalScrollIndicator = true,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "PlaylistEntriesGrid"
            };
            entriesCollection.RegisterClassForCell(typeof(PlaylistEntryCell), PlaylistEntryCell.ReuseId);
            entriesSource = new EntriesSource(
                posters,
                entry => _ = dispatch(new PlaylistEntrySelectedMessage(entry)),
                () => _ = dispatch(new PlaylistEntriesNextPageRequestedMessage()));
            entriesCollection.Source = entriesSource;
            View.AddSubview(entriesCollection);

            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new PlaylistEntriesRetryRequestedMessage()));
            stateView = TvosControls.Section(stateLabel, retryButton);
            stateView.Alignment = UIStackViewAlignment.Center;
            stateView.Hidden = true;
            View.AddSubview(stateView);

            loadingMoreLabel = TvosControls.Status();
            loadingMoreLabel.Text = "Weitere Einträge werden geladen…";
            loadingMoreLabel.Hidden = true;
            View.AddSubview(loadingMoreLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                coverView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                coverView.TopAnchor.ConstraintEqualTo(View.TopAnchor, 90),
                coverView.WidthAnchor.ConstraintEqualTo(360),
                coverView.HeightAnchor.ConstraintEqualTo(200),

                textStack.LeadingAnchor.ConstraintEqualTo(coverView.TrailingAnchor, 40),
                textStack.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -90),
                textStack.TopAnchor.ConstraintEqualTo(coverView.TopAnchor),
                textStack.BottomAnchor.ConstraintLessThanOrEqualTo(coverView.BottomAnchor, 40),

                entriesCollection.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 60),
                entriesCollection.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -60),
                entriesCollection.TopAnchor.ConstraintEqualTo(coverView.BottomAnchor, 30),
                entriesCollection.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -40),

                stateView.CenterXAnchor.ConstraintEqualTo(entriesCollection.CenterXAnchor),
                stateView.CenterYAnchor.ConstraintEqualTo(entriesCollection.CenterYAnchor),
                stateView.WidthAnchor.ConstraintLessThanOrEqualTo(entriesCollection.WidthAnchor, 0.6f),

                loadingMoreLabel.LeadingAnchor.ConstraintEqualTo(entriesCollection.LeadingAnchor, 30),
                loadingMoreLabel.BottomAnchor.ConstraintEqualTo(entriesCollection.BottomAnchor, -8)
            ]);

            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        /// <inheritdoc/>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            // Wiederherstellungspfad (Muster: PlaylistDetailsPage
            // .OnAppearing): bei geöffneter Playlist ohne passenden
            // Zustand werden Kopfdaten und erste Eintragsseite geladen;
            // bei Rückkehr auf dieselbe Playlist bleibt der Stand
            // erhalten — das Update deduppt über die Playlist-Id.
            var details = lastModel?.PlaylistDetails;
            if (details is null || details.PlaylistId != playlistId || !details.Attempted)
            {
                _ = dispatch(new PlaylistDetailsOpenedMessage(playlistId, playlistName));
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
                // der Detail-Zustand im Modell bleibt erhalten.
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
            var details = model.PlaylistDetails;
            if (details.PlaylistId != playlistId)
            {
                // Stand einer anderen Playlist nicht übernehmen — der
                // Navigator präsentiert die Seite erneut mit aktuellem
                // Kontext (Identitäts-Guard, Muster:
                // SourceBrowserViewController).
                return;
            }

            titleLabel!.Text = string.IsNullOrWhiteSpace(details.Name)
                ? playlistName
                : details.Name;
            summaryLabel!.Text = details.SummaryText;
            genreLabel!.Text = details.GenreText;
            genreLabel.Hidden = !details.HasGenreText;
            descriptionLabel!.Text = details.Description ?? string.Empty;
            descriptionLabel.Hidden = !details.HasDescription;
            playButton!.Hidden = !details.IsActive;

            ApplyCover(details);
            ApplyEntries(details);
            ApplyState(details);
        }

        private void ApplyCover(PlaylistDetailsState details)
        {
            if (appliedCoverPlaylistId == details.CoverPlaylistId)
            {
                return;
            }

            appliedCoverPlaylistId = details.CoverPlaylistId;
            coverView!.Image = null;
            coverView.Hidden = true;
            if (details.CoverPlaylistId is > 0)
            {
                _ = LoadCoverAsync(details.CoverPlaylistId.Value);
            }
        }

        private async Task LoadCoverAsync(long coverPlaylistId)
        {
            var image = await posters.GetPlaylistCoverAsync(coverPlaylistId);
            if (image is null || appliedCoverPlaylistId != coverPlaylistId)
            {
                return;
            }

            coverView!.Image = image;
            coverView.Hidden = false;
        }

        private void ApplyEntries(PlaylistDetailsState details)
        {
            if (appliedEntries.SequenceEqual(details.Entries))
            {
                return;
            }

            appliedEntries = details.Entries;
            entriesSource!.SetItems(details.Entries);
            entriesCollection!.ReloadData();
        }

        // Lade-, Leer- und Fehlerzustände des Rasters — Fehler zeigen
        // die Wiederholmöglichkeit; Nachladen läuft über den Hinweis
        // am unteren Rand (Muster: SourceBrowserViewController).
        private void ApplyState(PlaylistDetailsState details)
        {
            loadingMoreLabel!.Hidden = !details.IsLoadingMore;

            if (details.HasError && !details.HasItems)
            {
                stateLabel!.Text = details.Error ?? "Die Einträge konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (details.IsLoading && !details.HasItems)
            {
                stateLabel!.Text = "Einträge werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            if (details.ShowEmpty)
            {
                stateLabel!.Text = "Diese Playlist enthält keine Einträge.";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            // Anhänge-Fehler (Liste steht, HasMore bleibt): Hinweis im
            // Ladebereich statt Vollflächen-Fehler.
            if (details.HasError)
            {
                stateLabel!.Text = details.Error;
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            stateView!.Hidden = true;
        }

        private void OnPlay() =>
            _ = dispatch(new PlaylistPlaybackStartRequestedMessage(playlistId, null));

        // Datenquelle/Delegat des Eintrags-Rasters: posternde
        // PlaylistEntryCell-Karten, Auswahl- und
        // Listenende-Nachlade-Meldung an die Seite.
        private sealed class EntriesSource : UICollectionViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<PlaylistEntryItem> onEntrySelected;
            private readonly Action onNextPage;
            private IReadOnlyList<PlaylistEntryItem> items = [];

            internal EntriesSource(
                TvosPosterImageService posters,
                Action<PlaylistEntryItem> onEntrySelected,
                Action onNextPage)
            {
                this.posters = posters;
                this.onEntrySelected = onEntrySelected;
                this.onNextPage = onNextPage;
            }

            internal void SetItems(IReadOnlyList<PlaylistEntryItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (PlaylistEntryCell)collectionView.DequeueReusableCell(
                    PlaylistEntryCell.ReuseId, indexPath);
                cell.Update(items[(int)indexPath.Item], posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onEntrySelected(items[(int)indexPath.Item]);

            /// <inheritdoc/>
            public override void WillDisplayCell(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
            {
                // Annäherung an das Listenende → Folgeseite anfordern;
                // das Update dedupliziert über IsLoadingMore/HasMore.
                if (indexPath.Item >= items.Count - NextPageLookahead)
                {
                    onNextPage();
                }
            }
        }
    }
}
