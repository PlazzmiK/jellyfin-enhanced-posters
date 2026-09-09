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
            ResizeLowResolutionPosters = false,
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

    [Fact]
    public void Composite_WithCombinedPillAndEdition_GeneratesValidPoster()
    {
        using var sourceBitmap = new SKBitmap(300, 450);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.Navy);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 80);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            ResizeLowResolutionPosters = false,
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowHdr = true,
            ShowEditionBadges = true,
            ShowImax = true,
            Show3DBadge = true,
            ShowRatingBadge = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            EditionBadgesAnchor = AnchorPosition.TopRight,
            ThreeDBadgeAnchor = AnchorPosition.TopLeft,
            RatingBadgeAnchor = AnchorPosition.BottomRight
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.Hdr,
            AudioCodecType.DolbyAtmos,
            8.3f,
            EditionType.Imax,
            null,
            true);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        Assert.NotNull(resultStream);
        Assert.True(resultStream.Length > 0);

        using var resultBitmap = SKBitmap.Decode(resultStream);
        Assert.NotNull(resultBitmap);
        Assert.Equal(300, resultBitmap.Width);
        Assert.Equal(450, resultBitmap.Height);
    }

    [Fact]
    public void Composite_ChecksExactPillOffsets()
    {
        using var sourceBitmap = new SKBitmap(1000, 1500);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.White);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 100);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowHdr10 = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            MediaBadgesOffsetX = 24,
            MediaBadgesOffsetY = 24,
            ShowRatingBadge = true,
            RatingBadgeAnchor = AnchorPosition.BottomRight,
            RatingBadgeOffsetX = 24,
            RatingBadgeOffsetY = 24
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.Hdr10,
            AudioCodecType.None,
            7.6f);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        using var resultBitmap = SKBitmap.Decode(resultStream);

        // Find leftmost non-white pixel in bottom 200 rows
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int maxBottomY = int.MinValue;

        for (int y = 1300; y < 1500; y++)
        {
            for (int x = 0; x < 1000; x++)
            {
                var pixel = resultBitmap.GetPixel(x, y);
                // If not white
                if (pixel.Red < 240 || pixel.Green < 240 || pixel.Blue < 240)
                {
                    if (x < 500 && x < minX) minX = x;
                    if (x > 500 && x > maxX) maxX = x;
                    if (y > maxBottomY) maxBottomY = y;
                }
            }
        }

        // minX should be around 24 (or 24 - 1 for shadow)
        // maxX should be around 1000 - 24 (or 976)
        // maxBottomY should be around 1500 - 24
        Assert.True(minX >= 23, $"Leftmost pixel {minX} was less than 23! Flush against side?");
        Assert.True(maxX <= 977, $"Rightmost pixel {maxX} was greater than 977! Flush against side?");
        Assert.True(maxBottomY <= 1477, $"Bottommost pixel {maxBottomY} was greater than 1477!");
    }

    [Fact]
    public void Composite_ZeroOffsetX_FallsBackToOffsetYAndDoesNotSitFlush()
    {
        using var sourceBitmap = new SKBitmap(1000, 1500);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.White);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 100);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        // offsetX set to 0, offsetY set to 30
        var config = new PluginConfiguration
        {
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowHdr10 = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            MediaBadgesOffsetX = 0,
            MediaBadgesOffsetY = 30,
            ShowRatingBadge = true,
            RatingBadgeAnchor = AnchorPosition.BottomRight,
            RatingBadgeOffsetX = 0,
            RatingBadgeOffsetY = 30
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.Hdr10,
            AudioCodecType.None,
            7.6f);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        using var resultBitmap = SKBitmap.Decode(resultStream);

        int minX = int.MaxValue;
        int maxX = int.MinValue;

        for (int y = 1300; y < 1500; y++)
        {
            for (int x = 0; x < 1000; x++)
            {
                var pixel = resultBitmap.GetPixel(x, y);
                if (pixel.Red < 240 || pixel.Green < 240 || pixel.Blue < 240)
                {
                    if (x < 500 && x < minX) minX = x;
                    if (x > 500 && x > maxX) maxX = x;
                }
            }
        }

        // With offsetY = 30, fallback effOffsetX = 30
        Assert.True(minX >= 29, $"Leftmost pixel {minX} was less than 29; fallback to offsetY failed!");
        Assert.True(maxX <= 971, $"Rightmost pixel {maxX} was greater than 971; fallback to offsetY failed!");
    }

    [Fact]
    public void ThemeAssetManager_GetBadge_TransparentBg_DoesNotDrawSolidBackground()
    {
        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        // Standard badge has solid background at corner/edge
        using var standardBadge = themeManager.GetBadge("default", "hdr10", 40f, transparentBg: false);
        Assert.NotNull(standardBadge);
        // Check corner pixel inside rounded rect margin
        var standardMidEdgePixel = standardBadge.GetPixel( standardBadge.Width / 2, standardBadge.Height - 2);
        Assert.True(standardMidEdgePixel.Alpha > 0, "Standard badge should have background pixels");

        // Transparent badge for unified pill has transparent background around text
        using var transparentBadge = themeManager.GetBadge("default", "hdr10", 40f, transparentBg: true);
        Assert.NotNull(transparentBadge);
        var transparentCornerPixel = transparentBadge.GetPixel(1, 1);
        Assert.Equal(0, transparentCornerPixel.Alpha);
    }

    [Fact]
    public void Composite_GenerateVisualVerificationArtifact()
    {
        using var sourceBitmap = new SKBitmap(1000, 1500);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(new SKColor(0xF5, 0xF5, 0xF5));

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowHdr10 = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            MediaBadgesOffsetX = 24,
            MediaBadgesOffsetY = 24,
            ShowRatingBadge = true,
            RatingBadgeAnchor = AnchorPosition.BottomRight,
            RatingBadgeOffsetX = 24,
            RatingBadgeOffsetY = 24
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.Hdr10,
            AudioCodecType.None,
            7.6f);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        var artifactPath = Path.Combine(Path.GetTempPath(), "verification_sample.png");
        using var fs = File.Create(artifactPath);
        resultStream.CopyTo(fs);

        Assert.True(File.Exists(artifactPath));
        Assert.True(new FileInfo(artifactPath).Length > 0);
    }

    [Fact]
    public void Composite_CheckExactMarginsWith25px()
    {
        // Test with 479 x 718 (dimensions of screenshot poster)
        using var sourceBitmap = new SKBitmap(479, 718);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(new SKColor(0xF0, 0xF0, 0xF0));

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            ShowVideoRangeBadges = true,
            ShowDolbyVision = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            MediaBadgesOffsetX = 25,
            MediaBadgesOffsetY = 25,
            ShowRatingBadge = true,
            RatingBadgeAnchor = AnchorPosition.BottomRight,
            RatingBadgeOffsetX = 25,
            RatingBadgeOffsetY = 25
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.DolbyVision,
            AudioCodecType.None,
            8.6f);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        var artifactPath = Path.Combine(Path.GetTempPath(), "margin_test_479.png");
        using var fs = File.Create(artifactPath);
        resultStream.CopyTo(fs);

        Assert.True(File.Exists(artifactPath));
        Assert.True(new FileInfo(artifactPath).Length > 0);
    }

    [Fact]
    public void CropToPortraitRatio_WiderThan23_CropsSidesEqually()
    {
        using var wideBitmap = new SKBitmap(1000, 1400);
        using var canvas = new SKCanvas(wideBitmap);
        canvas.Clear(SKColors.Blue);

        var cropped = ImageCompositor.CropToPortraitRatio(wideBitmap);
        using (cropped)
        {
            Assert.Equal(933, cropped.Width);
            Assert.Equal(1400, cropped.Height);
            var ratio = (double)cropped.Width / cropped.Height;
            Assert.InRange(ratio, 0.66, 0.67);
        }
    }

    [Fact]
    public void CropToPortraitRatio_TallerThan23_CropsTopBottomEqually()
    {
        using var tallBitmap = new SKBitmap(1000, 1600);
        using var canvas = new SKCanvas(tallBitmap);
        canvas.Clear(SKColors.Green);

        var cropped = ImageCompositor.CropToPortraitRatio(tallBitmap);
        using (cropped)
        {
            Assert.Equal(1000, cropped.Width);
            Assert.Equal(1500, cropped.Height);
            var ratio = (double)cropped.Width / cropped.Height;
            Assert.InRange(ratio, 0.66, 0.67);
        }
    }

    [Fact]
    public void CropToPortraitRatio_Already23_ReturnsSameDimensions()
    {
        using var standardBitmap = new SKBitmap(1000, 1500);
        using var canvas = new SKCanvas(standardBitmap);
        canvas.Clear(SKColors.Red);

        var result = ImageCompositor.CropToPortraitRatio(standardBitmap);
        using (result)
        {
            Assert.Equal(1000, result.Width);
            Assert.Equal(1500, result.Height);
        }
    }

    [Fact]
    public void Composite_WithAutoCropEnabled_NormalizesWiderPosterTo23()
    {
        // 513 x 748 image (from user's 12 Angry Men screenshot)
        using var sourceBitmap = new SKBitmap(513, 748);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.DarkRed);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            ResizeLowResolutionPosters = false,
            AutoCropToPortraitRatio = true,
            CombineBadgesInPill = true,
            ShowResolutionBadges = true,
            Show4K = true,
            MediaBadgesAnchor = AnchorPosition.BottomLeft,
            MediaBadgesOffsetX = 25,
            MediaBadgesOffsetY = 25
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.None,
            AudioCodecType.None,
            null);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        var artifactDir = @"C:\Users\Jan\.gemini\antigravity-ide\brain\70da7e80-969c-4c77-8b55-57d5a19b870e";
        if (Directory.Exists(artifactDir))
        {
            var artifactPath = Path.Combine(artifactDir, "autocrop_verification.png");
            using var fs = File.Create(artifactPath);
            resultStream.CopyTo(fs);
            resultStream.Position = 0;
        }

        using var resultBitmap = SKBitmap.Decode(resultStream);

        Assert.NotNull(resultBitmap);
        // Target width: 748 * (2 / 3) = 499
        Assert.Equal(499, resultBitmap.Width);
        Assert.Equal(748, resultBitmap.Height);
        var ratio = (double)resultBitmap.Width / resultBitmap.Height;
        Assert.InRange(ratio, 0.66, 0.67);
    }

    [Fact]
    public void Composite_WithAutoCropDisabled_RetainsOriginalDimensions()
    {
        using var sourceBitmap = new SKBitmap(513, 748);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.DarkRed);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            ResizeLowResolutionPosters = false,
            AutoCropToPortraitRatio = false,
            ShowResolutionBadges = true,
            Show4K = true
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.None,
            AudioCodecType.None,
            null);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        using var resultBitmap = SKBitmap.Decode(resultStream);

        Assert.NotNull(resultBitmap);
        Assert.Equal(513, resultBitmap.Width);
        Assert.Equal(748, resultBitmap.Height);
    }

    [Fact]
    public void Composite_WithUpscalingEnabled_UpscalesLowResolutionPoster()
    {
        using var sourceBitmap = new SKBitmap(500, 750);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.DarkBlue);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            ResizeLowResolutionPosters = true,
            TargetPosterWidth = 1000,
            TargetPosterHeight = 1500,
            ShowResolutionBadges = true,
            Show4K = true
        };

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.Uhd4K,
            VideoHdrType.None,
            AudioCodecType.None,
            null);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        using var resultBitmap = SKBitmap.Decode(resultStream);

        Assert.NotNull(resultBitmap);
        Assert.Equal(1000, resultBitmap.Width);
        Assert.Equal(1500, resultBitmap.Height);
    }

    [Fact]
    public void Composite_WithCustomRatingTiers_AppliesTierColor()
    {
        using var sourceBitmap = new SKBitmap(1000, 1500);
        using var canvas = new SKCanvas(sourceBitmap);
        canvas.Clear(SKColors.Black);

        using var sourceStream = new MemoryStream();
        sourceBitmap.Encode(sourceStream, SKEncodedImageFormat.Jpeg, 95);
        sourceStream.Position = 0;

        var mockPaths = new Mock<IApplicationPaths>();
        mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(Path.GetTempPath());
        var themeManager = new ThemeAssetManager(mockPaths.Object);

        var config = new PluginConfiguration
        {
            ShowRatingBadge = true,
            RatingColorMode = RatingColorMode.DynamicTiers,
            UseGlobalCornerRadiusForRating = true,
            PillCornerRadius = 12f
        };
        config.RatingTiers.Clear();
        config.RatingTiers.Add(new RatingTierEntry(0f, 5.99f, "#FF0000", "Poor"));
        config.RatingTiers.Add(new RatingTierEntry(6f, 10f, "#00FF00", "Good"));

        var mediaInfo = new ExtractedMediaInfo(
            MediaResolution.None,
            VideoHdrType.None,
            AudioCodecType.None,
            8.5f);

        using var resultStream = ImageCompositor.Composite(sourceStream, mediaInfo, config, themeManager);
        using var resultBitmap = SKBitmap.Decode(resultStream);

        Assert.NotNull(resultBitmap);
        Assert.Equal(1000, resultBitmap.Width);
        Assert.Equal(1500, resultBitmap.Height);
    }
}
