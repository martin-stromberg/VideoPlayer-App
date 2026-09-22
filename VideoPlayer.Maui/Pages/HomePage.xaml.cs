using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Startseite der App: horizontale Medienlisten „Weiterschauen",
/// „Favoriten" und „Neu im Programm" aus der lokalen Cache-Ablage
/// (Cache-First, Aktualisierung über MVU-Modell nach Synchronisation und
/// Server-Ereignissen). Die Auswahl eines Eintrags navigiert über den MVU-
/// Kreislauf zur Detailseite des Mediums.
/// </summary>
public partial class HomePage : ContentPage
{
    private readonly MvuHost mvuHost;

    public HomePage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateOfflineBanner(mvuHost.Model);
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        // UI-Anpassungen auf dem Hauptthread; die Listen binden über
        // BindingContext an das MVU-Modell.
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateOfflineBanner(model));
    }

    private void UpdateOfflineBanner(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
    }

    private void OnMediaItemTapped(object? sender, HomeMediaItem item)
    {
        // Die Auswahl läuft über den MVU-Kreislauf — Update entscheidet
        // über die Detail-Navigation.
        _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(item));
    }
}
