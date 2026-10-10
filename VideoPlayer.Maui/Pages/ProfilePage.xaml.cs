using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Profil (Menüseite „Profil"): zeigt den angemeldeten Server-Benutzer
/// (Benutzername und E-Mail aus den Claims des gespeicherten
/// Anmelde-Tokens bzw. der gespeicherten Anmelde-Kennung) und das
/// aktive lokale Profil. „Abmelden" beendet die Sitzung lokal über den
/// MVU-Kreislauf (Token/Zugangsdaten werden verworfen, der Zustand
/// zurückgesetzt) und führt zur Anmeldemaske.
/// </summary>
public partial class ProfilePage : ContentPage
{
    private readonly MvuHost mvuHost;
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="ProfilePage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Profil-Nachrichten.</param>
    public ProfilePage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        UpdateUi(mvuHost.Model);
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Das ModelChanged-Abo gilt nur, solange die Seite sichtbar ist
        // (Muster: SettingsPage) — verlassene Instanzen dürfen nicht
        // weiter am MvuHost hängen.
        subscribed = true;
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
        // Beim Öffnen die Profilinformationen (erneut) ermitteln —
        // offline greift die Einschränkung des MVU-Updates.
        _ = mvuHost.DispatchAsync(new ProfilePageOpenedMessage());
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
        subscribed = false;
        mvuHost.ModelChanged -= OnModelChanged;
        base.OnDisappearing();
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (subscribed)
            {
                UpdateUi(model);
            }
        });
    }

    private void UpdateUi(AppModel model)
    {
        // Ohne Serververbindung: Hinweis statt Benutzerbereich — lokale
        // Installationsdaten und „Abmelden" bleiben verfügbar.
        ProfileOfflineBanner.IsVisible = model.IsOffline;
        UserInfoBorder.IsVisible = !model.IsOffline;
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        // Die Abmeldung läuft über den MVU-Kreislauf — Update setzt den
        // Sitzungszustand zurück und löst Trennung, Räumung und
        // Navigation zur Anmeldemaske aus.
        _ = mvuHost.DispatchAsync(new LogoutRequestedMessage());
    }
}
