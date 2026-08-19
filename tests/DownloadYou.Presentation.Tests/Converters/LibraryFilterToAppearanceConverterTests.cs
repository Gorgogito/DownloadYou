using DownloadYou.Presentation.Converters;
using DownloadYou.Presentation.Models;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Tests.Converters;

public class LibraryFilterToAppearanceConverterTests
{
    private readonly LibraryFilterToAppearanceConverter _converter = new();

    [Fact]
    public void Convert_ReturnsPrimary_WhenFilterMatchesParameter() =>
        Assert.Equal(ControlAppearance.Primary, _converter.Convert(LibraryFilter.Favorites, typeof(ControlAppearance), "Favorites", null!));

    [Fact]
    public void Convert_ReturnsSecondary_WhenFilterDoesNotMatch() =>
        Assert.Equal(ControlAppearance.Secondary, _converter.Convert(LibraryFilter.Recent, typeof(ControlAppearance), "Favorites", null!));

    [Fact]
    public void Convert_ReturnsSecondary_WhenValueIsNotALibraryFilter() =>
        Assert.Equal(ControlAppearance.Secondary, _converter.Convert("not-a-filter", typeof(ControlAppearance), "Favorites", null!));
}
