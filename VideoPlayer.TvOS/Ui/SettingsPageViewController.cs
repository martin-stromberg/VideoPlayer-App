using UIKit;
using VideoPlayer.Mvu;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Einstellungen": zeigt den Serverstatus und bietet den
    /// Einstieg „Server wechseln" — die Serververwaltung
    /// (<see cref="ServerSetupViewController"/>) wird als Workflow-Seite
    /// geöffnet. Weitere Einstellungsbereiche folgen in einem späteren
    /// Entwicklungsschritt.
    /// </summary>
    public sealed class SettingsPageViewController : UIViewController, IModelRenderable
    {
        private readonly Func<AppMessage, Task> dispatch;
        private UILabel? serverLabel;
        private UILabel? statusLabel;

        /// <summary>Initialisiert eine neue Instanz der <see cref="SettingsPageViewController"/>-Klasse.</summary>
        /// <param name="dispatch">Rückmeldung in den MVU-Zyklus (Hauptthread).</param>
        public SettingsPageViewController(Func<AppMessage, Task> dispatch)
        {
            Title = "Einstellungen";
            this.dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        /// <inheritdoc/>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View!.BackgroundColor = UIColor.Black;

            var content = TvosControls.Section(
                TvosControls.Title(Title),
                TvosControls.Body("App- und Wiedergabe-Einstellungen."));
            content.Spacing = 30;
            View.AddSubview(content);

            serverLabel = TvosControls.Body();
            serverLabel.TextColor = UIColor.White;
            statusLabel = TvosControls.Status();
            content.AddArrangedSubview(TvosControls.Section(
                TvosControls.Heading("Server"),
                serverLabel,
                statusLabel,
                TvosControls.Button(
                    "Server wechseln",
                    () => _ = dispatch(new NavigateToMessage(AppPage.ServerSetup)))));

            NSLayoutConstraint.ActivateConstraints(
            [
                content.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor, 90),
                content.TopAnchor.ConstraintEqualTo(View.TopAnchor, 160),
                content.WidthAnchor.ConstraintEqualTo(1100)
            ]);
        }

        /// <inheritdoc/>
        public void Render(AppModel model)
        {
            serverLabel!.Text = model.ActiveServerAddress is null
                ? "Es ist kein Server eingerichtet."
                : $"Server: {model.ActiveServerAddress}";
            statusLabel!.Text = model.ActiveServerAddress is null
                ? string.Empty
                : model.ServerHealthText;
        }
    }
}
