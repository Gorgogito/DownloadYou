using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Converters;

/// <summary>ConverterParameter es el nombre (string) del valor de enum que representa el botón. Sirve para cualquier enum (segmented buttons genéricos).</summary>
public sealed class EnumToAppearanceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter as string, StringComparison.OrdinalIgnoreCase)
            ? ControlAppearance.Primary
            : ControlAppearance.Secondary;

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
