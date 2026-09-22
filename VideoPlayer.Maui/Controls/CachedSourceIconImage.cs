using Microsoft.Extensions.DependencyInjection;
using VideoPlayer.Maui.Services;

namespace VideoPlayer.Maui.Controls;

/// <summary>
/// <see cref="Image"/> mit Bindable Property <see cref="PictureId"/> für
/// Quellen-Icons: löst die Server-Bild-Id (<c>api/sourceicons/{id}</c>)
/// über <see cref="PosterImageService.GetSourceIconSourceAsync"/> in eine
/// Bildquelle auf — zuerst aus der lokalen Vorschaubild-Ablage, sonst
/// nach Abruf vom Server. Spiegel von <see cref="CachedPosterImage"/>
/// für den Icon-Endpunkt.
/// </summary>
public sealed class CachedSourceIconImage : Image
{
    public static readonly BindableProperty PictureIdProperty = BindableProperty.Create(
        nameof(PictureId),
        typeof(long?),
        typeof(CachedSourceIconImage),
        null,
        propertyChanged: OnPictureIdChanged);

    /// <summary>Icon-Bild-Id; <c>null</c> oder ≤ 0 = kein Icon.</summary>
    public long? PictureId
    {
        get => (long?)GetValue(PictureIdProperty);
        set => SetValue(PictureIdProperty, value);
    }

    private static void OnPictureIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CachedSourceIconImage image)
        {
            image.Source = null;
            _ = image.LoadAsync((long?)newValue);
        }
    }

    private async Task LoadAsync(long? pictureId)
    {
        if (pictureId is null or <= 0)
        {
            return;
        }

        var service = IPlatformApplication.Current?.Services?.GetService<PosterImageService>();
        if (service is null)
        {
            return;
        }

        var source = await service.GetSourceIconSourceAsync(pictureId.Value);
        // Zwischenzeitlich neu belegtes Element nicht mit dem alten Icon
        // befüllen.
        if (PictureId == pictureId)
        {
            Source = source;
        }
    }
}
