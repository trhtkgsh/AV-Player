using Player.Core.Logic.Theming;
using Player.Core.Models;

namespace Player.Core.Tests.Theming;

public sealed class ThemeBrightnessTests
{
    [Theory]
    [InlineData(ThemeId.DarkOled)]
    [InlineData(ThemeId.DarkSlate)]
    [InlineData(ThemeId.HighContrast)]
    public void IsDark_DarkTheme_ReturnsTrue(ThemeId id)
    {
        Assert.True(ThemeBrightness.IsDark(id));
    }

    [Fact]
    public void IsDark_LightTheme_ReturnsFalse()
    {
        Assert.False(ThemeBrightness.IsDark(ThemeId.Light));
    }

    [Fact]
    public void IsDark_EveryThemeId_AnswersWithoutThrowing()
    {
        foreach (var id in Enum.GetValues<ThemeId>())
        {
            var exception = Record.Exception(() => ThemeBrightness.IsDark(id));

            Assert.Null(exception);
        }
    }
}
