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

        private UILabel? activeServerLabel;
        private UILabel? healthLabel;
        private UILabel? knownHeader;
        private UITableView? knownTable;
        private ServerListTableSource? knownSource;
        private UIButton? discoverButton;
        private UILabel? discoveryStatusLabel;
        private UITableView? discoveredTable;
        private ServerListTableSource? discoveredSource;
        private UIView? addressStage;
        private UITextField? addressField;
        private UIButton? connectButton;
        private UILabel? checkStatusLabel;
        private UILabel? setupErrorLabel;
        private UITextField? ticketField;
        private UIButton? ticketButton;
        private UILabel? bootstrapStatusLabel;
        private UILabel? bootstrapErrorLabel;
        private UIView? authStage;
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

            // Bekannte Server.
            knownHeader = TvosControls.Heading("Bekannte Server");
            content.AddArrangedSubview(knownHeader);
            knownSource = new ServerListTableSource(OnKnownServerSelected);
            knownTable = CreateServerTable(knownSource);
            content.AddArrangedSubview(knownTable);

            // Netzwerk-Erkennung.
            content.AddArrangedSubview(TvosControls.Heading("Im Netzwerk suchen"));
            content.AddArrangedSubview(TvosControls.Body(
                "Die App kann VideoPlayer-Server im eigenen Netzwerk automatisch finden."));
            discoverButton = TvosControls.Button(
                "Server im Netzwerk suchen",
                () => _ = dispatch(new ServerDiscoveryRequestedMessage()));
            content.AddArrangedSubview(discoverButton);
            discoveryStatusLabel = TvosControls.Status();
            content.AddArrangedSubview(discoveryStatusLabel);
            discoveredSource = new ServerListTableSource(OnDiscoveredServerSelected);
            discoveredTable = CreateServerTable(discoveredSource);
            content.AddArrangedSubview(discoveredTable);

            // Stufe 1: Serverwahl (manuelle Adresse + Ticket-Adresse).
            addressField = TvosControls.TextField("http://server/videoplayer/");
            connectButton = TvosControls.Button(
                "Verbinden",
                () => _ = dispatch(new ServerAddressSubmittedMessage(addressField.Text ?? string.Empty)));
            checkStatusLabel = TvosControls.Status();
            setupErrorLabel = TvosControls.Error();
            var manualSection = TvosControls.Section(
                TvosControls.Heading("Adresse selbst eingeben"),
                TvosControls.Body("Adresse des Servers manuell eingeben."),
                addressField, connectButton, checkStatusLabel, setupErrorLabel);

            ticketField = TvosControls.TextField("Ticket-Adresse");
            ticketButton = TvosControls.Button(
                "Ticket einlösen",
                () => _ = dispatch(new BootstrapTicketSubmittedMessage(ticketField.Text ?? string.Empty)));
            bootstrapStatusLabel = TvosControls.Status();
            bootstrapErrorLabel = TvosControls.Error();
            var ticketSection = TvosControls.Section(
                TvosControls.Heading("Mit Kopplungsadresse verbinden"),
                TvosControls.Body("Die Serververwaltung (Profilseite „Geräte“) stellt eine Kopplungsadresse bereit — sie enthält die Serveradresse und koppelt das Gerät direkt."),
                ticketField, ticketButton, bootstrapStatusLabel, bootstrapErrorLabel);

            addressStage = TvosControls.Section(manualSection, ticketSection);
            addressStage.Spacing = 30;
            content.AddArrangedSubview(addressStage);

            // Stufe 2: Anmeldung am geprüften Server (Kurzcode, Pairing
            // oder Zugangsdaten).
            shortCodeField = TvosControls.TextField("Kurzcode");
            shortCodeButton = TvosControls.Button(
                "Kurzcode einlösen",
                () => _ = dispatch(new BootstrapTicketSubmittedMessage(shortCodeField.Text ?? string.Empty)));
            shortCodeStatusLabel = TvosControls.Status();
            shortCodeErrorLabel = TvosControls.Error();
            var shortCodeSection = TvosControls.Section(
                TvosControls.Heading("Mit Kurzcode anmelden"),
                TvosControls.Body("Kurzcode aus der Serververwaltung (Profilseite „Geräte“) eingeben — er wird gegen den verbundenen Server eingelöst, ohne Passwort."),
                shortCodeField, shortCodeButton, shortCodeStatusLabel, shortCodeErrorLabel);

            pairingField = TvosControls.TextField("Pairing-Code");
            pairButton = TvosControls.Button(
                "Koppeln",
                () => _ = dispatch(new PairingCodeSubmittedMessage(pairingField.Text ?? string.Empty)));
            pairingStatusLabel = TvosControls.Status();
            pairingErrorLabel = TvosControls.Error();
            pairingSection = TvosControls.Section(
                TvosControls.Heading("Gerät koppeln"),
                TvosControls.Body("Der Server verlangt zuerst eine Geräte-Kopplung: Einmal-Code aus der Verwaltung deines Servers (Profilseite „Geräte“) eingeben."),
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
                shortCodeSection, pairingSection, credentialsSection);
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
            // aktualisieren (Muster: ServerSetupPage der MAUI-App).
            _ = dispatch(new LoadServersRequestedMessage());
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

        private void RenderStages(AppModel model)
        {
            // Stufenlogik (Muster: ServerSetupPage der MAUI-App): Stufe 1
            // (Adresswahl), solange kein verbundener Server eine Anmeldung
            // oder ein Geräte-Pairing erwartet.
            var authChoice = model.ConnectionState == ConnectionState.NeedsLogin
                || model.IsPairingRequired;
            addressStage!.Hidden = authChoice;
            authStage!.Hidden = !authChoice;
            pairingSection!.Hidden = !model.IsPairingRequired;
            credentialsSection!.Hidden = model.IsPairingRequired;
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
                _ = dispatch(new ServerSwitchRequestedMessage(server.Address, server.DisplayName));
            }
        }

        private void OnDiscoveredServerSelected(nint row)
        {
            var servers = model?.DiscoveredServers;
            if (servers is not null && row >= 0 && row < servers.Count)
            {
                var server = servers[(int)row];
                _ = dispatch(new DiscoveredServerSelectedMessage(server.Address, server.Name));
            }
        }
    }
}
