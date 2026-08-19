using System.Globalization;
using System.Windows.Data;
using DownloadYou.Presentation.Models;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Converters;

public sealed class PipelineStepStateToSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        PipelineStepState.Done => SymbolRegular.CheckmarkCircle24,
        PipelineStepState.Current => SymbolRegular.Circle24,
        PipelineStepState.Error => SymbolRegular.ErrorCircle24,
        _ => SymbolRegular.Circle24
    };

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
