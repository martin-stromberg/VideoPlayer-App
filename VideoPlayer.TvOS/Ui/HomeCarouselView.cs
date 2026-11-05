using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Eine horizontale Karussell-Reihe der Startseite: Sektionstitel
    /// über einer horizontal scrollenden <see cref="UICollectionView"/>
    /// mit <see cref="HomeMediaItemCell"/>-Karten. Die Reihe ist per
    /// Siri Remote fokussierbar und scrollbar (Focus-Engine);
    /// <c>RemembersLastFocusedIndexPath</c> lässt den Fokus beim
    /// Reihenwechsel nachvollziehbar an der zuletzt fokussierten Karte
    /// fortsetzen. Leere Listen blenden die Sektion auf Seitenebene aus
    /// — die Reihe selbst rendert dann schlicht keine Karten.
    /// </summary>
    internal sealed class HomeCarouselView : UIView
    {
        // Zusätzlicher vertikaler Spielraum um die Karten, damit die Fokus-Vergrößerung nicht abgeschnitten wird.
        private static readonly nfloat FocusMargin = 30f;

        private readonly UICollectionView collectionView;
        private readonly HomeCarouselSource source;
        private IReadOnlyList<HomeMediaItem> applied = [];

        /// <summary>Initialisiert eine neue Instanz der <see cref="HomeCarouselView"/>-Klasse.</summary>
        /// <param name="title">Sektionsüberschrift der Reihe.</param>
        /// <param name="itemSize">Kartengröße (inklusive Titelbereich) der Zellen.</param>
        /// <param name="posters">Poster-Cache-/Ladedienst der Karten.</param>
        /// <param name="onItemSelected">Auswahl-Callback eines Eintrags (MVU-Dispatch auf Seitenebene).</param>
        /// <param name="accessibilityId">Optionale stabile Kennung für UI-Automatisierung (<see cref="HomeSeasonalSection.AutomationId"/>).</param>
        internal HomeCarouselView(
            string title,
            CGSize itemSize,
            TvosPosterImageService posters,
            Action<HomeMediaItem> onItemSelected,
            string? accessibilityId = null)
        {
            ClipsToBounds = false;

            var heading = TvosControls.Heading(title);
            AddSubview(heading);

            var flow = new UICollectionViewFlowLayout
            {
                ScrollDirection = UICollectionViewScrollDirection.Horizontal,
                ItemSize = itemSize,
                MinimumLineSpacing = 40,
                SectionInset = new UIEdgeInsets(FocusMargin, 0, FocusMargin, 0)
            };

            collectionView = new UICollectionView(CGRect.Empty, flow)
            {
                BackgroundColor = UIColor.Clear,
                ClipsToBounds = false,
                ShowsHorizontalScrollIndicator = false,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            if (accessibilityId is not null)
            {
                collectionView.AccessibilityIdentifier = accessibilityId;
            }

            collectionView.RegisterClassForCell(typeof(HomeMediaItemCell), HomeMediaItemCell.ReuseId);
            source = new HomeCarouselSource(posters, onItemSelected);
            collectionView.Source = source;
            AddSubview(collectionView);

            NSLayoutConstraint.ActivateConstraints(
            [
                heading.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                heading.TopAnchor.ConstraintEqualTo(TopAnchor),

                collectionView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                collectionView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                collectionView.TopAnchor.ConstraintEqualTo(heading.BottomAnchor, 8),
                collectionView.BottomAnchor.ConstraintEqualTo(BottomAnchor)
            ]);
        }

        /// <summary>Übernimmt den aktuellen Listeneintragsstand; löst <c>ReloadData</c> nur bei tatsächlicher Änderung aus (Vergleich über die Record-Gleichheit der Einträge).</summary>
        /// <param name="items">Die anzuzeigenden Einträge der Reihe.</param>
        internal void SetItems(IReadOnlyList<HomeMediaItem> items)
        {
            if (applied.SequenceEqual(items))
            {
                return;
            }

            applied = items;
            source.SetItems(items);
            collectionView.ReloadData();
        }

        // Datenquelle und Delegat einer Karussell-Reihe: liefert die
        // HomeMediaItemCell-Karten und meldet die Auswahl eines Eintrags
        // an den Seiten-Callback.
        private sealed class HomeCarouselSource : UICollectionViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<HomeMediaItem> onItemSelected;
            private IReadOnlyList<HomeMediaItem> items = [];

            internal HomeCarouselSource(TvosPosterImageService posters, Action<HomeMediaItem> onItemSelected)
            {
                this.posters = posters;
                this.onItemSelected = onItemSelected;
            }

            internal void SetItems(IReadOnlyList<HomeMediaItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (HomeMediaItemCell)collectionView.DequeueReusableCell(
                    HomeMediaItemCell.ReuseId, indexPath);
                cell.Update(items[(int)indexPath.Item], posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onItemSelected(items[(int)indexPath.Item]);
        }
    }
}
