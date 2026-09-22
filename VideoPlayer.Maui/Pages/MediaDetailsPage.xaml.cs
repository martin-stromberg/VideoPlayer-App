namespace VideoPlayer.Maui.Pages;

/// <summary>
/// Platzhalter-Detailseite für ein ausgewähltes Medium (Entwicklungsschritt
/// 8). Die Identität des Mediums kommt über die Shell-Route
/// <c>mediadetails?mediaType=…&amp;id=…&amp;title=…</c> und steht der
/// späteren echten Detailseite als <see cref="MediaType"/>/
/// <see cref="MediaId"/> zur Verfügung.
/// </summary>
[QueryProperty(nameof(MediaType), "mediaType")]
[QueryProperty(nameof(MediaId), "id")]
[QueryProperty(nameof(MediaTitle), "title")]
public partial class MediaDetailsPage : ContentPage
{
    private string? mediaType;
    private string? mediaTitle;

    public MediaDetailsPage()
    {
        InitializeComponent();
    }

    /// <summary>Medientyp der Auswahl (movie, tvshow, …).</summary>
    public string? MediaType
    {
        get => mediaType;
        set
        {
            mediaType = value;
            MediaTypeLabel.Text = MediaTypeText(value);
        }
    }

    /// <summary>Id des Mediums — interner Schlüssel für die spätere Detailseite.</summary>
    public long MediaId { get; set; }

    /// <summary>Titel des Mediums aus der Auswahl.</summary>
    public string? MediaTitle
    {
        get => mediaTitle;
        set
        {
            mediaTitle = value;
            MediaTitleLabel.Text = value;
        }
    }

    private static string MediaTypeText(string? mediaType) => mediaType switch
    {
        // CLR-Konvention (Favoriten/neue Inhalte) und Server-Konvention
        // (Weiterschauen: „show", „season", „episode", „collection").
        "movie" => "Film",
        "moviecollection" or "collection" => "Filmkollektion",
        "tvshow" or "show" => "Serie",
        "tvshowseason" or "season" => "Staffel",
        "tvshowepisode" or "episode" => "Episode",
        _ => "Medium"
    };
}
