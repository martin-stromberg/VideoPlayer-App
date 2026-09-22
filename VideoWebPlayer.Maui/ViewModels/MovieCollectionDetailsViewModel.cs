using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SkiaSharp;
using VideoWebPlayer.Client;
using VideoWebPlayer.Client.Models;
using VideoWebPlayer.Maui.Components;

namespace VideoWebPlayer.Maui.ViewModels;

/// <summary>
/// ViewModel für die Detailseite einer Film-Collection mit Banner, Filmliste und Favoritenstatus.
/// </summary>
public class MovieCollectionDetailsViewModel : INotifyPropertyChanged, IMediaBannerViewModel
{
    private readonly VideoWebPlayerClient? _client;
    private bool _isLoading;
    private string? _title;
    private string? _plot;
    private string? _genreNames;
    private string? _releaseYear;
    private ImageSource? _collectionBannerSource;
    private ImageSource? _displayBannerSource;
    private Color _displayBannerBackgroundColor = Colors.Transparent;
    private int _selectedMovieIndex = -1;
    private bool _isSelectedMovieFavorite;

    /// <summary>
    /// Die ID der Film-Collection.
    /// </summary>
    public long CollectionId { get; }

    /// <summary>
    /// Der Titel der Collection.
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
    /// Die Handlungsbeschreibung des aktuell ausgewählten Films.
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
    /// Die Genre-Namen des aktuell ausgewählten Films als Text.
    /// </summary>
    public string? GenreNames
    {
        get => _genreNames;
        set
        {
            if (_genreNames != value)
            {
                _genreNames = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Das Erscheinungsjahr des aktuell ausgewählten Films.
    /// </summary>
    public string? ReleaseYear
    {
        get => _releaseYear;
        set
        {
            if (_releaseYear != value)
            {
                _releaseYear = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die Bildquelle des aktuell angezeigten Banners (Collection- oder Film-Banner).
    /// </summary>
    public ImageSource? DisplayBannerSource
    {
        get => _displayBannerSource;
        set
        {
            if (_displayBannerSource != value)
            {
                _displayBannerSource = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die aus dem Banner-Bild extrahierte Hintergrundfarbe.
    /// </summary>
    public Color DisplayBannerBackgroundColor
    {
        get => _displayBannerBackgroundColor;
        set
        {
            if (_displayBannerBackgroundColor != value)
            {
                _displayBannerBackgroundColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gibt an, ob die Daten der Collection gerade geladen werden.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Der Index des aktuell ausgewählten Films in <see cref="Movies"/> (-1 = keine Auswahl).
    /// </summary>
    public int SelectedMovieIndex
    {
        get => _selectedMovieIndex;
        set
        {
            if (_selectedMovieIndex != value)
            {
                _selectedMovieIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMovie));

                UpdateSelectedMovieFavorite();

                if (_selectedMovieIndex >= 0)
                {
                    _ = LoadMovieBannerAsync();
                }
            }
        }
    }

    /// <summary>
    /// Gibt an, ob der aktuell ausgewählte Film als Favorit markiert ist.
    /// </summary>
    public bool IsSelectedMovieFavorite
    {
        get => _isSelectedMovieFavorite;
        set
        {
            if (_isSelectedMovieFavorite != value)
            {
                _isSelectedMovieFavorite = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMovieFavoriteStarText));
            }
        }
    }

    /// <summary>
    /// Das Stern-Symbol für den Favoritenstatus des ausgewählten Films (gefüllt = Favorit, leer = kein Favorit).
    /// </summary>
    public string SelectedMovieFavoriteStarText => IsSelectedMovieFavorite ? "★" : "☆";

    /// <summary>
    /// Die Filme der Collection.
    /// </summary>
    /// <value>Liste der Filme; initial leer.</value>
    public ObservableCollection<MediaItemViewModel> Movies { get; } = new();

    /// <summary>
    /// Der aktuell ausgewählte Film oder <c>null</c>, wenn keine gültige Auswahl vorliegt.
    /// </summary>
    public MediaItemViewModel? SelectedMovie => SelectedMovieIndex >= 0 && SelectedMovieIndex < Movies.Count
        ? Movies[SelectedMovieIndex]
        : null;

    /// <summary>
    /// Gibt an, ob die Filmliste angezeigt werden soll (nur bei mehr als einem Film).
    /// </summary>
    public bool ShowMoviesList => Movies.Count > 1;

    private readonly Dictionary<long, DtoMovie> _movieDetailsCache = new();

    /// <summary>
    /// Erstellt eine neue Instanz des <see cref="MovieCollectionDetailsViewModel"/>.
    /// </summary>
    /// <param name="collectionId">Die ID der anzuzeigenden Film-Collection.</param>
    public MovieCollectionDetailsViewModel(long collectionId)
    {
        CollectionId = collectionId;

        _client = App.ServiceProvider?.GetService<VideoWebPlayerClient>();
    }

    /// <summary>
    /// Lädt die Collection-Daten (Banner, Filme, Favoritenstatus) vom Server.
    /// </summary>
    /// <param name="initialMovieId">Optionale ID eines Films, der nach dem Laden ausgewählt werden soll.</param>
    public async Task LoadDataAsync(long? initialMovieId = null)
    {
        if (_client == null)
            return;

        IsLoading = true;

        try
        {
            var collection = await _client.RequestMovieCollectionAsync(CollectionId) as DtoMovieCollection;

            if (collection != null)
            {
                Title = collection.Name;

                // Lade Collection Banner über API-Client
                if (collection.BannerPictureId.HasValue)
                {
                    await LoadCollectionBannerAsync(collection.BannerPictureId.Value);
                }

                Movies.Clear();
                if (collection.Movies != null)
                {
                    foreach (var movie in collection.Movies.OrderBy(m => m.ReleaseDate ?? DateTime.MinValue))
                    {
                        var movieVm = new MediaItemViewModel
                        {
                            Title = movie.Name,
                            EntryId = movie.Id,
                            ImageSource = "placeholder.png"
                        };

                        if (movie.PosterPictureId.HasValue)
                        {
                            // store picture id and load via API client
                            movieVm.PosterPictureId = movie.PosterPictureId;
                            _ = LoadMoviePosterImageAsync(movie.PosterPictureId.Value, movieVm);
                        }

                        Movies.Add(movieVm);

                        // Cache movie details
                        _movieDetailsCache[movie.Id] = movie;
                    }
                }

                OnPropertyChanged(nameof(ShowMoviesList));

                // Film auswählen
                if (Movies.Count > 0)
                {
                    if (initialMovieId.HasValue)
                    {
                        var movieIndex = Movies.ToList().FindIndex(m => m.EntryId == initialMovieId.Value);
                        SelectedMovieIndex = movieIndex >= 0 ? movieIndex : 0;
                    }
                    else
                    {
                        // Wähle automatisch den ersten Film (egal wie viele es gibt)
                        SelectedMovieIndex = 0;
                    }

                    UpdateSelectedMovieFavorite();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading movie collection: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateSelectedMovieFavorite()
    {
        var movieId = SelectedMovie?.EntryId;
        if (!movieId.HasValue)
        {
            IsSelectedMovieFavorite = false;
            return;
        }

        if (_movieDetailsCache.TryGetValue(movieId.Value, out var movie))
        {
            IsSelectedMovieFavorite = movie.IsFavorite;
        }
        else
        {
            IsSelectedMovieFavorite = false;
        }
    }

    /// <summary>
    /// Schaltet den Favoritenstatus des aktuell ausgewählten Films auf dem Server um.
    /// </summary>
    public async Task ToggleSelectedMovieFavoriteAsync()
    {
        if (_client == null)
            return;

        var movieId = SelectedMovie?.EntryId;
        if (!movieId.HasValue)
            return;

        try
        {
            var isFav = await _client.ToggleFavorite(new DtoMovie { Id = movieId.Value, Name = SelectedMovie?.Title ?? string.Empty });
            if (_movieDetailsCache.TryGetValue(movieId.Value, out var movie))
            {
                movie.IsFavorite = isFav;
            }
            IsSelectedMovieFavorite = isFav;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] Error toggling movie favorite: {ex.Message}");
        }
    }

    private async Task<Color> ExtractAverageColorAsync(byte[] imageBytes)
    {
        try
        {
            using var stream = new MemoryStream(imageBytes);
            using var bitmap = SKBitmap.Decode(stream);

            if (bitmap == null || bitmap.Width == 0 || bitmap.Height == 0)
                return Colors.Transparent;

            int sampleSize = Math.Min(10, Math.Min(bitmap.Width, bitmap.Height));
            long totalR = 0, totalG = 0, totalB = 0;
            int pixelCount = 0;

            for (int y = 0; y < sampleSize; y++)
            {
                for (int x = 0; x < sampleSize; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    totalR += pixel.Red;
                    totalG += pixel.Green;
                    totalB += pixel.Blue;
                    pixelCount++;
                }
            }

            if (pixelCount > 0)
            {
                byte avgR = (byte)(totalR / pixelCount);
                byte avgG = (byte)(totalG / pixelCount);
                byte avgB = (byte)(totalB / pixelCount);

                return Color.FromRgb(avgR, avgG, avgB);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting pixel color: {ex.Message}");
        }

        return Colors.Transparent;
    }

    private async Task LoadCollectionBannerAsync(long pictureId)
    {
        try
        {
            if (_client == null)
                return;

            var imageBytes = await _client.GetPictureAsync(pictureId);
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            var backgroundColor = await ExtractAverageColorAsync(imageBytes);

            var imageSource = new StreamImageSource
            {
                Stream = (token) => Task.FromResult((Stream)new MemoryStream(imageBytes))
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _collectionBannerSource = imageSource;
                DisplayBannerSource = imageSource;
                DisplayBannerBackgroundColor = backgroundColor;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading collection banner image {pictureId}: {ex.Message}");
        }
    }

    private async Task LoadMovieBannerAsync()
    {
        if (SelectedMovie == null || !SelectedMovie.EntryId.HasValue)
            return;

        try
        {
            if (!_movieDetailsCache.TryGetValue(SelectedMovie.EntryId.Value, out var movie))
                return;

            // Aktualisiere Plot und Genre
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Plot = movie.Plot;
                GenreNames = movie.GenreNames;

                // Setze Erscheinungsjahr
                if (movie.ReleaseDate.HasValue)
                {
                    ReleaseYear = movie.ReleaseDate.Value.Year.ToString();
                }
                else if (movie.PremieredAt.HasValue)
                {
                    ReleaseYear = movie.PremieredAt.Value.Year.ToString();
                }
                else
                {
                    ReleaseYear = null;
                }
            });

            // Versuche Banner, dann Fanart, dann Poster
            long? imageId = movie.BannerPictureId ?? movie.FanartPictureId ?? movie.PosterPictureId;

            if (imageId.HasValue)
            {
                if (_client == null)
                    return;

                var imageBytes = await _client.GetPictureAsync(imageId.Value);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    var backgroundColor = await ExtractAverageColorAsync(imageBytes);

                    var imageSource = new StreamImageSource
                    {
                        Stream = (token) => Task.FromResult((Stream)new MemoryStream(imageBytes))
                    };

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayBannerSource = imageSource;
                        DisplayBannerBackgroundColor = backgroundColor;
                    });
                }
            }
            else
            {
                // Kein Bild vorhanden → Collection Banner anzeigen
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    DisplayBannerSource = _collectionBannerSource;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading movie banner: {ex.Message}");
            // Bei Fehler: Collection Banner anzeigen
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                DisplayBannerSource = _collectionBannerSource;
            });
        }
    }

    private async Task LoadMoviePosterImageAsync(long pictureId, MediaItemViewModel movie)
    {
        try
        {
            if (_client == null)
                return;

            var imageBytes = await _client.GetPictureAsync(pictureId);
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            var imageSource = new StreamImageSource
            {
                Stream = (token) => Task.FromResult((Stream)new MemoryStream(imageBytes))
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                movie.ImageSource = imageSource;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading movie image for picture {pictureId}: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public bool ShouldShowPlayButton => SelectedMovie != null;

    /// <inheritdoc/>
    public async Task<(long VideoId, string VideoType, string Title)?> GetVideoInfoForPlaybackAsync()
    {
        var movie = SelectedMovie;

        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] GetVideoInfoForPlaybackAsync called");
        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] SelectedMovie: {movie?.Title ?? "null"}");
        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] SelectedMovieIndex: {SelectedMovieIndex}");
        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] Movies.Count: {Movies.Count}");

        if (movie == null)
        {
            System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] SelectedMovie is null!");
            return null;
        }

        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] EntryId: {movie.EntryId}");

        if (!movie.EntryId.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] EntryId has no value!");
            return null;
        }

        var result = (movie.EntryId.Value, Models.MediaTypes.Movie, movie.Title ?? "Unknown Movie");
        System.Diagnostics.Debug.WriteLine($"[MovieCollectionDetailsViewModel] Returning: VideoId={result.Item1}, VideoType={result.Item2}, Title={result.Item3}");

        return result;
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
