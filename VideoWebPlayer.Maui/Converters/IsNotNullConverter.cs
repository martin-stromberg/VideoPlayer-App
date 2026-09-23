using System.Globalization;

namespace VideoWebPlayer.Maui.Converters;

/// <summary>
/// Konvertiert einen Wert in <c>true</c>, wenn er nicht <c>null</c> ist.
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Rückkonvertierung ist nicht sinnvoll — der Konverter ist ausschließlich für OneWay-Bindings gedacht.
        return Binding.DoNothing;
    }
}
