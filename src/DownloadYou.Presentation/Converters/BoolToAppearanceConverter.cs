using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Converters;

/// <summary>Botón "seleccionado" (true) se resalta como Primary; el resto queda Secondary.</summary>
public sealed class BoolToAppearanceConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? ControlAppearance.Primary : ControlAppearance.Secondary;

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
