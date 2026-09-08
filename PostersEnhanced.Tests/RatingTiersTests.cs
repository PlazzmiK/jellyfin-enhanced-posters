using PostersEnhanced.Configuration;
using PostersEnhanced.Models;
using Xunit;

namespace PostersEnhanced.Tests;

public class RatingTiersTests
{
    [Theory]
    [InlineData(4.2f, "#E53935")] // < 5.0 (Red)
    [InlineData(4.9f, "#E53935")] // < 5.0 (Red)
    [InlineData(5.0f, "#FB8C00")] // 5.0 - 5.9 (Orange)
    [InlineData(5.8f, "#FB8C00")] // 5.0 - 5.9 (Orange)
    [InlineData(6.0f, "#F5C518")] // 6.0 - 6.9 (Yellow)
    [InlineData(6.3f, "#F5C518")] // 6.0 - 6.9 (Yellow, matches attachment 6.3)
    [InlineData(6.9f, "#F5C518")] // 6.0 - 6.9 (Yellow)
    [InlineData(7.0f, "#43A047")] // 7.0 - 7.9 (Green)
    [InlineData(7.8f, "#43A047")] // 7.0 - 7.9 (Green)
    [InlineData(8.0f, "#00BCD4")] // >= 8.0 (Blue / Diamond)
    [InlineData(9.2f, "#00BCD4")] // >= 8.0 (Blue / Diamond)
    public void GetRatingBackgroundColor_DynamicTiers_ReturnsExpectedColor(float score, string expectedColor)
    {
        var config = new PluginConfiguration
        {
            RatingColorMode = RatingColorMode.DynamicTiers
        };

        var actualColor = config.GetRatingBackgroundColor(score);
        Assert.Equal(expectedColor, actualColor);
    }

    [Fact]
    public void GetRatingBackgroundColor_FixedColor_ReturnsFixed()
    {
        var config = new PluginConfiguration
        {
            RatingColorMode = RatingColorMode.FixedColor,
            RatingFixedColor = "#FF00FF"
        };

        Assert.Equal("#FF00FF", config.GetRatingBackgroundColor(6.3f));
        Assert.Equal("#FF00FF", config.GetRatingBackgroundColor(9.0f));
    }
}
