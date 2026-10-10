using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Favoriten (Menüseite „Favoriten"): listet die Favoriten des
/// angemeldeten Server-Benutzers aus der lokalen Kopie
/// <c>cached_favorites</c> (online zuvor vom Serverstand erneuert) mit
/// Poster, Titel und Medientyp. Einträge lassen sich online direkt
/// entfernen (<c>api/favorites/remove</c>); im Offline-Zustand wird der
/// Cache-Stand mit Hinweis gezeigt und das Entfernen ist gesperrt. Die
/// Auswahl navigiert über den MVU-Kreislauf zur Detailseite.
/// </summary>
public partial class FavoritesPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="FavoritesPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Favoriten-Nachrichten.</param>
    public FavoritesPage(MvuHost mvuHost)
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
        // Beim Öffnen (neu) laden: online wird die lokale Kopie zuerst
        // vom Serverstand erneuert, offline der Cache-Stand gezeigt.
        _ = mvuHost.DispatchAsync(new FavoritesPageOpenedMessage());
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
        OfflineBanner.IsVisible = model.IsOffline;
        ErrorPanel.IsVisible = model.FavoritesPage.HasError && !model.FavoritesPage.IsLoading;
        ErrorLabel.Text = model.FavoritesPage.Error;
        EmptyLabel.IsVisible = model.FavoritesPage.ShowEmpty;
    }

    private void OnFavoriteSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FavoriteListItem item)
        {
            // Die Auswahl läuft über den MVU-Kreislauf — Update entscheidet
            // über die Detail-Navigation (inkl. Serienkontext-Hints).
            _ = mvuHost.DispatchAsync(new MediaItemSelectedMessage(item.ToMediaItem()));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRemoveClicked(object? sender, EventArgs e)
    {
        if ((sender as Element)?.BindingContext is FavoriteListItem item)
        {
            _ = mvuHost.DispatchAsync(new FavoriteRemoveRequestedMessage(item));
        }
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new FavoritesPageOpenedMessage());
    }
}
