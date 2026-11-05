using Foundation;
using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Vorbereitetes Navigationsziel für Eintrags-Auswahlen, deren
    /// eigentliche Zielseiten (Detailansicht, Wiedergabe) in späteren
    /// Entwicklungsschritten entstehen: zeigt den Titel des Eintrags
    /// samt Hinweis auf den Folgeschritt — ohne Fehler, ohne No-Op.
    /// Die Seite wird modal präsentiert und per Menü-Taste der
    /// Siri Remote oder dem „Zurück"-Button geschlossen; ein bereits
    /// präsentiertes Ziel wird über <see cref="Update"/> mit dem
    /// Folgeziel aktualisiert statt gestapelt.
    /// </summary>
    public sealed class PreparedTargetViewController : UIViewController
    {
        private UILabel? titleLabel;
        private UILabel? detailLabel;
        private string pendingTitle;
        private string pendingDetail;

        /// <summary>Initialisiert eine neue Instanz der <see cref="PreparedTargetViewController"/>-Klasse.</summary>
        /// <param name="title">Titel des angesteuerten Eintrags.</param>
        /// <param name="detail">Hinweistext des vorbereiteten Zielzustands.</param>
        public PreparedTargetViewController(string title, string detail)
        {
            pendingTitle = title;
            pendingDetail = detail;
            Title = title;
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = TvosControls.Title(pendingTitle);
            detailLabel = TvosControls.Body(pendingDetail);

            var content = TvosControls.Section(
                titleLabel,
                detailLabel,
                TvosControls.Button("Zurück", () => DismissViewController(true, null)));
            content.Spacing = 30;
            View.AddSubview(content);

            NSLayoutConstraint.ActivateConstraints(
            [
                content.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                content.TopAnchor.ConstraintEqualTo(View.TopAnchor, 160),
                content.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.7f)
            ]);
        }

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            var menuPressed = presses.ToArray<UIPress>()
                .Any(press => press.Type == UIPressType.Menu);
            if (menuPressed)
            {
                // Menü-Taste der Siri Remote: zurück zum Menürahmen
                // (Muster: ServerSetupViewController).
                DismissViewController(true, null);
                return;
            }

            base.PressesBegan(presses, evt);
        }

        /// <summary>Aktualisiert Titel und Hinweistext des präsentierten Ziels — ersetzt das Stapeln mehrerer Platzhalter bei aufeinanderfolgenden Navigations-Effekten (z. B. Detail → Wiedergabe).</summary>
        /// <param name="title">Neuer Titel.</param>
        /// <param name="detail">Neuer Hinweistext.</param>
        public void Update(string title, string detail)
        {
            pendingTitle = title;
            pendingDetail = detail;
            Title = title;
            if (!IsViewLoaded)
            {
                return;
            }

            titleLabel!.Text = title;
            detailLabel!.Text = detail;
        }
    }
}
