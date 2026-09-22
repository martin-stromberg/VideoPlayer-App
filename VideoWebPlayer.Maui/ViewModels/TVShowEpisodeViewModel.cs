using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoWebPlayer.Maui.ViewModels;

/// <summary>
/// ViewModel für eine einzelne Episode einer TV-Show in der Episodenliste.
/// </summary>
public class TVShowEpisodeViewModel : INotifyPropertyChanged
{
    private string? _title;
    private string? _plot;
    private string? _imageUrl;
    private ImageSource? _imageSource;
    private long _episodeId;
    private int _episodeNumber;
    private int _seasonNumber;
    private bool _isDownloaded;
    private bool _isFavorite;

    /// <summary>
    /// Der Titel der Episode.
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
    /// Die Handlungsbeschreibung der Episode.
    /// </summary>
    public string? Plot
    {
        get => _plot;
        set
        {
            if (_plot != value)
            {
                _plot = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die URL des Episoden-Bildes (relativ zur API oder absolut).
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
    /// Die geladene Bildquelle für das Episoden-Bild.
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
    /// Die ID der Episode.
    /// </summary>
    public long EpisodeId
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
    /// Die Episodennummer innerhalb der Staffel.
    /// </summary>
    public int EpisodeNumber
    {
        get => _episodeNumber;
        set
        {
            if (_episodeNumber != value)
            {
                _episodeNumber = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die Nummer der Staffel, zu der die Episode gehört.
    /// </summary>
    public int SeasonNumber
    {
        get => _seasonNumber;
        set
        {
            if (_seasonNumber != value)
            {
                _seasonNumber = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Episode lokal heruntergeladen wurde.
    /// </summary>
    public bool IsDownloaded
    {
        get => _isDownloaded;
        set
        {
            if (_isDownloaded != value)
            {
                _isDownloaded = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Episode als Favorit markiert ist.
    /// </summary>
    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite != value)
            {
                _isFavorite = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FavoriteStarText));
            }
        }
    }

    /// <summary>
    /// Das Stern-Symbol für den Favoritenstatus der Episode (gefüllt = Favorit, leer = kein Favorit).
    /// </summary>
    public string FavoriteStarText => IsFavorite ? "★" : "☆";

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
