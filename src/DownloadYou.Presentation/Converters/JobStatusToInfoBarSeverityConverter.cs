using System.Globalization;
using System.Windows.Data;
using DownloadYou.Domain.Enums;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Converters;

public sealed class JobStatusToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        JobStatus.Completed => InfoBarSeverity.Success,
        JobStatus.Failed => InfoBarSeverity.Error,
        JobStatus.Canceled => InfoBarSeverity.Warning,
        _ => InfoBarSeverity.Informational
    };

    public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
