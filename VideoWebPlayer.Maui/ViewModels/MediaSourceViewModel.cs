using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VideoWebPlayer.Maui.ViewModels;

/// <summary>
/// ViewModel für eine Medienquelle (Source), die in der Quellenübersicht angezeigt wird.
/// </summary>
public class MediaSourceViewModel : INotifyPropertyChanged
{
    private long _id;
    private string? _name;
    private string? _icon;
    private long? _iconPictureId;
    private ImageSource? _iconImageSource;

    /// <summary>
    /// Die ID der Medienquelle.
    /// </summary>
    public long Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Der Anzeigename der Medienquelle.
    /// </summary>
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Das Icon der Medienquelle (z. B. ein Emoji oder Symbol), falls kein Bild geladen wurde.
    /// </summary>
    public string? Icon
    {
        get => _icon;
        set
        {
            if (_icon != value)
            {
                _icon = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die Bild-ID des Quellen-Icons, über die das Bild per API-Client angefordert werden kann.
    /// </summary>
    public long? IconPictureId
    {
        get => _iconPictureId;
        set
        {
            if (_iconPictureId != value)
            {
                _iconPictureId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Die geladene Bildquelle für das Icon der Medienquelle.
    /// </summary>
    public ImageSource? IconImageSource
    {
        get => _iconImageSource;
        set
        {
            if (_iconImageSource != value)
            {
                _iconImageSource = value;
                OnPropertyChanged();
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
