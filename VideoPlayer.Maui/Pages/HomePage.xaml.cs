using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Startseite (Grundgerüst). Zeigt den Serverstatus aus dem MVU-Model sowie
/// im Offline-Zustand einen Hinweis auf den eingeschränkten Betrieb.
/// </summary>
public partial class HomePage : ContentPage
{
    private readonly MvuHost mvuHost;

    public HomePage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        UpdateUi(mvuHost.Model);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        mvuHost.ModelChanged -= OnModelChanged;
        mvuHost.ModelChanged += OnModelChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        mvuHost.ModelChanged -= OnModelChanged;
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(mvuHost.Model));
    }

    private void UpdateUi(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
    }
}
