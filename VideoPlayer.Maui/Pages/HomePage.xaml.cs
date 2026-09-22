using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>Startseite (Grundgerüst). Zeigt den Serverstatus aus dem MVU-Model.</summary>
public partial class HomePage : ContentPage
{
    public HomePage(MvuHost mvuHost)
    {
        InitializeComponent();
        BindingContext = mvuHost;
    }
}
