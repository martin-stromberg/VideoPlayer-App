using UIKit;
using VideoPlayer.Mvu;
using VideoPlayer.Storage;

namespace VideoPlayer.TvOS.Ui
{
    /// <summary>
    /// Menüseite „Einstellungen" — schlanker, fernbedienungstauglicher
    /// Einstellungsbereich auf den plattformneutralen Einstellungsdiensten
    /// (<see cref="AppSettings"/>/<see cref="SaveSettingsRequestedMessage"/>;
    /// die Wertebegrenzung greift über <see cref="AppSettings.Normalize"/>):
    /// Aufbewahrungszeit und Speicherplatzkontingent der Downloads sowie
    /// Auto-Fortsetzung und „angesehen"-Endfenster der Wiedergabe.
    /// Numerische Werte werden per Auswahlliste eingestellt, der Schalter
    /// als Ein/Aus-Eintrag — alle Zeilen sind einfache fokussierbare
    /// Buttons. Ergänzend zeigt der Bereich Konto-Informationen
    /// (angemeldeter Benutzer, „Abmelden" über den bestehenden
    /// Abmeldefluss) und den Serverabschnitt mit „Server wechseln" —
    /// die Serververwaltung (<see cref="ServerSetupViewController"/>) wird
    /// als Workflow-Seite geöffnet. Bewusst nicht exponiert sind
    /// Auto-Download/Vorlauf, „nach dem Abspielen löschen",
    /// Nur-WLAN-Downloads und der Protokollversand — auf tvOS nicht
    /// verdrahtet bzw. außerhalb des Umfangs.
    /// </summary>
    public sealed class SettingsPageViewController : UIViewController, IModelRenderable
    {
        // Vorgabewerte der numerischen Einstellungen — innerhalb der
        // Schranken von AppSettings.Normalize; ein abweichender Ist-Wert
        // wird der Auswahlliste zusätzlich angehängt (kein Werteverlust).
        private static readonly int[] RetentionOptions = [1, 3, 7, 14, 30, 60, 90, 180, 365];
        private static readonly int[] QuotaOptions = [1, 2, 5, 10, 25, 50, 100, 250, 500];
        private static readonly int[] EndWindowOptions = [30, 60, 120, 300, 600, 900, 1800, 3600];

        private readonly Func<AppMessage, Task> dispatch;

        private UILabel? accountLabel;
        private UIButton? logoutButton;
        private UIButton? retentionButton;
        private UIButton? quotaButton;
        private UIButton? autoResumeButton;
        private UIButton? endWindowButton;
        private UILabel? serverLabel;
        private UILabel? statusLabel;

        private AppSettings settings = new();
        private AppModel? lastModel;

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

            accountLabel = TvosControls.Body();
            accountLabel.TextColor = UIColor.White;
            logoutButton = TvosControls.Button(
                "Abmelden",
                () => _ = dispatch(new LogoutRequestedMessage()));
            content.AddArrangedSubview(TvosControls.Section(
                TvosControls.Heading("Konto"),
                accountLabel,
                logoutButton));

            retentionButton = TvosControls.Button(
                string.Empty,
                () => ShowValuePicker(
                    "Aufbewahrungszeit für Downloads",
                    settings.DownloadRetentionDays,
                    RetentionOptions,
                    FormatDays,
                    value => SaveSettings(s => s with { DownloadRetentionDays = value })));
            quotaButton = TvosControls.Button(
                string.Empty,
                () => ShowValuePicker(
                    "Speicherplatzkontingent für Downloads",
                    settings.DownloadQuotaGb,
                    QuotaOptions,
                    value => $"{value} GB",
                    value => SaveSettings(s => s with { DownloadQuotaGb = value })));
            content.AddArrangedSubview(TvosControls.Section(
                TvosControls.Heading("Downloads"),
                retentionButton,
                quotaButton));

            autoResumeButton = TvosControls.Button(
                string.Empty,
                () => SaveSettings(
                    s => s with { AutoResumeEnabled = !s.AutoResumeEnabled }));
            endWindowButton = TvosControls.Button(
                string.Empty,
                () => ShowValuePicker(
                    "„Angesehen“-Endfenster vor dem Ende",
                    settings.WatchedEndWindowSeconds,
                    EndWindowOptions,
                    FormatEndWindow,
                    value => SaveSettings(s => s with { WatchedEndWindowSeconds = value })));
            content.AddArrangedSubview(TvosControls.Section(
                TvosControls.Heading("Wiedergabe"),
                autoResumeButton,
                endWindowButton));

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

            if (lastModel is not null)
            {
                ApplyRender(lastModel);
            }
        }

        /// <inheritdoc/>
        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // Beim Seitenaufruf den persistierten Stand erneut laden —
            // die Anzeige folgt anschließend dem Modell.
            _ = dispatch(new LoadSettingsRequestedMessage());
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
            settings = model.Settings;

            accountLabel!.Text = string.IsNullOrWhiteSpace(model.LoginUsername)
                ? "Angemeldet"
                : $"Angemeldet als: {model.LoginUsername}";
            // Die Abmeldung ist nur aus einem angemeldeten Zustand
            // wirksam (Guard: UpdateLogoutRequested — Muster
            // ProfilePageViewController).
            logoutButton!.Enabled =
                model.ConnectionState is ConnectionState.Connected or ConnectionState.Offline;

            retentionButton!.SetTitle(
                $"Aufbewahrungszeit für Downloads: {FormatDays(settings.DownloadRetentionDays)}",
                UIControlState.Normal);
            quotaButton!.SetTitle(
                $"Speicherplatzkontingent: {settings.DownloadQuotaGb} GB",
                UIControlState.Normal);
            autoResumeButton!.SetTitle(
                $"Auto-Fortsetzung: {(settings.AutoResumeEnabled ? "Ein" : "Aus")}",
                UIControlState.Normal);
            endWindowButton!.SetTitle(
                $"„Angesehen“-Endfenster: {FormatEndWindow(settings.WatchedEndWindowSeconds)}",
                UIControlState.Normal);

            serverLabel!.Text = model.ActiveServerAddress is null
                ? "Es ist kein Server eingerichtet."
                : $"Server: {model.ActiveServerAddress}";
            statusLabel!.Text = model.ActiveServerAddress is null
                ? string.Empty
                : model.IsOffline
                    ? "Offline — lokale Daten; keine Serververbindung."
                    : model.ServerHealthText;
        }

        // Änderung an den Einstellungen in den MVU-Zyklus melden —
        // das Update normalisiert und persistiert den Stand
        // (PersistSettingsEffect → AppSettingsService → NSUserDefaults).
        private void SaveSettings(Func<AppSettings, AppSettings> change) =>
            _ = dispatch(new SaveSettingsRequestedMessage(change(settings)));

        // Werteauswahl einer numerischen Einstellung als fernbedienungs-
        // taugliche Auswahlliste — alle Vorgaben liegen innerhalb der
        // Normalisierungs-Schranken; ein abweichender Ist-Wert wird
        // sortiert mit angeboten (Muster: Aktionsmenü der Download-Liste).
        private void ShowValuePicker(
            string title,
            int currentValue,
            IReadOnlyList<int> options,
            Func<int, string> formatValue,
            Action<int> apply)
        {
            var values = options.Contains(currentValue)
                ? options
                : options.Append(currentValue).OrderBy(v => v).ToArray();

            var alert = UIAlertController.Create(
                title,
                $"Aktuell: {formatValue(currentValue)}",
                UIAlertControllerStyle.ActionSheet);
            foreach (var value in values)
            {
                alert.AddAction(UIAlertAction.Create(
                    formatValue(value),
                    UIAlertActionStyle.Default,
                    action => apply(value)));
            }

            alert.AddAction(UIAlertAction.Create(
                "Zurück", UIAlertActionStyle.Cancel, null));
            PresentViewController(alert, true, null);
        }

        private static string FormatDays(int value) =>
            value == 1 ? "1 Tag" : $"{value} Tage";

        private static string FormatEndWindow(int seconds) =>
            seconds % 60 == 0
                ? $"{seconds / 60} {(seconds == 60 ? "Minute" : "Minuten")}"
                : $"{seconds} Sekunden";
    }
}
