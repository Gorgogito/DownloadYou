using DownloadYou.Presentation.Converters;
using DownloadYou.Presentation.Models;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Tests.Converters;

public class PipelineStepStateToSymbolConverterTests
{
    private readonly PipelineStepStateToSymbolConverter _converter = new();

    [Theory]
    [InlineData(PipelineStepState.Done, SymbolRegular.CheckmarkCircle24)]
    [InlineData(PipelineStepState.Current, SymbolRegular.Circle24)]
    [InlineData(PipelineStepState.Error, SymbolRegular.ErrorCircle24)]
    [InlineData(PipelineStepState.Pending, SymbolRegular.Circle24)]
    public void Convert_MapsEachStateToExpectedSymbol(PipelineStepState state, SymbolRegular expected) =>
        Assert.Equal(expected, _converter.Convert(state, typeof(SymbolRegular), null!, null!));
}
