using Foundation;
using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Server-Einrichtung beim Erststart und Serververwaltung
    /// („Server wechseln") — tvOS-Gegenstück der MAUI-
    /// <c>ServerSetupPage</c>: Liste bekannter Server, Netzwerk-Erkennung
    /// (mDNS + UDP), manuelle Adresseingabe, Ticket-/Kurzcode-Kopplung,
    /// Geräte-Pairing per Einmal-Code und Anmeldung mit Zugangsdaten.
    /// Der Zustand kommt vollständig aus dem MVU-Model; Benutzeraktionen
    /// werden als Messages in den MVU-Zyklus dispatcht. Alle Eingaben
    /// erfolgen fernbedienungstauglich über <see cref="UITextField"/>
    /// (tvOS-Tastatur inkl. Diktat/Continuity-Tastatur).
    /// </summary>
    public sealed class ServerSetupViewController : UIViewController
    {
        private const int ContentWidth = 1100;

        private readonly Func<AppMessage, Task> dispatch;
        private readonly Func<Task> backToMenu;
        private AppModel? model;
        private bool usernamePrefilled;
        private bool manualEntryMode;
        private bool serverSelectionOverride;
        private AuthMethod chosenMethod = AuthMethod.None;

        private UILabel? activeServerLabel;
        private UILabel? healthLabel;
        private UILabel? knownHeader;
        private UITableView? knownTable;
        private ServerListTableSource? knownSource;
        private UIButton? discoverButton;
        private UILabel? discoveryStatusLabel;
        private UITableView? discoveredTable;
        private ServerListTableSource? discoveredSource;
        private UIStackView? serverSelectSection;
        private UIButton? manualEntryButton;
        private UIButton? manualCancelButton;
        private UIView? manualSection;
        private UIStackView? methodSection;
        private UIButton? authBackButton;
        private UIView? shortCodeSection;
        private UITextField? addressField;
        private UIButton? connectButton;
        private UILabel? checkStatusLabel;
        private UILabel? setupErrorLabel;
        private UITextField? ticketField;
        private UIButton? ticketButton;
        private UILabel? bootstrapStatusLabel;
        private UILabel? bootstrapErrorLabel;
        private UIStackView? authStage;
        private UITextField? shortCodeField;
        private UIButton? shortCodeButton;
        private UILabel? shortCodeStatusLabel;
        private UILabel? shortCodeErrorLabel;
        private UIView? pairingSection;
        private UITextField? pairingField;
        private UIButton? pairButton;
        private UILabel? pairingStatusLabel;
        private UILabel? pairingErrorLabel;
        private UIView? credentialsSection;
        private UITextField? usernameField;
        private UITextField? passwordField;
        private UIButton? loginButton;
        private UILabel? loginStatusLabel;
        private UILabel? loginErrorLabel;
        private UIButton? backButton;

        /// <summary>Gewählte Anmeldemethode in Stufe 2 des Einrichtungs-Wizards.</summary>
        private enum AuthMethod
        {
            /// <summary>Noch keine Methode gewählt — die Auswahl wird gezeigt.</summary>
            None,

            /// <summary>Einmal-Pairing-Code (api/pairing/exchange).</summary>
            Pairing,

            /// <summary>Kurzcode/Ticket aus dem Benutzerprofil (api/pairing/bootstrap).</summary>
            ShortCode,

            /// <summary>Benutzername + Passwort.</summary>
            Credentials
        }

        /// <summary>Initialisiert eine neue Instanz der <see cref="ServerSetupViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        /// <param name="backToMenu">Rückkehr zum Menürahmen (nur bei bestehender Verbindung wirksam).</param>
        public ServerSetupViewController(Func<AppMessage, Task> dispatch, Func<Task> backToMenu)
        {
            Title = "Serververbindung";
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
                content.TopAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.TopAnchor, 110),
                content.BottomAnchor.ConstraintEqualTo(scrollView.ContentLayoutGuide.BottomAnchor, -80),
                content.CenterXAnchor.ConstraintEqualTo(scrollView.FrameLayoutGuide.CenterXAnchor),
                content.WidthAnchor.ConstraintEqualTo(ContentWidth)
            ]);

            // Kopfbereich.
            content.AddArrangedSubview(TvosControls.Title("Serververbindung"));
            content.AddArrangedSubview(TvosControls.Body(
                "Die App verbindet sich mit einem VideoPlayer-Server im eigenen Netzwerk."));
            activeServerLabel = TvosControls.Body();
            activeServerLabel.TextColor = UIColor.White;
            content.AddArrangedSubview(activeServerLabel);
            healthLabel = TvosControls.Status();
            content.AddArrangedSubview(healthLabel);

            // Stufe 1 des Wizards: Serverwahl — Auto-Discovery beim
            // Öffnen (ViewWillAppear), bekannte + gefundene Server als
            // Listen, dazu „Suche wiederholen" und der Unterschritt
            // „Serveradresse manuell eingeben" (blendet die Listen aus,
            // „Abbrechen" bringt sie zurück).
            knownHeader = TvosControls.Heading("Bekannte Server");
            knownSource = new ServerListTableSource(OnKnownServerSelected);
            knownTable = CreateServerTable(knownSource);
            discoverButton = TvosControls.Button(
                "Suche wiederholen",
                () => _ = dispatch(new ServerDiscoveryRequestedMessage()));
            discoveryStatusLabel = TvosControls.Status();
            discoveredSource = new ServerListTableSource(OnDiscoveredServerSelected);
            discoveredTable = CreateServerTable(discoveredSource);
            manualEntryButton = TvosControls.Button(
                "Serveradresse manuell eingeben",
                () => { manualEntryMode = true; ApplyStages(); });
            manualCancelButton = TvosControls.Button(
                "Abbrechen",
                () => { manualEntryMode = false; ApplyStages(); });

            ticketField = TvosControls.TextField("Ticket-Adresse");
            ticketButton = TvosControls.Button(
                "Ticket einlösen",
                () => { serverSelectionOverride = false; _ = dispatch(new BootstrapTicketSubmittedMessage(ticketField.Text ?? string.Empty)); });
            bootstrapStatusLabel = TvosControls.Status();
            bootstrapErrorLabel = TvosControls.Error();
            var ticketSection = TvosControls.Section(
                TvosControls.Heading("Mit Kopplungsadresse verbinden"),
                TvosControls.Body("Im Benutzerprofil unter „Gerät koppeln“ stellt der Server eine Kopplungsadresse bereit — sie enthält die Serveradresse und koppelt das Gerät direkt."),
                ticketField, ticketButton, bootstrapStatusLabel, bootstrapErrorLabel);

            serverSelectSection = TvosControls.Section(
                knownHeader, knownTable,
                TvosControls.Heading("Im Netzwerk suchen"),
                TvosControls.Body("Die App sucht VideoPlayer-Server im eigenen Netzwerk automatisch."),
                discoverButton, discoveryStatusLabel, discoveredTable,
                manualEntryButton, ticketSection);
            serverSelectSection.Spacing = 30;
            content.AddArrangedSubview(serverSelectSection);

            // Unterschritt der Stufe 1: manuelle Adresseingabe.
            addressField = TvosControls.TextField("http://server/videoplayer/");
            connectButton = TvosControls.Button(
                "Verbinden",
                () => { serverSelectionOverride = false; _ = dispatch(new ServerAddressSubmittedMessage(addressField.Text ?? string.Empty)); });
            checkStatusLabel = TvosControls.Status();
            setupErrorLabel = TvosControls.Error();
            manualSection = TvosControls.Section(
                TvosControls.Heading("Serveradresse eingeben"),
                TvosControls.Body("Adresse des Servers manuell eingeben."),
                addressField, connectButton, checkStatusLabel, setupErrorLabel,
                manualCancelButton);
            manualSection.Hidden = true;
            content.AddArrangedSubview(manualSection);

            // Stufe 2: Methodenwahl — erst die Auswahl, dann nur das
            // zugehörige Formular (Pairing-Code, Kurzcode oder
            // Zugangsdaten).
            methodSection = TvosControls.Section(
                TvosControls.Heading("Anmeldemethode wählen"),
                TvosControls.Body("Der Server ist verbunden. Wähle aus, wie du dieses Gerät anmelden möchtest."),
                TvosControls.Button("Mit Pairing-Code koppeln",
                    () => { chosenMethod = AuthMethod.Pairing; ApplyStages(); }),
                TvosControls.Button("Mit Kurzcode anmelden",
                    () => { chosenMethod = AuthMethod.ShortCode; ApplyStages(); }),
                TvosControls.Button("Mit Zugangsdaten anmelden",
                    () => { chosenMethod = AuthMethod.Credentials; ApplyStages(); }),
                TvosControls.Button("Anderen Server wählen",
                    () => { serverSelectionOverride = true; ApplyStages(); }));

            authBackButton = TvosControls.Button(
                "Andere Methode wählen",
                () => { chosenMethod = AuthMethod.None; ApplyStages(); });
            authBackButton.Hidden = true;

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

            pairingField = TvosControls.TextField("Pairing-Code");
            pairButton = TvosControls.Button(
                "Koppeln",
                () => _ = dispatch(new PairingCodeSubmittedMessage(pairingField.Text ?? string.Empty)));
            pairingStatusLabel = TvosControls.Status();
            pairingErrorLabel = TvosControls.Error();
            pairingSection = TvosControls.Section(
                TvosControls.Heading("Gerät koppeln"),
                TvosControls.Body("Der Server verlangt zuerst eine Geräte-Kopplung: Einmal-Code aus der Administration (Seite „Geräte“ → „Pairing-Code generieren“) eingeben."),
                pairingField, pairButton, pairingStatusLabel, pairingErrorLabel);

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

            authStage = TvosControls.Section(
                methodSection, shortCodeSection, pairingSection, credentialsSection,
                authBackButton);
            authStage.Spacing = 30;
            content.AddArrangedSubview(authStage);

            // Wiederholung des Start-Workflows und Rückkehr.
            content.AddArrangedSubview(TvosControls.Button(
                "Erneut prüfen",
                () => _ = dispatch(new StartupRequestedMessage())));
            backButton = TvosControls.Button("Zurück zum Menü", () => _ = backToMenu());
            content.AddArrangedSubview(backButton);
        }

        /// <inheritdoc/>
        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            // Beim Öffnen aus den Einstellungen die Serverliste
            // aktualisieren (Muster: ServerSetupPage der MAUI-App) und
            // die Netzwerk-Suche automatisch anstoßen.
            _ = dispatch(new LoadServersRequestedMessage());
            if (model?.IsDiscoveryRunning != true)
            {
                _ = dispatch(new ServerDiscoveryRequestedMessage());
            }

            // Wird die Seite bei bereits aktivem Server geöffnet (z. B.
            // „Server wechseln" aus den Einstellungen), startet der Wizard
            // in Stufe 1 statt in der erzwungenen Methodenwahl.
            serverSelectionOverride = model?.ActiveServerAddress is not null;
            ApplyStages();
        }

        /// <inheritdoc/>
        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            // Menü-Taste der Siri Remote: bei bestehender Verbindung führt
            // sie zurück zum Menürahmen (die Einrichtung wurde aus den
            // Einstellungen geöffnet). Im Einrichtungs-/Anmeldezwang
            // behält die Taste ihre Systemfunktion.
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
            RenderServerStatus(model);
            RenderServerLists(model);
            RenderStages(model);
            RenderBusyAndErrors(model);
        }

        private void RenderServerStatus(AppModel model)
        {
            var hasActiveServer = model.ActiveServerAddress is not null;
            activeServerLabel!.Hidden = !hasActiveServer;
            healthLabel!.Hidden = !hasActiveServer;
            activeServerLabel.Text = hasActiveServer
                ? $"Aktiver Server: {model.ActiveServerAddress}"
                : string.Empty;
            healthLabel.Text = model.ServerHealthText;
        }

        private void RenderServerLists(AppModel model)
        {
            knownHeader!.Hidden = model.KnownServers.Count == 0;
            knownTable!.Hidden = model.KnownServers.Count == 0;
            knownSource!.SetRows(model.KnownServers
                .Select(server => (server.NameOrAddress,
                    $"{server.Address} · zuletzt verwendet {server.LastUsedAtUtc:dd.MM.yyyy}"))
                .ToList());
            knownTable.ReloadData();

            discoverButton!.Enabled = !model.IsDiscoveryRunning;
            discoveryStatusLabel!.Text = model.IsDiscoveryRunning
                ? "Suche nach Servern im Netzwerk läuft …"
                : model.DiscoveryAttempted && model.DiscoveredServers.Count == 0
                    ? "Kein Server gefunden. Bitte die Adresse manuell eingeben."
                    : string.Empty;
            discoveredTable!.Hidden = model.DiscoveredServers.Count == 0;
            discoveredSource!.SetRows(model.DiscoveredServers
                .Select(server => (server.NameOrAddress, server.Address))
                .ToList());
            discoveredTable.ReloadData();
        }

        private void RenderStages(AppModel model) => ApplyStages();

        /// <summary>
        /// Stufenlogik des Wizards: Stufe 1 zeigt die Serverwahl
        /// (bekannte + gefundene Server) oder — im Unterschritt — die
        /// manuelle Adresseingabe, Stufe 2 die Methodenwahl oder das
        /// jeweils gewählte Anmeldeformular.
        /// </summary>
        private void ApplyStages()
        {
            var model = this.model;
            var authChoice = model is not null
                && !serverSelectionOverride
                && (model.ConnectionState == ConnectionState.NeedsLogin
                    || model.IsPairingRequired);
            if (authChoice)
            {
                manualEntryMode = false;
            }
            else
            {
                chosenMethod = AuthMethod.None;
            }

            serverSelectSection!.Hidden = authChoice || manualEntryMode;
            manualSection!.Hidden = authChoice || !manualEntryMode;

            authStage!.Hidden = !authChoice;
            methodSection!.Hidden = chosenMethod != AuthMethod.None;
            pairingSection!.Hidden = chosenMethod != AuthMethod.Pairing;
            shortCodeSection!.Hidden = chosenMethod != AuthMethod.ShortCode;
            credentialsSection!.Hidden = chosenMethod != AuthMethod.Credentials;
            authBackButton!.Hidden = chosenMethod == AuthMethod.None;
        }

        private void RenderBusyAndErrors(AppModel model)
        {
            connectButton!.Enabled = !model.IsServerCheckRunning;
            addressField!.Enabled = !model.IsServerCheckRunning;
            checkStatusLabel!.Text = model.IsServerCheckRunning
                ? model.ConnectionState == ConnectionState.Connecting
                    ? "Verbindung wird hergestellt …"
                    : "Verbindung wird geprüft …"
                : string.Empty;
            setupErrorLabel!.Text = model.ServerSetupError ?? string.Empty;

            var bootstrapBusy = model.IsBootstrapRunning || model.IsServerCheckRunning;
            ticketButton!.Enabled = !bootstrapBusy;
            ticketField!.Enabled = !bootstrapBusy;
            bootstrapStatusLabel!.Text = model.IsBootstrapRunning ? "Kopplung läuft …" : string.Empty;
            bootstrapErrorLabel!.Text = model.BootstrapError ?? string.Empty;

            var shortCodeBusy = model.IsBootstrapRunning || model.IsLoginRunning;
            shortCodeButton!.Enabled = !shortCodeBusy;
            shortCodeField!.Enabled = !shortCodeBusy;
            shortCodeStatusLabel!.Text = model.IsBootstrapRunning ? "Kopplung läuft …" : string.Empty;
            shortCodeErrorLabel!.Text = model.BootstrapError ?? string.Empty;

            pairButton!.Enabled = !model.IsPairingRunning;
            pairingField!.Enabled = !model.IsPairingRunning;
            pairingStatusLabel!.Text = model.IsPairingRunning ? "Kopplung läuft …" : string.Empty;
            pairingErrorLabel!.Text = model.PairingError ?? string.Empty;

            var loginEnabled = !model.IsLoginRunning && !model.IsBootstrapRunning;
            loginButton!.Enabled = loginEnabled;
            usernameField!.Enabled = loginEnabled;
            passwordField!.Enabled = loginEnabled;
            loginStatusLabel!.Text = model.IsLoginRunning ? "Anmeldung läuft …" : string.Empty;
            loginErrorLabel!.Text = model.LoginError ?? string.Empty;

            // Der bekannte Benutzername wird nur einmalig vorbefüllt, damit
            // spätere Model-Updates die Eingabe nicht überschreiben.
            if (!usernamePrefilled && !string.IsNullOrEmpty(model.LoginUsername))
            {
                usernameField.Text = model.LoginUsername;
                usernamePrefilled = true;
            }

            // Rückweg zum Menürahmen nur bei bestehender Verbindung —
            // im Einrichtungs-/Anmeldezwang sind Menüseiten gesperrt.
            backButton!.Hidden = model.ConnectionState != ConnectionState.Connected;
        }

        private UITableView CreateServerTable(ServerListTableSource source)
        {
            var table = new UITableView
            {
                Source = source,
                BackgroundColor = UIColor.Clear,
                RowHeight = 90,
                TranslatesAutoresizingMaskIntoConstraints = false
            };
            table.HeightAnchor.ConstraintEqualTo(280).Active = true;
            return table;
        }

        private void OnKnownServerSelected(nint row)
        {
            var servers = model?.KnownServers;
            if (servers is not null && row >= 0 && row < servers.Count)
            {
                var server = servers[(int)row];
                serverSelectionOverride = false;
                _ = dispatch(new ServerSwitchRequestedMessage(server.Address, server.DisplayName));
            }
        }

        private void OnDiscoveredServerSelected(nint row)
        {
            var servers = model?.DiscoveredServers;
            if (servers is not null && row >= 0 && row < servers.Count)
            {
                var server = servers[(int)row];
                serverSelectionOverride = false;
                _ = dispatch(new DiscoveredServerSelectedMessage(server.Address, server.Name));
            }
        }
    }
}
