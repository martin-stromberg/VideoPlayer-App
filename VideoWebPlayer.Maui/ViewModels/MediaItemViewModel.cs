using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VideoWebPlayer.Maui.Services;
using VideoWebPlayer.Maui.Models;

namespace VideoWebPlayer.Maui.ViewModels;

/// <summary>
/// ViewModel für ein Medien-Element (Film, Serie, Episode oder Download), das in Listen und Karussells angezeigt wird.
/// </summary>
public class MediaItemViewModel : INotifyPropertyChanged
{
    private string? _title;
    private string? _imageUrl;
    private ImageSource? _imageSource;
    private long? _entryId;
    private string? _mediaType;
    private Color _backgroundColor = Colors.Transparent;
    private long? _seasonId;
    private long? _episodeId;

    /// <summary>
    /// Der Anzeigetitel des Elements.
    /// </summary>
    public string? Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die URL des Poster-Bildes (relativ zur API oder absolut).
    /// </summary>
    public string? ImageUrl
    {
        get => _imageUrl;
        set
        {
            if (_imageUrl != value)
            {
                _imageUrl = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Optional: die ursprüngliche Poster-Bild-ID, damit Aufrufer Bilder über den API-Client anfordern können.
    /// </summary>
    public long? PosterPictureId { get; set; }

    /// <summary>
    /// Die geladene Bildquelle für das Poster des Elements.
    /// </summary>
    public ImageSource? ImageSource
    {
        get => _imageSource;
        set
        {
            if (_imageSource != value)
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die aus dem Poster-Bild extrahierte Hintergrundfarbe des Elements.
    /// </summary>
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die ID des zugehörigen Eintrags (z. B. Collection-, Show- oder Video-ID).
    /// </summary>
    public long? EntryId
    {
        get => _entryId;
        set
        {
            if (_entryId != value)
            {
                _entryId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Der Medientyp des Elements (z. B. "collection", "show", "movie" oder "episode").
    /// </summary>
    public string? MediaType
    {
        get => _mediaType;
        set
        {
            if (_mediaType != value)
            {
                _mediaType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die ID der Staffel, sofern das Element zu einer Staffel gehört.
    /// </summary>
    public long? SeasonId
    {
        get => _seasonId;
        set
        {
            if (_seasonId != value)
            {
                _seasonId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die ID der Episode, sofern das Element eine Episode ist.
    /// </summary>
    public long? EpisodeId
    {
        get => _episodeId;
        set
        {
            if (_episodeId != value)
            {
                _episodeId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob dieses Element aus dem lokalen Cache stammt und noch nicht durch Live-Daten ersetzt wurde.
    /// </summary>
    public bool IsFromCache { get; set; }

    /// <summary>
    /// Befehl zum Löschen des Downloads dieses Elements (mit Bestätigungsdialog).
    /// </summary>
    public ICommand DeleteDownloadCommand { get; }

    /// <summary>
    /// Erstellt eine neue Instanz des <see cref="MediaItemViewModel"/>.
    /// </summary>
    public MediaItemViewModel()
    {
        DeleteDownloadCommand = new Command(async () => await OnDeleteDownloadAsync());
    }

    private async Task OnDeleteDownloadAsync()
    {
        // Zeige Bestätigungsdialog
        var result = await VideoWebPlayer.Maui.App.SafeDisplayAlertAsync(
            "Download löschen?",
            $"'{Title}' und die lokalen Dateien werden gelöscht.",
            "Löschen",
            "Abbrechen");

        if (result)
        {
            try
            {
                // Lösche die Download-Datei
                await DownloadManager.Instance.DeleteDownloadAsync(
                    EntryId ?? 0,
                    MediaType ?? "movie");

                System.Diagnostics.Debug.WriteLine($"[MediaItemViewModel] Deleted: {Title}");
            }
            catch (Exception ex)
            {
                await VideoWebPlayer.Maui.App.SafeDisplayAlertAsync(
                    "Fehler",
                    $"Download konnte nicht gelöscht werden: {ex.Message}",
                    "OK");
            }
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Löst das <see cref="PropertyChanged"/>-Ereignis aus.
    /// </summary>
    /// <param name="propertyName">Der Name der geänderten Eigenschaft (wird automatisch ermittelt).</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
