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

    /// <summary>Initialisiert eine neue Instanz der <see cref="HomePage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Auswahl-Nachrichten.</param>
    public HomePage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateBanners(mvuHost.Model);
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        // UI-Anpassungen auf dem Hauptthread; die Listen binden über
        // BindingContext an das MVU-Modell.
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateBanners(model));
    }

    private void UpdateBanners(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
        StorageNoticeBanner.IsVisible = model.StorageNotice is not null;
    }

    private void OnMediaItemTapped(object? sender, HomeMediaItem item)
    {
        // Die Auswahl läuft über den MVU-Kreislauf — Update entscheidet
        // über die Detail-Navigation.
        _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(item));
    }
}
