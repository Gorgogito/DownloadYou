using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Converters;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Tests.Converters;

public class EnumToAppearanceConverterTests
{
    private readonly EnumToAppearanceConverter _converter = new();

    [Fact]
    public void Convert_ReturnsPrimary_WhenValueMatchesParameter() =>
        Assert.Equal(ControlAppearance.Primary, _converter.Convert(ThemePreference.Dark, typeof(ControlAppearance), "Dark", null!));

    [Fact]
    public void Convert_IsCaseInsensitive() =>
        Assert.Equal(ControlAppearance.Primary, _converter.Convert(ThemePreference.Dark, typeof(ControlAppearance), "dark", null!));

    [Fact]
    public void Convert_ReturnsSecondary_WhenValueDoesNotMatchParameter() =>
        Assert.Equal(ControlAppearance.Secondary, _converter.Convert(ThemePreference.Light, typeof(ControlAppearance), "Dark", null!));

    [Fact]
    public void Convert_WorksForIntValues_ViaToStringComparison() =>
        Assert.Equal(ControlAppearance.Primary, _converter.Convert(192, typeof(ControlAppearance), "192", null!));
}
