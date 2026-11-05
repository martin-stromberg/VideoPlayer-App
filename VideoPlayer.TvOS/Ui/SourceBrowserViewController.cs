using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Browsing-Seite einer geöffneten Medienquelle — tvOS-Pendant der
    /// MAUI-<c>SourceOverviewPage</c>: Suchfeld (tvOS-Systemtastatur mit
    /// Diktat bzw. Continuity-Tastatur vom iPhone), Genre-Filterleiste
    /// inklusive „Alle" und ein vertikal scrollendes Poster-Raster, das
    /// am Listenende weitere Seiten nachlädt. Die Seite wird modal vom
    /// <see cref="TvosNavigator"/> präsentiert, rendert den
    /// <see cref="AppModel.SourceBrowser"/>-Stand und schließt per
    /// Menü-Taste der Siri Remote. Lade-, Leer- und Fehlerzustände
    /// (mit Wiederholmöglichkeit) sind fernbedienungstauglich sichtbar.
    /// </summary>
    public sealed class SourceBrowserViewController : UIViewController, IModelRenderable
    {
        // Kartengröße des Rasters (2:3-Poster plus Titelbereich, wie die
        // Poster-Reihen der Startseite).
        private static readonly CGSize ItemSize = new(300f, 514f);

        // Anzahl Einträge vor dem Listenende, ab der die Folgeseite angefordert wird.
        private const int NextPageLookahead = 5;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;

        private long sourceId;
        private string sourceName = string.Empty;

        private UILabel? titleLabel;
        private UITextField? searchField;
        private UICollectionView? genreCollection;
        private NSLayoutConstraint? genreHeightConstraint;
        private UICollectionView? itemsCollection;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;
        private UILabel? loadingMoreLabel;

        private GenreSource? genreSource;
        private ItemsSource? itemsSource;
        private IReadOnlyList<SourceGenreItem> appliedGenres = [];
        private IReadOnlyList<SourceMediaItem> appliedItems = [];
        private string? mirroredSearchText;
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="SourceBrowserViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Karten.</param>
        public SourceBrowserViewController(Func<AppMessage, Task> dispatch, TvosPosterImageService posters)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
        }

        /// <summary>Setzt den Quellen-Kontext der Seite (Aufruf des Navigators vor der Präsentation).</summary>
        /// <param name="sourceId">Id der geöffneten Quelle.</param>
        /// <param name="sourceName">Anzeigename der Quelle (Seitentitel).</param>
        public void SetSource(long sourceId, string sourceName)
        {
            this.sourceId = sourceId;
            this.sourceName = sourceName ?? string.Empty;
            Title = this.sourceName;
            if (IsViewLoaded)
            {
                titleLabel!.Text = this.sourceName;
            }
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = TvosControls.Title(sourceName);
            View.AddSubview(titleLabel);

            searchField = TvosControls.TextField("Suche");
            searchField.EditingDidEnd += (_, _) =>
                _ = dispatch(new SourceSearchSubmittedMessage(searchField.Text ?? string.Empty));
            View.AddSubview(searchField);

            var genreFlow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Horizontal,
                ItemSize = new CGSize(240f, 64f),
                MinimumInteritemSpacing = 24,
                SectionInset = new UIEdgeInsets(6, 0, 6, 0)
            };
            genreCollection = new UICollectionView(CGRect.Empty, genreFlow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsHorizontalScrollIndicator = false,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "GenreBar"
            };
            genreCollection.RegisterClassForCell(typeof(SourceGenreCell), SourceGenreCell.ReuseId);
            genreSource = new GenreSource(genre => _ = dispatch(new SourceGenreSelectedMessage(genre.Id)));
            genreCollection.Source = genreSource;
            View.AddSubview(genreCollection);
            genreHeightConstraint = genreCollection.HeightAnchor.ConstraintEqualTo(76);
            genreHeightConstraint.Active = true;

            var itemsFlow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Vertical,
                ItemSize = ItemSize,
                MinimumLineSpacing = 40,
                MinimumInteritemSpacing = 40,
                SectionInset = new UIEdgeInsets(30, 0, 30, 0)
            };
            itemsCollection = new UICollectionView(CGRect.Empty, itemsFlow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsVerticalScrollIndicator = true,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "SourceItemsGrid"
            };
            itemsCollection.RegisterClassForCell(typeof(HomeMediaItemCell), HomeMediaItemCell.ReuseId);
            itemsSource = new ItemsSource(
                posters,
                item => _ = dispatch(new SourceMediaItemSelectedMessage(item)),
                () => _ = dispatch(new SourceItemsNextPageRequestedMessage()));
            itemsCollection.Source = itemsSource;
            View.AddSubview(itemsCollection);

            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new SourceItemsRetryRequestedMessage()));
            stateView = TvosControls.Section(stateLabel, retryButton);
            stateView.Alignment = UIStackViewAlignment.Center;
            stateView.Hidden = true;
            View.AddSubview(stateView);

            loadingMoreLabel = TvosControls.Status("Weitere Einträge werden geladen…");
            loadingMoreLabel.Hidden = true;
            View.AddSubview(loadingMoreLabel);

            NSLayoutConstraint.ActivateConstraints(
            [
                titleLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                titleLabel.TopAnchor.ConstraintEqualTo(View.TopAnchor, 110),

                searchField.LeadingAnchor.ConstraintEqualTo(titleLabel.LeadingAnchor),
                searchField.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 30),
                searchField.WidthAnchor.ConstraintEqualTo(700),

                genreCollection.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                genreCollection.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -40),
                genreCollection.TopAnchor.ConstraintEqualTo(searchField.BottomAnchor, 24),

                itemsCollection.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 60),
                itemsCollection.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -60),
                itemsCollection.TopAnchor.ConstraintEqualTo(genreCollection.BottomAnchor, 10),
                itemsCollection.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -40),

                stateView.CenterXAnchor.ConstraintEqualTo(itemsCollection.CenterXAnchor),
                stateView.CenterYAnchor.ConstraintEqualTo(itemsCollection.CenterYAnchor),
                stateView.WidthAnchor.ConstraintLessThanOrEqualTo(itemsCollection.WidthAnchor, 0.6f),

                loadingMoreLabel.LeadingAnchor.ConstraintEqualTo(itemsCollection.LeadingAnchor, 30),
                loadingMoreLabel.BottomAnchor.ConstraintEqualTo(itemsCollection.BottomAnchor, -8)
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
            // Wiederherstellungspfad des Updates abdecken (Muster:
            // SourceOverviewPage.OnAppearing) — bei bereits aktivem
            // Browser dieser Quelle bleibt die Message wirkungslos.
            _ = dispatch(new SourceBrowserOpenedMessage(sourceId, sourceName));
        }

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var menuPressed = presses.ToArray<UIPress>()
                .Any(press => press.Type == UIPressType.Menu);
            if (menuPressed)
            {
                // Menü-Taste der Siri Remote: zurück zum Menürahmen
                // (Muster: PreparedTargetViewController). Der
                // Browser-Zustand im Modell bleibt erhalten.
                DismissViewController(true, null);
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
            var browser = model.SourceBrowser;
            if (!browser.IsActive || browser.SourceId != sourceId)
            {
                // Stand einer anderen/geöffneten Quelle nicht
                // übernehmen — der Navigator präsentiert den Browser
                // erneut mit aktuellem Kontext.
                return;
            }

            ApplySearchText(browser);
            ApplyGenres(browser);
            ApplyItems(browser);
            ApplyState(browser);
        }

        // Suchtext-Spiegelung: das Feld nur zurückschreiben, wenn sich
        // der Modell-Text geändert hat UND das Feld gerade nicht
        // bearbeitet wird — der eingegebene Text bleibt sichtbar
        // (Muster: mirroredSearchText der MAUI-Seite; behebt den
        // bekannten Fehler des Zurücksetzens des Suchtexts).
        private void ApplySearchText(SourceBrowserState browser)
        {
            if (searchField!.IsFirstResponder)
            {
                return;
            }

            if (!string.Equals(mirroredSearchText, browser.SearchText, StringComparison.Ordinal))
            {
                mirroredSearchText = browser.SearchText;
                searchField.Text = browser.SearchText;
            }
        }

        private void ApplyGenres(SourceBrowserState browser)
        {
            var visible = browser.ShowGenreFilter;
            genreCollection!.Hidden = !visible;
            genreHeightConstraint!.Constant = visible ? 76 : 0;
            if (!visible || appliedGenres.SequenceEqual(browser.Genres))
            {
                return;
            }

            appliedGenres = browser.Genres;
            genreSource!.SetItems(browser.Genres);
            genreCollection.ReloadData();
        }

        private void ApplyItems(SourceBrowserState browser)
        {
            if (appliedItems.SequenceEqual(browser.Items))
            {
                return;
            }

            appliedItems = browser.Items;
            itemsSource!.SetItems(browser.Items);
            itemsCollection!.ReloadData();
        }

        // Lade-, Leer- und Fehlerzustände des Rasters — Fehler zeigen
        // die Wiederholmöglichkeit; Nachladen läuft über den Hinweis
        // am unteren Rand.
        private void ApplyState(SourceBrowserState browser)
        {
            loadingMoreLabel!.Hidden = !browser.IsLoadingMore;

            if (browser.HasError)
            {
                stateLabel!.Text = browser.Error ?? "Die Einträge konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (browser.IsLoading && !browser.HasItems)
            {
                stateLabel.Text = "Einträge werden geladen…";
                retryButton.Hidden = true;
                stateView.Hidden = false;
                return;
            }

            if (browser.ShowEmpty)
            {
                stateLabel.Text = string.IsNullOrWhiteSpace(browser.SearchText)
                    ? "Keine Einträge in dieser Sicht."
                    : "Keine Einträge gefunden.";
                retryButton.Hidden = true;
                stateView.Hidden = false;
                return;
            }

            stateView.Hidden = true;
        }

        // Datenquelle/Delegat der Genre-Filterleiste.
        private sealed class GenreSource : UICollectionViewSource
        {
            private readonly Action<SourceGenreItem> onGenreSelected;
            private IReadOnlyList<SourceGenreItem> items = [];

            internal GenreSource(Action<SourceGenreItem> onGenreSelected) =>
                this.onGenreSelected = onGenreSelected;

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
                onGenreSelected(items[(int)indexPath.Item]);
        }

        // Datenquelle/Delegat des Eintrags-Rasters: posternde
        // HomeMediaItemCell-Karten, Auswahl- und
        // Listenende-Nachlade-Meldung an die Seite.
        private sealed class ItemsSource : UICollectionViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<SourceMediaItem> onItemSelected;
            private readonly Action onNextPage;
            private IReadOnlyList<SourceMediaItem> items = [];

            internal ItemsSource(
                TvosPosterImageService posters,
                Action<SourceMediaItem> onItemSelected,
                Action onNextPage)
            {
                this.posters = posters;
                this.onItemSelected = onItemSelected;
                this.onNextPage = onNextPage;
            }

            internal void SetItems(IReadOnlyList<SourceMediaItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (HomeMediaItemCell)collectionView.DequeueReusableCell(
                    HomeMediaItemCell.ReuseId, indexPath);
                var item = items[(int)indexPath.Item];
                cell.Update(
                    new HomeMediaItem
                    {
                        Id = item.Id,
                        MediaType = item.MediaType,
                        Title = item.Title,
                        PosterPictureId = item.PosterPictureId
                    },
                    posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onItemSelected(items[(int)indexPath.Item]);

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
