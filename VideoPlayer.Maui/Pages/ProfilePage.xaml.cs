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

    /// <summary>Initialisiert eine neue Instanz der <see cref="ProfilePage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Profil-Nachrichten.</param>
    public ProfilePage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Beim Öffnen die Profilinformationen (erneut) ermitteln.
        _ = mvuHost.DispatchAsync(new ProfilePageOpenedMessage());
    }

    private void OnLogoutClicked(object? sender, EventArgs e)
    {
        // Die Abmeldung läuft über den MVU-Kreislauf — Update setzt den
        // Sitzungszustand zurück und löst Trennung, Räumung und
        // Navigation zur Anmeldemaske aus.
        _ = mvuHost.DispatchAsync(new LogoutRequestedMessage());
    }
}
