using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Quellen-Übersicht (Menüseite „Quellen"): listet die vom Server
/// freigegebenen Medienbereiche mit Icon und Name. Im Offline-Zustand
/// wird ein Hinweis statt einer leeren oder veralteten Liste gezeigt.
/// Alle Benutzeraktionen laufen über den MVU-Kreislauf.
/// </summary>
public partial class SourcesPage : ContentPage
{
    private readonly MvuHost mvuHost;

    public SourcesPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Live-Stand der Server-API laden (im Offline-Zustand löst die
        // Message keinen Ladevorgang aus).
        _ = mvuHost.DispatchAsync(new SourcesPageOpenedMessage());
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(model));
    }

    private void UpdateUi(AppModel model)
    {
        OfflineBanner.IsVisible = model.IsOffline;
        SourcesCollection.IsVisible = !model.IsOffline;
        ErrorPanel.IsVisible = !model.IsOffline && model.SourcesOverview.HasError;
        ErrorLabel.Text = model.SourcesOverview.Error;
        EmptyLabel.IsVisible = !model.IsOffline && model.SourcesOverview.ShowEmpty;
    }

    private void OnSourceSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SourceItem source)
        {
            _ = mvuHost.DispatchAsync(new SourceSelectedMessage(source));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new SourcesPageOpenedMessage());
    }
}
