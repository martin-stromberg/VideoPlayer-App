using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Playlists" — tvOS-Pendant der MAUI-<c>PlaylistsPage</c>:
    /// eine gruppierte <see cref="UITableView"/> mit den Sektionen
    /// „Meine Playlists" und „Öffentliche Playlists", jede Zeile mit
    /// Cover, Name, Sortiermodus und Anzeige-Genres. Playlists sind ein
    /// reines Online-Feature — ohne Serververbindung zeigt die Seite
    /// eine verständliche Fehleranzeige mit Wiederholmöglichkeit. Die
    /// Auswahl öffnet über den MVU-Kreislauf die Playlist-Detailseite;
    /// jegliche Verwaltung ist auf tvOS nicht verdrahtet.
    /// </summary>
    public sealed class PlaylistsPageViewController : UIViewController, IModelRenderable
    {
        private static readonly nfloat RowHeight = 170f;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;

        private UILabel? titleLabel;
        private UITableView? tableView;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;

        private PlaylistsSource? source;
        private IReadOnlyList<PlaylistOverviewItem> appliedItems = [];
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistsPageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Bild-Cache-/Ladedienst der Playlist-Cover.</param>
        public PlaylistsPageViewController(
            Func<AppMessage, Task> dispatch,
            TvosPosterImageService posters)
        {
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.posters = posters ?? throw new ArgumentNullException(nameof(posters));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = TvosControls.Title("Playlists");
            View.AddSubview(titleLabel);

            tableView = new UITableView(CGRect.Empty, UITableViewStyle.Plain)
            {
                BackgroundColor = UIColor.Clear,
                SeparatorStyle = UITableViewCellSeparatorStyle.None,
                ShowsVerticalScrollIndicator = true,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "PlaylistsTable"
            };
            source = new PlaylistsSource(
                posters,
                playlist => _ = dispatch(new PlaylistSelectedMessage(playlist)));
            tableView.Source = source;
            View.AddSubview(tableView);

            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new PlaylistsPageOpenedMessage()));
            stateView = TvosControls.Section(stateLabel, retryButton);
            stateView.Alignment = UIStackViewAlignment.Center;
            stateView.Hidden = true;
            View.AddSubview(stateView);

            NSLayoutConstraint.ActivateConstraints(
            [
                titleLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                titleLabel.TopAnchor.ConstraintEqualTo(View.TopAnchor, 110),

                tableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 60),
                tableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -60),
                tableView.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 30),
                tableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor, -40),

                stateView.CenterXAnchor.ConstraintEqualTo(tableView.CenterXAnchor),
                stateView.CenterYAnchor.ConstraintEqualTo(tableView.CenterYAnchor),
                stateView.WidthAnchor.ConstraintLessThanOrEqualTo(tableView.WidthAnchor, 0.6f)
            ]);

            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        /// <inheritdoc/>
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // Beim Öffnen (neu) laden: online vom Server; ohne Verbindung
            // entscheidet das Update über die Fehleranzeige (Muster:
            // PlaylistsPage.OnAppearing).
            _ = dispatch(new PlaylistsPageOpenedMessage());
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
            var page = model.PlaylistsPage;
            ApplyItems(page);
            ApplyState(page);
        }

        private void ApplyItems(PlaylistsPageState page)
        {
            if (appliedItems.SequenceEqual(page.Items))
            {
                return;
            }

            appliedItems = page.Items;
            source!.SetItems(page.Items);
            tableView!.ReloadData();
        }

        // Lade-, Leer- und Fehlerzustände — Fehler zeigen die
        // Wiederholmöglichkeit (Muster: SourceBrowserViewController).
        private void ApplyState(PlaylistsPageState page)
        {
            if (page.HasError && !page.IsLoading)
            {
                stateLabel!.Text = page.Error ?? "Die Playlists konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (page.IsLoading && !page.HasItems)
            {
                stateLabel!.Text = "Playlists werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            if (page.ShowEmpty)
            {
                stateLabel!.Text = "Keine Playlists vorhanden.";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            stateView!.Hidden = true;
        }

        /// <summary>
        /// Tabellen-Datenquelle der Übersicht: die flache
        /// <see cref="PlaylistOverviewItem"/>-Zeilenliste des Modells
        /// wird auf Sektionen abgebildet — jede Überschriftenzeile
        /// eröffnet eine Sektion mit <see cref="TitleForHeader"/>, die
        /// folgenden Einträge werden zu Zeilen.
        /// </summary>
        private sealed class PlaylistsSource : UITableViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<PlaylistListItem> onPlaylistSelected;
            private List<(string Title, List<PlaylistListItem> Entries)> sections = [];

            internal PlaylistsSource(
                TvosPosterImageService posters,
                Action<PlaylistListItem> onPlaylistSelected)
            {
                this.posters = posters;
                this.onPlaylistSelected = onPlaylistSelected;
            }

            internal void SetItems(IReadOnlyList<PlaylistOverviewItem> items) =>
                sections = BuildSections(items);

            /// <inheritdoc/>
            public override nint NumberOfSections(UITableView tableView) => sections.Count;

            /// <inheritdoc/>
            public override nint RowsInSection(UITableView tableView, nint section) =>
                sections[(int)section].Entries.Count;

            /// <inheritdoc/>
            public override string TitleForHeader(UITableView tableView, nint section) =>
                sections[(int)section].Title;

            /// <inheritdoc/>
            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) =>
                RowHeight;

            /// <inheritdoc/>
            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = tableView.DequeueReusableCell(PlaylistTableCell.ReuseId) as PlaylistTableCell
                    ?? new PlaylistTableCell();
                cell.Update(
                    sections[(int)indexPath.Section].Entries[(int)indexPath.Row],
                    posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow(indexPath, true);
                onPlaylistSelected(
                    sections[(int)indexPath.Section].Entries[(int)indexPath.Row]);
            }

            // Gruppiert die flache Zeilenliste: Header-Zeilen eröffnen
            // eine Sektion, Eintragszeilen fallen in die aktuelle.
            private static List<(string Title, List<PlaylistListItem> Entries)> BuildSections(
                IReadOnlyList<PlaylistOverviewItem> items)
            {
                var result = new List<(string Title, List<PlaylistListItem> Entries)>();
                foreach (var item in items)
                {
                    if (item.IsHeader)
                    {
                        result.Add((item.Title, []));
                    }
                    else if (item.Playlist is { } playlist && result.Count > 0)
                    {
                        result[^1].Entries.Add(playlist);
                    }
                }

                return result;
            }
        }
    }
}
