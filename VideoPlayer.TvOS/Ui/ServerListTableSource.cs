using Foundation;
using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Tabellen-Datenquelle der Serverlisten (bekannte und per Erkennung
    /// gefundene Server): Untertitel-Zellen mit Anzeigename und Detailzeile
    /// sowie einem Auswahl-Callback. Fokussierbarkeit und Selektion laufen
    /// über die tvOS-Standardmechanismen der <see cref="UITableView"/>.
    /// </summary>
    internal sealed class ServerListTableSource : UITableViewSource
    {
        private const string CellId = "serverCell";

        private readonly Action<nint> onSelected;
        private IReadOnlyList<(string Title, string Detail)> rows = [];

        /// <summary>Initialisiert eine neue Instanz der <see cref="ServerListTableSource"/>-Klasse.</summary>
        /// <param name="onSelected">Callback mit dem Zeilenindex der Auswahl.</param>
        internal ServerListTableSource(Action<nint> onSelected)
        {
            this.onSelected = onSelected ?? throw new ArgumentNullException(nameof(onSelected));
        }

        /// <summary>Ersetzt die dargestellten Zeilen.</summary>
        /// <param name="rows">Zeileninhalte (Titel + Detailzeile).</param>
        internal void SetRows(IReadOnlyList<(string Title, string Detail)> rows)
        {
            this.rows = rows ?? [];
        }

        /// <inheritdoc/>
        public override nint RowsInSection(UITableView tableView, nint section) => rows.Count;

        /// <inheritdoc/>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(CellId)
                ?? new UITableViewCell(UITableViewCellStyle.Subtitle, CellId);
            var row = rows[(int)indexPath.Row];
            cell.TextLabel!.Text = row.Title;
            cell.TextLabel.TextColor = UIColor.White;
            cell.TextLabel.Font = UIFont.SystemFontOfSize(32);
            cell.DetailTextLabel!.Text = row.Detail;
            cell.DetailTextLabel.TextColor = UIColor.LightGray;
            cell.DetailTextLabel.Font = UIFont.SystemFontOfSize(26);
            cell.BackgroundColor = UIColor.Clear;
            return cell;
        }

        /// <inheritdoc/>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);
            onSelected(indexPath.Row);
        }
    }
}
