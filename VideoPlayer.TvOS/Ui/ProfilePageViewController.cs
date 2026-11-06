using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Profil": Verbindungsanzeige (aktiver Server und
    /// Verbindungszustand) sowie „Abmelden" — die Abmeldung läuft über
    /// <see cref="LogoutRequestedMessage"/> in den MVU-Kreislauf (Update
    /// setzt den Sitzungszustand zurück und navigiert zur Anmeldemaske).
    /// Weitere Profilinformationen folgen in einem späteren
    /// Entwicklungsschritt.
    /// </summary>
    public sealed class ProfilePageViewController : UIViewController, IModelRenderable
    {
        private readonly Func<AppMessage, Task> dispatch;
        private UILabel? serverLabel;
        private UILabel? statusLabel;
        private UIButton? logoutButton;
        private AppModel? lastModel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="ProfilePageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public ProfilePageViewController(Func<AppMessage, Task> dispatch)
        {
            Title = "Profil";
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var content = TvosControls.Section(
                TvosControls.Title(Title),
                TvosControls.Body("Konto- und Profilinformationen."));
            content.Spacing = 30;
            View.AddSubview(content);

            serverLabel = TvosControls.Body();
            serverLabel.TextColor = UIColor.White;
            statusLabel = TvosControls.Status();
            logoutButton = TvosControls.Button(
                "Abmelden",
                () => _ = dispatch(new LogoutRequestedMessage()));
            content.AddArrangedSubview(TvosControls.Section(
                TvosControls.Heading("Sitzung"),
                serverLabel,
                statusLabel,
                logoutButton));

            NSLayoutConstraint.ActivateConstraints(
            [
                content.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                content.TopAnchor.ConstraintEqualTo(View.TopAnchor, 160),
                content.WidthAnchor.ConstraintEqualTo(1100)
            ]);

            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        /// <inheritdoc/>
        public void Render(AppModel model)
        {
            lastModel = model;
            if (!IsViewLoaded)
            {
                // Unsichtbare Tabs laden ihre Ansicht nicht
                // vorsorglich — der Stand wird beim ersten ViewDidLoad
                // nachgezogen (Muster: HomePageViewController).
                return;
            }

            ApplyRender(model);
        }

        private void ApplyRender(AppModel model)
        {
            serverLabel!.Text = model.ActiveServerAddress is null
                ? "Es ist kein Server eingerichtet."
                : $"Server: {model.ActiveServerAddress}";
            statusLabel!.Text = model.IsOffline
                ? "Offline — lokale Daten; keine Serververbindung."
                : model.ServerHealthText;
            // Die Abmeldung ist nur aus einem angemeldeten Zustand
            // wirksam (Guard: UpdateLogoutRequested).
            logoutButton!.Enabled =
                model.ConnectionState is ConnectionState.Connected or ConnectionState.Offline;
        }
    }
}
