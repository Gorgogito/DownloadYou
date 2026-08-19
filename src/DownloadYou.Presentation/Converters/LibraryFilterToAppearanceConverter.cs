using System.Globalization;
using System.Windows.Data;
using DownloadYou.Presentation.Models;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Converters;

/// <summary>ConverterParameter es el nombre (string) del LibraryFilter que representa el botón.</summary>
public sealed class LibraryFilterToAppearanceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var current = value is LibraryFilter filter ? filter.ToString() : null;
        return string.Equals(current, parameter as string, StringComparison.OrdinalIgnoreCase)
            ? ControlAppearance.Primary
            : ControlAppearance.Secondary;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
