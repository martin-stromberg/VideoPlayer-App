using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>Ladeanzeige für den App-Start (kein Eintrag im Hauptmenü).</summary>
public partial class LoadingPage : ContentPage
{
    /// <summary>Initialisiert eine neue Instanz der <see cref="LoadingPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> für die Fortschrittsanzeige.</param>
    public LoadingPage(MvuHost mvuHost)
    {
        InitializeComponent();
        BindingContext = mvuHost;
    }
}
