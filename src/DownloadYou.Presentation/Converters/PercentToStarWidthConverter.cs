using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DownloadYou.Presentation.Converters;

/// <summary>
/// Convierte un porcentaje 0-100 en un GridLength en unidades "estrella", para dibujar
/// una barra de progreso propia con dos columnas (llena/vacía) sin medir píxeles a mano.
/// ConverterParameter="Remaining" devuelve la porción complementaria (100 - valor).
/// </summary>
public sealed class PercentToStarWidthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        var percent = value is double d ? Math.Clamp(d, 0, 100) : 0;
        var isRemaining = string.Equals(parameter as string, "Remaining", StringComparison.OrdinalIgnoreCase);
        var stars = isRemaining ? 100 - percent : percent;

        // Un GridLength en 0 estrellas colapsa la columna; usamos un mínimo ínfimo para
        // que la barra siga siendo válida en 0% y 100% sin división por cero en el layout.
        return new GridLength(Math.Max(stars, 0.001), GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
