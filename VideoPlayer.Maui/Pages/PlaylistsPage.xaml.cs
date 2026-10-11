using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Playlists (Menüseite „Playlists"): listet die eigenen und die
/// öffentlichen Playlists des angemeldeten Server-Benutzers in einer
/// scrollbaren Liste mit Sektionsüberschriften, Cover, Name,
/// Sortiermodus und Genres. Reines Online-Feature — ohne
/// Serververbindung zeigt die Seite eine verständliche Fehleranzeige
/// mit Neuversuch. Die Auswahl öffnet über den MVU-Kreislauf die
/// Playlist-Detailseite.
/// </summary>
public partial class PlaylistsPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Playlist-Nachrichten.</param>
    public PlaylistsPage(MvuHost mvuHost)
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
        // Beim Öffnen (neu) laden: online vom Server; ohne Verbindung
        // entscheidet das Update über die Fehleranzeige.
        _ = mvuHost.DispatchAsync(new PlaylistsPageOpenedMessage());
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
        var page = model.PlaylistsPage;
        ErrorPanel.IsVisible = page.HasError && !page.IsLoading;
        ErrorLabel.Text = page.Error;
        EmptyLabel.IsVisible = page.ShowEmpty;
        PlaylistsCollection.IsVisible = page.HasItems;
    }

    private void OnPlaylistSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PlaylistOverviewItem row
            && row.Playlist is { } playlist)
        {
            // Die Auswahl läuft über den MVU-Kreislauf — Update
            // entscheidet über die Detail-Navigation; Überschriftenzeilen
            // sind nicht wählbar.
            _ = mvuHost.DispatchAsync(new PlaylistSelectedMessage(playlist));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistsPageOpenedMessage());
    }
}
