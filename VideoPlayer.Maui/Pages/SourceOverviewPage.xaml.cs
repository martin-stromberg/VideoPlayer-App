using VideoPlayer.Mvu;

namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Browsing-Seite einer Quelle (Shell-Route „sourceoverview?id=…&amp;name=…"):
/// Genre-Leiste mit „Alle"-Eintrag, Suchfeld, Medien-Raster mit
/// seitenweisem Nachladen — wie in der alten App (SourceOverviewPage).
/// Benutzeraktionen laufen über den MVU-Kreislauf; der Seiten-Status
/// steht in <see cref="AppModel.SourceBrowser"/>.
/// </summary>
[QueryProperty(nameof(SourceId), "id")]
[QueryProperty(nameof(SourceName), "name")]
public partial class SourceOverviewPage : ContentPage
{
    private readonly MvuHost mvuHost;
    private long sourceId;
    private string? sourceName;

    public SourceOverviewPage(MvuHost mvuHost)
    {
        InitializeComponent();
        this.mvuHost = mvuHost;
        BindingContext = mvuHost;
        mvuHost.ModelChanged += OnModelChanged;
        UpdateUi(mvuHost.Model);
    }

    /// <summary>Quellen-Id aus dem Navigationspfad.</summary>
    public string SourceId
    {
        set => sourceId = long.TryParse(Uri.UnescapeDataString(value ?? string.Empty), out var id) ? id : 0;
    }

    /// <summary>Quellenname aus dem Navigationspfad (Seitentitel).</summary>
    public string SourceName
    {
        set
        {
            sourceName = Uri.UnescapeDataString(value ?? string.Empty);
            Title = string.IsNullOrWhiteSpace(sourceName) ? "Quelle" : sourceName;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Beim ersten Öffnen (oder erneutem Aufruf einer anderen Quelle)
        // werden Genres + erste Seite geladen; im Offlinebetrieb ist die
        // Seite über die Quellenliste gar nicht erreichbar — ein
        // verwaister Zustand wird vom Update auf Initial zurückgesetzt.
        if (mvuHost.Model.SourceBrowser.SourceId != sourceId || !mvuHost.Model.SourceBrowser.Attempted)
        {
            _ = mvuHost.DispatchAsync(new SourceBrowserOpenedMessage(sourceId, sourceName ?? string.Empty));
        }
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        var model = mvuHost.Model;
        MainThread.BeginInvokeOnMainThread(() => UpdateUi(model));
    }

    private void UpdateUi(AppModel model)
    {
        var browser = model.SourceBrowser;
        ErrorPanel.IsVisible = browser.HasError && browser.Items.Count == 0;
        ErrorLabel.Text = browser.Error;
        FooterErrorPanel.IsVisible = browser.HasError && browser.Items.Count > 0;
        EmptyLabel.IsVisible = browser.ShowEmpty;
        ItemsCollection.IsVisible = !browser.ShowEmpty;
        // Suchtext des Browser-Zustands bei Rückkehr in das Feld
        // zurückspiegeln (Filter bleibt sichtbar aktiv).
        if (!string.Equals(MediaSearchBar.Text, browser.SearchText, StringComparison.Ordinal))
        {
            MediaSearchBar.Text = browser.SearchText;
        }
    }

    private void OnGenreSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SourceGenreItem genre)
        {
            _ = mvuHost.DispatchAsync(new SourceGenreSelectedMessage(genre.Id));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new SourceSearchSubmittedMessage(MediaSearchBar.Text));
    }

    private void OnMediaItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SourceMediaItem item)
        {
            _ = mvuHost.DispatchAsync(new SourceMediaItemSelectedMessage(item));
        }

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
    }

    private void OnRemainingItemsThresholdReached(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new SourceItemsNextPageRequestedMessage());
    }

    private void OnRetryClicked(object? sender, EventArgs e)
    {
        _ = mvuHost.DispatchAsync(new SourceItemsRetryRequestedMessage());
    }
}
