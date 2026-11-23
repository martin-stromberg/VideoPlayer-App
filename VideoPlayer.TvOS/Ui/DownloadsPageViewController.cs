using CoreGraphics;
using Foundation;
using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.TvOS.Services;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Downloads" — schlankes tvOS-Pendant der
    /// MAUI-<c>DownloadsPage</c>: eine <see cref="UITableView"/> mit den
    /// Einträgen des Download-Manifests (Zustand, Fortschritt,
    /// Ablaufdatum, Fehlerhinweis), Kontingent-Belegung im Kopf und die
    /// Sammelaktion „Fehlerhafte löschen". Die Auswahl einer Zeile
    /// öffnet das Aktionsmenü des Eintrags (Abbrechen, Erneut
    /// versuchen, Löschen). Die Seite arbeitet ohne Serververbindung —
    /// die Daten liegen lokal vor; beim Erscheinen läuft der
    /// Ablauf-/Abgleichslauf über <see cref="DownloadsPageOpenedMessage"/>.
    /// </summary>
    public sealed class DownloadsPageViewController : UIViewController, IModelRenderable
    {
        private static readonly nfloat RowHeight = 150f;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly TvosPosterImageService posters;

        private UILabel? titleLabel;
        private UILabel? quotaLabel;
        private UIButton? deleteFailedButton;
        private UITableView? tableView;
        private UIStackView? stateView;
        private UILabel? stateLabel;
        private UIButton? retryButton;

        private DownloadsSource? source;
        private IReadOnlyList<DownloadListItem> appliedItems = [];
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="DownloadsPageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="posters">Bild-Cache-/Ladedienst der Vorschaubilder.</param>
        public DownloadsPageViewController(
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

            titleLabel = TvosControls.Title("Downloads");
            View.AddSubview(titleLabel);

            quotaLabel = TvosControls.Status();
            View.AddSubview(quotaLabel);

            deleteFailedButton = TvosControls.Button(
                "Fehlerhafte löschen",
                () => _ = dispatch(new DownloadsDeleteFailedRequestedMessage()));
            deleteFailedButton.Hidden = true;
            View.AddSubview(deleteFailedButton);

            tableView = new UITableView(CGRect.Empty, UITableViewStyle.Plain)
            {
                BackgroundColor = UIColor.Clear,
                ShowsVerticalScrollIndicator = true,
                RemembersLastFocusedIndexPath = true,
                TranslatesAutoresizingMaskIntoConstraints = false,
                AccessibilityIdentifier = "DownloadsTable"
            };
            source = new DownloadsSource(posters, ShowItemActions);
            tableView.Source = source;
            View.AddSubview(tableView);

            stateLabel = TvosControls.Status();
            retryButton = TvosControls.Button(
                "Erneut versuchen",
                () => _ = dispatch(new DownloadsPageOpenedMessage()));
            stateView = TvosControls.Section(stateLabel, retryButton);
            stateView.Alignment = UIStackViewAlignment.Center;
            stateView.Hidden = true;
            View.AddSubview(stateView);

            NSLayoutConstraint.ActivateConstraints(
            [
                titleLabel.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                titleLabel.TopAnchor.ConstraintEqualTo(View.TopAnchor, 110),

                quotaLabel.LeadingAnchor.ConstraintEqualTo(titleLabel.LeadingAnchor),
                quotaLabel.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 10),

                deleteFailedButton.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -90),
                deleteFailedButton.CenterYAnchor.ConstraintEqualTo(quotaLabel.CenterYAnchor),

                tableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 60),
                tableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor, -60),
                tableView.TopAnchor.ConstraintEqualTo(quotaLabel.BottomAnchor, 24),
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
            // Beim Öffnen (neu) laden — inklusive Ablauf-/Abgleichslauf
            // des Manifests (Muster: DownloadsPage.OnAppearing).
            _ = dispatch(new DownloadsPageOpenedMessage());
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
            var page = model.Downloads;
            ApplyItems(page);
            ApplyHeader(page);
            ApplyState(page);
        }

        private void ApplyItems(DownloadsPageState page)
        {
            if (appliedItems.SequenceEqual(page.Items))
            {
                return;
            }

            appliedItems = page.Items;
            source!.SetItems(page.Items);
            tableView!.ReloadData();
        }

        // Kopfzeile: Kontingent-Belegung (+ erkannte Restdaten) und die
        // Sammelaktion für fehlgeschlagene/abgebrochene Einträge.
        private void ApplyHeader(DownloadsPageState page)
        {
            var text = page.QuotaText;
            if (page.OrphanedText is { } orphaned)
            {
                text += $" — {orphaned}";
            }

            quotaLabel!.Text = text;
            deleteFailedButton!.Hidden = !page.HasFailedItems;
        }

        // Lade-, Leer- und Fehlerzustände — Fehler zeigen die
        // Wiederholmöglichkeit (Muster: PlaylistsPageViewController).
        private void ApplyState(DownloadsPageState page)
        {
            if (page.HasError && !page.IsLoading)
            {
                stateLabel!.Text = page.Error ?? "Die Downloads konnten nicht geladen werden.";
                retryButton!.Hidden = false;
                stateView!.Hidden = false;
                return;
            }

            if (page.IsLoading && !page.HasItems)
            {
                stateLabel!.Text = "Downloads werden geladen…";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            if (page.ShowEmpty)
            {
                stateLabel!.Text = "Keine Downloads vorhanden.";
                retryButton!.Hidden = true;
                stateView!.Hidden = false;
                return;
            }

            stateView!.Hidden = true;
        }

        // Aktionsmenü eines Eintrags — nur die zum Zustand passenden
        // Aktionen (Abbrechen aktiver, Wiederholen fehlgeschlagener,
        // Löschen immer).
        private void ShowItemActions(DownloadListItem item)
        {
            var alert = UIAlertController.Create(
                item.Title,
                $"{item.StateText}{FormatDetailSuffix(item)}",
                UIAlertControllerStyle.ActionSheet);
            if (item.CanCancel)
            {
                alert.AddAction(UIAlertAction.Create(
                    "Abbrechen", UIAlertActionStyle.Default,
                    action => _ = dispatch(new DownloadCancelRequestedMessage(item))));
            }

            if (item.CanRetry)
            {
                alert.AddAction(UIAlertAction.Create(
                    "Erneut versuchen", UIAlertActionStyle.Default,
                    action => _ = dispatch(new DownloadRetryRequestedMessage(item))));
            }

            if (item.CanDelete)
            {
                alert.AddAction(UIAlertAction.Create(
                    "Löschen", UIAlertActionStyle.Destructive,
                    action => _ = dispatch(new DownloadDeleteRequestedMessage(item))));
            }

            alert.AddAction(UIAlertAction.Create(
                "Zurück", UIAlertActionStyle.Cancel, null));
            PresentViewController(alert, true, null);
        }

        private static string FormatDetailSuffix(DownloadListItem item)
        {
            var detail = item.ErrorText ?? item.ExpiresText;
            return detail is null ? string.Empty : $" — {detail}";
        }

        /// <summary>
        /// Tabellen-Datenquelle der Download-Liste: flache
        /// <see cref="DownloadListItem"/>-Zeilen in Auftragsreihenfolge;
        /// die Zeilenauswahl öffnet das Aktionsmenü des Eintrags.
        /// </summary>
        private sealed class DownloadsSource : UITableViewSource
        {
            private readonly TvosPosterImageService posters;
            private readonly Action<DownloadListItem> onItemSelected;
            private IReadOnlyList<DownloadListItem> items = [];

            internal DownloadsSource(
                TvosPosterImageService posters,
                Action<DownloadListItem> onItemSelected)
            {
                this.posters = posters;
                this.onItemSelected = onItemSelected;
            }

            internal void SetItems(IReadOnlyList<DownloadListItem> items) =>
                this.items = items;

            /// <inheritdoc/>
            public override nint RowsInSection(UITableView tableView, nint section) =>
                items.Count;

            /// <inheritdoc/>
            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) =>
                RowHeight;

            /// <inheritdoc/>
            public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            {
                var cell = tableView.DequeueReusableCell(DownloadTableCell.ReuseId) as DownloadTableCell
                    ?? new DownloadTableCell();
                cell.Update(items[(int)indexPath.Row], posters);
                return cell;
            }

            /// <inheritdoc/>
            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow(indexPath, true);
                onItemSelected(items[(int)indexPath.Row]);
            }
        }
    }
}
