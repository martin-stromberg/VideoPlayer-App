using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>Ladeanzeige für den App-Start (kein Eintrag im Hauptmenü).</summary>
public partial class LoadingPage : ContentPage
{
    public LoadingPage(MvuHost mvuHost)
    {
        InitializeComponent();
        BindingContext = mvuHost;
    }
}
