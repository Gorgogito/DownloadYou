using System.Globalization;
using System.Windows.Data;
using DownloadYou.Presentation.Formatting;

namespace DownloadYou.Presentation.Converters;

/// <summary>Muestra el nombre legible de un código de idioma (FormatOption.Language); vacío para streams sin audio propio.</summary>
public sealed class LanguageDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is string code && !string.IsNullOrWhiteSpace(code) ? LanguageNames.Resolve(code) : string.Empty;

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
