using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Horizontale „Quellen"-Reihe der Startseite: Überschrift über einer
    /// horizontal scrollenden <see cref="UICollectionView"/> mit
    /// <see cref="SourceItemCell"/>-Karten der freigegebenen
    /// Medienquellen (<see cref="SourcesOverviewState.Sources"/>). Per
    /// Siri Remote fokussierbar und scrollbar (Focus-Engine,
    /// <c>RemembersLastFocusedIndexPath</c>) — Muster:
    /// <see cref="HomeCarouselView"/>.
    /// </summary>
    internal sealed class SourceCarouselView : UIView
    {
        // Zusätzlicher vertikaler Spielraum um die Karten, damit die
        // Fokus-Vergrößerung nicht abgeschnitten wird.
        private static readonly nfloat FocusMargin = 30f;

        private readonly UICollectionView collectionView;
        private readonly SourceCarouselSource source;
        private IReadOnlyList<SourceItem> applied = [];

        /// <summary>Initialisiert eine neue Instanz der <see cref="SourceCarouselView"/>-Klasse.</summary>
        /// <param name="itemSize">Kartengröße (inklusive Namensbereich) der Zellen.</param>
        /// <param name="posters">Bild-Cache-/Ladedienst der Karten.</param>
        /// <param name="onItemSelected">Auswahl-Callback eines Quelleneintrags (MVU-Dispatch auf Seitenebene).</param>
        internal SourceCarouselView(
            CGSize itemSize,
            TvosPosterImageService posters,
            Action<SourceItem> onItemSelected)
        {
            ClipsToBounds = false;

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
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "SourcesCarousel"
            };
            collectionView.RegisterClassForCell(typeof(SourceItemCell), SourceItemCell.ReuseId);
            source = new SourceCarouselSource(posters, onItemSelected);
            collectionView.Source = source;
            AddSubview(collectionView);

            NSLayoutConstraint.ActivateConstraints(
            [
                collectionView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                collectionView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                collectionView.TopAnchor.ConstraintEqualTo(TopAnchor),
                collectionView.BottomAnchor.ConstraintEqualTo(BottomAnchor),
                // Feste Höhe: UICollectionView hat keine intrinsische
                // Größe — in der StackView kollabiert die Reihe sonst auf
                // 0pt und die Zellen bleiben unsichtbar.
                collectionView.HeightAnchor.ConstraintEqualTo(itemSize.Height + 2 * FocusMargin)
            ]);
        }

        /// <summary>Übernimmt den aktuellen Quellenstand; löst <c>ReloadData</c> nur bei tatsächlicher Änderung aus (Vergleich über die Record-Gleichheit der Einträge).</summary>
        /// <param name="items">Die anzuzeigenden Quellen.</param>
        internal void SetItems(IReadOnlyList<SourceItem> items)
        {
            if (applied.SequenceEqual(items))
            {
                return;
            }

            applied = items;
            source.SetItems(items);
            collectionView.ReloadData();
        }

        // Datenquelle und Delegat der Quellen-Reihe: liefert die
        // SourceItemCell-Karten und meldet die Auswahl einer Quelle an
        // den Seiten-Callback.
        private sealed class SourceCarouselSource : UICollectionViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<SourceItem> onItemSelected;
            private IReadOnlyList<SourceItem> items = [];

            internal SourceCarouselSource(TvosPosterImageService posters, Action<SourceItem> onItemSelected)
            {
                this.posters = posters;
                this.onItemSelected = onItemSelected;
            }

            internal void SetItems(IReadOnlyList<SourceItem> items) => this.items = items;

            /// <inheritdoc/>
            public override nint GetItemsCount(UICollectionView collectionView, nint section) => items.Count;

            /// <inheritdoc/>
            public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
            {
                var cell = (SourceItemCell)collectionView.DequeueReusableCell(
                    SourceItemCell.ReuseId, indexPath);
                cell.Update(items[(int)indexPath.Item], posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath) =>
                onItemSelected(items[(int)indexPath.Item]);
        }
    }
}
