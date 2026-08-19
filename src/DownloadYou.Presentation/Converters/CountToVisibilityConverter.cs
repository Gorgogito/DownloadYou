using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DownloadYou.Presentation.Converters;

/// <summary>ConverterParameter="Invert" muestra el elemento cuando la colección está vacía (0), en vez de cuando tiene elementos.</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasItems = value is int count && count > 0;
        var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
        var visible = invert ? !hasItems : hasItems;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
