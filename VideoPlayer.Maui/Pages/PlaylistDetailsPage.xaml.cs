using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Detailseite einer Playlist (Shell-Route „playlistdetails?id=…&amp;name=…"):
/// Kopfbereich mit Cover, Name, Beschreibung, Sortiermodus und
/// Eintragszahl; darunter die Einträge in der serverseitigen Reihenfolge
/// mit seitenweisem Nachladen (<c>api/playlists/{id}/entries/paged</c>)
/// — wie das Quellen-Browsing (SourceOverviewPage). Benutzeraktionen
/// laufen über den MVU-Kreislauf; der Seiten-Status steht in
/// <see cref="AppModel.PlaylistDetails"/>.
/// </summary>
[QueryProperty(nameof(PlaylistId), "id")]
[QueryProperty(nameof(PlaylistName), "name")]
public partial class PlaylistDetailsPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private long playlistId;
    private string? playlistName;
    private bool subscribed;

    /// <summary>Initialisiert eine neue Instanz der <see cref="PlaylistDetailsPage"/>-Klasse.</summary>
    /// <param name="mvuHost">MVU-Host; dient als <see cref="BindableObject.BindingContext"/> und Dispatch-Ziel der Playlist-Nachrichten.</param>
    public PlaylistDetailsPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        UpdateUi(mvuHost.Model);
    }

    /// <summary>Playlist-Id aus dem Navigationspfad.</summary>
    public string PlaylistId
    {
        set => playlistId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) ? id : 0;
    }

    /// <summary>Playlist-Name aus dem Navigationspfad (Seitentitel-Fallback).</summary>
    public string PlaylistName
    {
        set
        {
            playlistName = Uri.UnescapeDataString(value ?? string.Empty);
            Title = string.IsNullOrWhiteSpace(playlistName) ? "Playlist" : playlistName;
        }
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
        // Bei geöffneter Playlist ohne Zustand (Wiederherstellungspfad)
        // werden Kopfdaten und erste Eintragsseite geladen; bei Rückkehr
        // auf dieselbe Playlist bleibt der Stand erhalten.
        if (mvuHost.Model.PlaylistDetails.PlaylistId != playlistId || !mvuHost.Model.PlaylistDetails.Attempted)
        {
            _ = mvuHost.DispatchAsync(new PlaylistDetailsOpenedMessage(playlistId, playlistName ?? string.Empty));
        }
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
        var details = model.PlaylistDetails;
        ErrorPanel.IsVisible = details.HasError && details.Entries.Count == 0;
        ErrorLabel.Text = details.Error;
        FooterErrorPanel.IsVisible = details.HasError && details.Entries.Count > 0;
        EmptyLabel.IsVisible = details.ShowEmpty;
        EntriesCollection.IsVisible = !details.ShowEmpty;
        if (details.Name.Length > 0)
        {
            Title = details.Name;
        }
    }

    private void OnEntrySelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PlaylistEntryItem item)
        {
            _ = mvuHost.DispatchAsync(new PlaylistEntrySelectedMessage(item));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistEntriesNextPageRequestedMessage());
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new PlaylistEntriesRetryRequestedMessage());
    }
}
