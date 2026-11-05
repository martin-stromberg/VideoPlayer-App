using UIKit;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Zustandsseite der Verbindungsphase ohne Menüeintrag
    /// (<c>AppPage.ServerSetup</c> / <c>AppPage.Login</c>): Titel,
    /// modellgetriebener Status-/Fehlertext und ein fokussierbarer
    /// „Erneut prüfen"-Button, der den Start-Workflow erneut anstößt
    /// (<c>StartupRequestedMessage</c>). Die vollständigen Eingabe-Dialoge
    /// (Serverwahl, Pairing, Anmeldung) folgen in einem späteren
    /// Entwicklungsschritt — die Seite bildet bereits den Verbindungszustand
    /// ab und ist per Fernbedienung bedienbar.
    /// </summary>
    public sealed class ConnectionStatePageViewController : UIViewController
    {
        private readonly Action retry;
        private UILabel? titleLabel;
        private UILabel? statusLabel;
        private UILabel? errorLabel;
        private UIButton? retryButton;

        /// <summary>Initialisiert eine neue Instanz der <see cref="ConnectionStatePageViewController"/>-Klasse.</summary>
        /// <param name="title">Seitentitel.</param>
        /// <param name="retry">Aktion des „Erneut prüfen"-Buttons.</param>
        public ConnectionStatePageViewController(string title, Action retry)
        {
            Title = title;
            this.retry = retry ?? throw new ArgumentNullException(nameof(retry));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            titleLabel = new UILabel
            {
                Text = Title,
                TextColor = UIColor.White,
                Font = UIFont.BoldSystemFontOfSize(60),
                TextAlignment = UITextAlignment.Center,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            statusLabel = new UILabel
            {
                TextColor = UIColor.LightGray,
                Font = UIFont.SystemFontOfSize(32),
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            errorLabel = new UILabel
            {
                TextColor = UIColor.SystemRed,
                Font = UIFont.SystemFontOfSize(28),
                TextAlignment = UITextAlignment.Center,
                Lines = 0,
                TranslatesAutoresizingMaskIntoConstraints = false
            };

            retryButton = UIButton.FromType(UIButtonType.System);
            retryButton.SetTitle("Erneut prüfen", UIControlState.Normal);
            retryButton.TranslatesAutoresizingMaskIntoConstraints = false;
            retryButton.PrimaryActionTriggered += (_, _) => retry();

            View.AddSubview(titleLabel);
            View.AddSubview(statusLabel);
            View.AddSubview(errorLabel);
            View.AddSubview(retryButton);

            NSLayoutConstraint.ActivateConstraints(
            [
                titleLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                titleLabel.TopAnchor.ConstraintEqualTo(View.TopAnchor, 260),
                titleLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.8f),
                statusLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                statusLabel.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, 50),
                statusLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.7f),
                errorLabel.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                errorLabel.TopAnchor.ConstraintEqualTo(statusLabel.BottomAnchor, 40),
                errorLabel.WidthAnchor.ConstraintLessThanOrEqualTo(View.WidthAnchor, 0.7f),
                retryButton.CenterXAnchor.ConstraintEqualTo(View.CenterXAnchor),
                retryButton.TopAnchor.ConstraintEqualTo(errorLabel.BottomAnchor, 70)
            ]);
        }

        /// <summary>Aktualisiert Status- und Fehlertext der Seite.</summary>
        /// <param name="statusText">Statustext (Verbindungszustand, Hinweise).</param>
        /// <param name="errorText">Inline-Fehlertext; <c>null</c> = ausgeblendet.</param>
        public void UpdateStatus(string? statusText, string? errorText)
        {
            if (statusLabel is not null)
            {
                statusLabel.Text = statusText ?? string.Empty;
            }

            if (errorLabel is not null)
            {
                errorLabel.Text = errorText ?? string.Empty;
            }
        }
    }
}
