using Foundation;
using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Quellen" — die vom Server freigegebenen
    /// Medienbereiche als fokussierbare <see cref="UITableView"/>-Liste
    /// (tvOS-Pendant der MAUI-Quellenauswahl). Die Auswahl öffnet über
    /// <see cref="SourceSelectedMessage"/> die Browsing-Seite der
    /// Quelle; Lade-, Leer- und Fehlerzustände folgen
    /// <see cref="SourcesOverviewState"/> (Muster:
    /// <see cref="PlaylistsPageViewController"/>).
    /// </summary>
    public sealed class SourcesPageViewController : UIViewController, IModelRenderable
    {
        private readonly Func<AppMessage, Task> dispatch;

        private UILabel? titleLabel;
        private UITableView? tableView;
        private ServerListTableSource? source;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;
        private IReadOnlyList<SourceItem> appliedItems = [];
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="SourcesPageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public SourcesPageViewController(Func<AppMessage, Task> dispatch)
        {
            Title = "Quellen";
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = TvosControls.Title(Title);
            View.AddSubview(titleLabel);

            tableView = new UITableView
            {
                BackgroundColor = UIColor.Clear,
                RowHeight = 90,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "SourcesTable"
            };
            source = new ServerListTableSource(OnSourceRowSelected);
            tableView.Source = source;
            View.AddSubview(tableView);

            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new SourcesReloadRequestedMessage()));
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
            // Beim Öffnen aktualisieren — die Liste kommt zustands-
            // gesteuert aus dem Modell, der Reload stößt die Server-
            // Abfrage an (Muster: PlaylistsPageViewController).
            _ = dispatch(new SourcesReloadRequestedMessage());
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
            var overview = model.SourcesOverview;

            if (!appliedItems.SequenceEqual(overview.Sources))
            {
                appliedItems = overview.Sources;
                source!.SetRows(overview.Sources
                    .Select(item => (item.Name, string.Empty))
                    .ToList());
                tableView!.ReloadData();
            }

            if (overview.HasError && !overview.IsLoading)
            {
                stateLabel!.Text = overview.Error ?? "Die Quellen konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (overview.IsLoading && !overview.HasSources)
            {
                stateLabel!.Text = "Quellen werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            if (overview.ShowEmpty)
            {
                stateLabel!.Text = "Keine Quellen freigegeben.";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            stateView!.Hidden = true;
        }

        private void OnSourceRowSelected(nint row)
        {
            var sources = lastModel?.SourcesOverview.Sources;
            if (sources is not null && row >= 0 && row < sources.Count)
            {
                _ = dispatch(new SourceSelectedMessage(sources[(int)row]));
            }
        }
    }
}
