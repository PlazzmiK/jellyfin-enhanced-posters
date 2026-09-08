using System.IO;
using MediaBrowser.Common.Configuration;
using Moq;
using PostersEnhanced.Configuration;
using PostersEnhanced.Drawing;
using PostersEnhanced.Metadata;
using PostersEnhanced.Models;
using PostersEnhanced.Themes;
using SkiaSharp;
using Xunit;

namespace PostersEnhanced.Tests;

public class ImageCompositorTests
{
    [Theory]
    [InlineData(AnchorPosition.TopLeft, 20, 20, 20, 20)]
    [InlineData(AnchorPosition.TopCenter, 10, 15, 460, 15)]
    [InlineData(AnchorPosition.TopRight, 20, 20, 880, 20)]
    [InlineData(AnchorPosition.BottomLeft, 24, 24, 24, 1426)]
    [InlineData(AnchorPosition.BottomRight, 24, 24, 876, 1426)]
    public void CalculateAnchorCoordinates_ReturnsCorrectCoordinates(
        AnchorPosition anchor,
        int offsetX,
        int offsetY,
        float expectedX,
        float expectedY)
    {
        // Poster: 1000 x 1500, Element: 100 x 50
        var (x, y) = ImageCompositor.CalculateAnchorCoordinates(
            anchor,
            1000,
            1500,
            100,
            50,
            offsetX,
            offsetY);

        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }

    [Fact]
    public void Composite_GeneratesValidJpegStream()
    {
        // 1. Create a dummy base poster bitmap (300 x 450)
        using var sourceBitmap = new SKBitmap(300, 450);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.DarkRed);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 80);
        sourceStream.Position = 0;

        // 2. Setup mock application paths for theme manager
        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        // 3. Setup configuration (matching user's screenshot: 4K + DV at BL, 6.3 Rating at BR)
        var config = new PluginConfiguration
        {
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowDolbyVision = true,
            ShowRatingBadge = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            RatingBadgeAnchor = AnchorPosition.BottomRight
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.DolbyVision,
            AudioCodecType.None,
            6.3f);

        // 4. Composite image
        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);

        Assert.NotNull(resultStream);
        Assert.True(resultStream.Length > 0);

        // 5. Verify the output is a readable SKBitmap of the same dimensions
        using var resultBitmap = SKBitmap.Decode(resultStream);
        Assert.NotNull(resultBitmap);
        Assert.Equal(300, resultBitmap.Width);
        Assert.Equal(450, resultBitmap.Height);
    }
}
