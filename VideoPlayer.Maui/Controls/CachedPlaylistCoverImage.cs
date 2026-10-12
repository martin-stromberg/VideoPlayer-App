using Microsoft.Extensions.DependencyInjection;
using VideoPlayer.Maui.Services;

namespace VideoPlayer.Maui.Controls;

/// <summary>
/// <see cref="Image"/> mit Bindable Property <see cref="PlaylistId"/>:
/// löst das Cover einer Playlist (<c>api/playlists/{id}/cover</c>)
/// über den <see cref="PosterImageService"/> in eine Bildquelle auf —
/// zuerst aus der lokalen Vorschaubild-Ablage, sonst nach Abruf vom
/// Server. Playlists ohne Cover bzw. fehlgeschlagene Abrufe ergeben
/// keine Quelle — der Titel-Fallback der Karte bleibt sichtbar.
/// </summary>
public sealed class CachedPlaylistCoverImage : Image
{
    /// <value>Bindable Property für <see cref="PlaylistId"/> — Id der Playlist, deren Cover angezeigt wird.</value>
    public static readonly BindableProperty PlaylistIdProperty = BindableProperty.Create(
        nameof(PlaylistId),
        typeof(long?),
        typeof(CachedPlaylistCoverImage),
        null,
        propertyChanged: OnPlaylistIdChanged);

    /// <summary>Id der Playlist; <c>null</c> oder ≤ 0 = kein Cover.</summary>
    public long? PlaylistId
    {
        get => (long?)GetValue(PlaylistIdProperty);
        set => SetValue(PlaylistIdProperty, value);
    }

    private static void OnPlaylistIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CachedPlaylistCoverImage image)
        {
            image.Source = null;
            _ = image.LoadAsync((long?)newValue);
        }
    }

    private async Task LoadAsync(long? playlistId)
    {
        if (playlistId is null or <= 0)
        {
            return;
        }

        var service = IPlatformApplication.Current?.Services?.GetService<PosterImageService>();
        if (service is null)
        {
            return;
        }

        var source = await service.GetPlaylistCoverSourceAsync(playlistId.Value);
        // Zwischenzeitlich neu belegtes Element nicht mit dem alten Bild
        // befüllen.
        if (PlaylistId == playlistId)
        {
            Source = source;
        }
    }
}
