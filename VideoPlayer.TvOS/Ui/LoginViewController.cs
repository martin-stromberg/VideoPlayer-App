using Foundation;
using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Anmeldemaske des Verbindungs-Workflows (<c>AppPage.Login</c>) —
    /// tvOS-Gegenstück der MAUI-<c>LoginPage</c>: Benutzername/Passwort-
    /// Eingabe, Kurzcode-Alternative, sichtbarer Laufzustand und
    /// Inline-Fehlertext sowie der Ausstieg zur Servereinrichtung. Der
    /// Zustand kommt vollständig aus dem MVU-Model; der Benutzername wird
    /// einmalig aus dem bekannten Wert vorbefüllt.
    /// </summary>
    public sealed class LoginViewController : UIViewController
    {
        private const int ContentWidth = 1100;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly Func<Task> backToMenu;
        private AppModel? model;
        private bool usernamePrefilled;

        private UILabel? serverLabel;
        private UITextField? usernameField;
        private UITextField? passwordField;
        private UIButton? loginButton;
        private UILabel? loginStatusLabel;
        private UILabel? loginErrorLabel;
        private UITextField? shortCodeField;
        private UIButton? shortCodeButton;
        private UILabel? shortCodeStatusLabel;
        private UILabel? shortCodeErrorLabel;
        private UIView? methodSection;
        private UIView? credentialsSection;
        private UIView? shortCodeSection;
        private UIButton? authBackButton;
        private AuthMethod chosenMethod = AuthMethod.None;

        /// <summary>Gewählte Anmeldeform der Maske.</summary>
        private enum AuthMethod
        {
            /// <summary>Methodenwahl wird gezeigt.</summary>
            None,

            /// <summary>Kurzcode aus dem Benutzerprofil.</summary>
            ShortCode,

            /// <summary>Benutzername + Passwort.</summary>
            Credentials
        }

        /// <summary>Initialisiert eine neue Instanz der <see cref="LoginViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="backToMenu">Rückkehr zum Menürahmen (nur bei bestehender Verbindung wirksam).</param>
        public LoginViewController(Func<AppMessage, Task> dispatch, Func<Task> backToMenu)
        {
            Title = "Anmelden";
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            this.backToMenu = backToMenu ?? throw new ArgumentNullException(nameof(backToMenu));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var scrollView = new UIScrollView
            {
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            View.AddSubview(scrollView);
            NSLayoutConstraint.ActivateConstraints(
            [
                scrollView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                scrollView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
                scrollView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                scrollView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor)
            ]);

            var content = TvosControls.Section();
            content.Spacing = 34;
            scrollView.AddSubview(content);
            NSLayoutConstraint.ActivateConstraints(
            [
                content.TopAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TopAnchor, 140),
                content.BottomAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.BottomAnchor, -80),
                content.CenterXAnchor.ConstraintEqualTo(scrollView.FrameLayoutGuide.CenterXAnchor),
                content.WidthAnchor.ConstraintEqualTo(ContentWidth)
            ]);

            content.AddArrangedSubview(TvosControls.Title(Title));
            serverLabel = TvosControls.Body();
            serverLabel.TextColor = UIColor.White;
            content.AddArrangedSubview(serverLabel);

            methodSection = TvosControls.Section(
                TvosControls.Heading("Anmeldemethode wählen"),
                TvosControls.Button("Mit Kurzcode anmelden",
                    () => { chosenMethod = AuthMethod.ShortCode; ApplyStages(); }),
                TvosControls.Button("Mit Zugangsdaten anmelden",
                    () => { chosenMethod = AuthMethod.Credentials; ApplyStages(); }));
            content.AddArrangedSubview(methodSection);

            authBackButton = TvosControls.Button(
                "Andere Methode wählen",
                () => { chosenMethod = AuthMethod.None; ApplyStages(); });
            authBackButton.Hidden = true;

            usernameField = TvosControls.TextField("Benutzername");
            passwordField = TvosControls.TextField("Passwort", secure: true);
            loginButton = TvosControls.Button(
                "Anmelden",
                () => _ = dispatch(new LoginSubmittedMessage(
                    usernameField.Text ?? string.Empty,
                    passwordField.Text ?? string.Empty)));
            loginStatusLabel = TvosControls.Status();
            loginErrorLabel = TvosControls.Error();
            credentialsSection = TvosControls.Section(
                TvosControls.Heading("Mit Zugangsdaten anmelden"),
                TvosControls.Body("Mit den Zugangsdaten des VideoPlayer-Servers anmelden."),
                usernameField, passwordField, loginButton, loginStatusLabel, loginErrorLabel);
            credentialsSection.Hidden = true;
            content.AddArrangedSubview(credentialsSection);

            shortCodeField = TvosControls.TextField("Kurzcode");
            shortCodeButton = TvosControls.Button(
                "Kurzcode einlösen",
                () => _ = dispatch(new BootstrapTicketSubmittedMessage(shortCodeField.Text ?? string.Empty)));
            shortCodeStatusLabel = TvosControls.Status();
            shortCodeErrorLabel = TvosControls.Error();
            shortCodeSection = TvosControls.Section(
                TvosControls.Heading("Mit Kurzcode anmelden"),
                TvosControls.Body("Kurzcode aus dem Benutzerprofil („Gerät koppeln“) eingeben — er wird gegen den verbundenen Server eingelöst, ohne Passwort."),
                shortCodeField, shortCodeButton, shortCodeStatusLabel, shortCodeErrorLabel);
            shortCodeSection.Hidden = true;
            content.AddArrangedSubview(shortCodeSection);
            content.AddArrangedSubview(authBackButton);

            content.AddArrangedSubview(TvosControls.Button(
                "Anderen Server einrichten",
                () => _ = dispatch(new NavigateToMessage(AppPage.ServerSetup))));
        }

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            // Menü-Taste der Siri Remote: bei bestehender Verbindung führt
            // sie zurück zum Menürahmen (Muster: ServerSetupViewController).
            var menuPressed = presses.ToArray<UIPress>()
                .Any(press => press.Type == UIPressType.Menu);
            if (menuPressed && model?.ConnectionState == ConnectionState.Connected)
            {
                _ = backToMenu();
                return;
            }

            base.PressesBegan(presses, evt);
        }

        /// <summary>Spiegelt den aktuellen Modellstand auf die Seite (idempotent).</summary>
        /// <param name="model">Das aktuelle App-Modell.</param>
        public void Render(AppModel model)
        {
            this.model = model;

            // Der bekannte Benutzername wird nur einmalig vorbefüllt, damit
            // spätere Model-Updates die Eingabe nicht überschreiben.
            if (!usernamePrefilled && !string.IsNullOrEmpty(model.LoginUsername))
            {
                usernameField!.Text = model.LoginUsername;
                usernamePrefilled = true;
            }

            serverLabel!.Hidden = model.ActiveServerAddress is null;
            serverLabel.Text = model.ActiveServerAddress is null
                ? string.Empty
                : $"Server: {model.ActiveServerAddress}";

            var loginEnabled = !model.IsLoginRunning;
            loginButton!.Enabled = loginEnabled;
            usernameField!.Enabled = loginEnabled;
            passwordField!.Enabled = loginEnabled;
            loginStatusLabel!.Text = model.IsLoginRunning ? "Anmeldung läuft …" : string.Empty;
            loginErrorLabel!.Text = model.LoginError ?? string.Empty;

            // Kurzcode-Felder: laufender Bootstrap oder Login sperrt beide
            // Wege — Anmeldung und Kopplung konkurrieren um dieselbe
            // Session (Muster: LoginPage der MAUI-App).
            var shortCodeBusy = model.IsLoginRunning || model.IsBootstrapRunning;
            shortCodeButton!.Enabled = !shortCodeBusy;
            shortCodeField!.Enabled = !shortCodeBusy;
            shortCodeStatusLabel!.Text = model.IsBootstrapRunning ? "Kopplung läuft …" : string.Empty;
            shortCodeErrorLabel!.Text = model.BootstrapError ?? string.Empty;
        }

        private void ApplyStages()
        {
            methodSection!.Hidden = chosenMethod != AuthMethod.None;
            credentialsSection!.Hidden = chosenMethod != AuthMethod.Credentials;
            shortCodeSection!.Hidden = chosenMethod != AuthMethod.ShortCode;
            authBackButton!.Hidden = chosenMethod == AuthMethod.None;
        }
    }
}
