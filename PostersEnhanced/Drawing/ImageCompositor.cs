using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using PostersEnhanced.Configuration;
using PostersEnhanced.Metadata;
using PostersEnhanced.Models;
using PostersEnhanced.Themes;
using SkiaSharp;

namespace PostersEnhanced.Drawing;

/// <summary>
/// Composites media badges, HDR icons, and dynamic rating pills over base poster artwork.
/// </summary>
public static class ImageCompositor
{
    /// <summary>
    /// Composites overlays on top of a poster image stream.
    /// </summary>
    /// <param name="posterStream">The source poster stream.</param>
    /// <param name="mediaInfo">The extracted media information.</param>
    /// <param name="configuration">The plugin configuration.</param>
    /// <param name="themeManager">The theme and asset manager.</param>
    /// <returns>A stream containing the composited JPEG image.</returns>
    public static Stream Composite(
        Stream posterStream,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration configuration,
        ThemeAssetManager themeManager)
    {
        ArgumentNullException.ThrowIfNull(posterStream);
        ArgumentNullException.ThrowIfNull(mediaInfo);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(themeManager);

        using var baseBitmap = SKBitmap.Decode(posterStream);
        if (baseBitmap is null)
        {
            throw new InvalidOperationException("Failed to decode poster image stream.");
        }

        var width = baseBitmap.Width;
        var height = baseBitmap.Height;

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;

        // 1. Draw pristine original poster
        canvas.DrawBitmap(baseBitmap, 0, 0);

        // 2. Draw Media Badges (Resolution, Video Range, Audio)
        DrawMediaBadges(canvas, width, height, mediaInfo, configuration, themeManager);

        // 3. Draw Dynamic Rating Pill
        DrawRatingBadge(canvas, width, height, mediaInfo, configuration);

        canvas.Flush();

        // 4. Encode to high-quality JPEG
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 92);

        var outputStream = new MemoryStream();
        data.SaveTo(outputStream);
        outputStream.Position = 0;
        return outputStream;
    }

    private static void DrawMediaBadges(
        SKCanvas canvas,
        int posterWidth,
        int posterHeight,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config,
        ThemeAssetManager themeManager)
    {
        var badgeKeys = ResolveActiveBadgeKeys(mediaInfo, config);
        if (badgeKeys.Count == 0)
        {
            return;
        }

        var badgeHeight = Math.Max(20f, posterHeight * (config.MediaBadgesScalePercent / 100f));
        var loadedBadges = new List<SKBitmap>();

        try
        {
            foreach (var key in badgeKeys)
            {
                var bmp = themeManager.GetBadge(config.Theme, key, badgeHeight);
                if (bmp is not null)
                {
                    loadedBadges.Add(bmp);
                }
            }

            if (loadedBadges.Count == 0)
            {
                return;
            }

            // Calculate bounding box for badges group
            var spacing = config.MediaBadgesSpacing;
            float totalWidth;
            float totalHeight;

            if (config.MediaBadgesDirection == BadgeLayoutDirection.Horizontal)
            {
                totalWidth = 0;
                totalHeight = badgeHeight;
                for (var i = 0; i < loadedBadges.Count; i++)
                {
                    totalWidth += loadedBadges[i].Width;
                    if (i < loadedBadges.Count - 1)
                    {
                        totalWidth += spacing;
                    }
                }
            }
            else
            {
                totalWidth = 0;
                totalHeight = 0;
                for (var i = 0; i < loadedBadges.Count; i++)
                {
                    totalWidth = Math.Max(totalWidth, loadedBadges[i].Width);
                    totalHeight += loadedBadges[i].Height;
                    if (i < loadedBadges.Count - 1)
                    {
                        totalHeight += spacing;
                    }
                }
            }

            var (originX, originY) = CalculateAnchorCoordinates(
                config.MediaBadgesAnchor,
                posterWidth,
                posterHeight,
                totalWidth,
                totalHeight,
                config.MediaBadgesOffsetX,
                config.MediaBadgesOffsetY);

            // Draw each badge
            var currentX = originX;
            var currentY = originY;

            foreach (var badge in loadedBadges)
            {
                canvas.DrawBitmap(badge, currentX, currentY);

                if (config.MediaBadgesDirection == BadgeLayoutDirection.Horizontal)
                {
                    currentX += badge.Width + spacing;
                }
                else
                {
                    currentY += badge.Height + spacing;
                }
            }
        }
        finally
        {
            foreach (var b in loadedBadges)
            {
                b.Dispose();
            }
        }
    }

    private static void DrawRatingBadge(
        SKCanvas canvas,
        int posterWidth,
        int posterHeight,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config)
    {
        if (!config.ShowRatingBadge || !mediaInfo.Rating.HasValue)
        {
            return;
        }

        var score = mediaInfo.Rating.Value;
        var scoreText = score.ToString("0.0", CultureInfo.InvariantCulture);

        var pillHeight = Math.Max(24f, posterHeight * (config.RatingBadgeScalePercent / 100f));
        var fontSize = pillHeight * 0.68f;

        using var textPaint = new SKPaint
        {
            Color = SKColor.TryParse(config.RatingTextColor, out var textColor) ? textColor : SKColors.Black,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var textWidth = textPaint.MeasureText(scoreText);
        var paddingX = pillHeight * (config.RatingPaddingX / 24f);
        var pillWidth = textWidth + (paddingX * 2f);

        var (originX, originY) = CalculateAnchorCoordinates(
            config.RatingBadgeAnchor,
            posterWidth,
            posterHeight,
            pillWidth,
            pillHeight,
            config.RatingBadgeOffsetX,
            config.RatingBadgeOffsetY);

        var pillRect = new SKRect(originX, originY, originX + pillWidth, originY + pillHeight);
        var cornerRadius = pillHeight * (config.RatingCornerRadius / 24f);
        var roundRect = new SKRoundRect(pillRect, cornerRadius);

        // Resolve background color (score-based tiers or fixed)
        var bgColorHex = config.GetRatingBackgroundColor(score);
        var bgColor = SKColor.TryParse(bgColorHex, out var parsedBgColor) ? parsedBgColor : new SKColor(0xF5, 0xC5, 0x18);

        using var bgPaint = new SKPaint
        {
            Color = bgColor,
            IsAntialias = true
        };

        // Draw shadow for contrast
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 0x66),
            IsAntialias = true
        };
        var shadowRect = new SKRoundRect(new SKRect(originX + 1, originY + 2, originX + pillWidth + 1, originY + pillHeight + 2), cornerRadius);
        canvas.DrawRoundRect(shadowRect, shadowPaint);

        // Draw pill body
        canvas.DrawRoundRect(roundRect, bgPaint);

        // Center text within pill
        var textX = originX + paddingX;
        var textY = originY + (pillHeight * 0.74f);
        canvas.DrawText(scoreText, textX, textY, textPaint);
    }

    /// <summary>
    /// Calculates the top-left coordinate for an element of given width and height based on the anchor point and offsets.
    /// </summary>
    /// <param name="anchor">The 9-point grid anchor position.</param>
    /// <param name="posterWidth">The width of the poster in pixels.</param>
    /// <param name="posterHeight">The height of the poster in pixels.</param>
    /// <param name="elementWidth">The width of the element to place in pixels.</param>
    /// <param name="elementHeight">The height of the element to place in pixels.</param>
    /// <param name="offsetX">The horizontal pixel offset from the anchor boundary.</param>
    /// <param name="offsetY">The vertical pixel offset from the anchor boundary.</param>
    /// <returns>A tuple containing the computed X and Y top-left coordinates.</returns>
    public static (float X, float Y) CalculateAnchorCoordinates(
        AnchorPosition anchor,
        int posterWidth,
        int posterHeight,
        float elementWidth,
        float elementHeight,
        int offsetX,
        int offsetY)
    {
        float x = anchor switch
        {
            AnchorPosition.TopLeft or AnchorPosition.CenterLeft or AnchorPosition.BottomLeft => offsetX,
            AnchorPosition.TopCenter or AnchorPosition.Center or AnchorPosition.BottomCenter => ((posterWidth - elementWidth) / 2f) + offsetX,
            AnchorPosition.TopRight or AnchorPosition.CenterRight or AnchorPosition.BottomRight => posterWidth - elementWidth - offsetX,
            _ => offsetX
        };

        float y = anchor switch
        {
            AnchorPosition.TopLeft or AnchorPosition.TopCenter or AnchorPosition.TopRight => offsetY,
            AnchorPosition.CenterLeft or AnchorPosition.Center or AnchorPosition.CenterRight => ((posterHeight - elementHeight) / 2f) + offsetY,
            AnchorPosition.BottomLeft or AnchorPosition.BottomCenter or AnchorPosition.BottomRight => posterHeight - elementHeight - offsetY,
            _ => offsetY
        };

        return (x, y);
    }

    private static List<string> ResolveActiveBadgeKeys(ExtractedMediaInfo info, PluginConfiguration config)
    {
        var keys = new List<string>();

        // Resolution Badges
        if (config.ShowResolutionBadges)
        {
            if (info.Resolution == MediaResolution.Uhd4K && config.Show4K)
            {
                keys.Add("4k");
            }
            else if (info.Resolution == MediaResolution.Fhd1080p && config.Show1080p)
            {
                keys.Add("1080p");
            }
            else if (info.Resolution == MediaResolution.Hd720p && config.Show720p)
            {
                keys.Add("720p");
            }
            else if (info.Resolution == MediaResolution.Sd && config.ShowSD)
            {
                keys.Add("sd");
            }
        }

        // Video Range / HDR Badges
        if (config.ShowVideoRangeBadges)
        {
            if (info.HdrType == VideoHdrType.DolbyVision && config.ShowDolbyVision)
            {
                keys.Add("dv");
            }
            else if (info.HdrType == VideoHdrType.Hdr10Plus && config.ShowHdr10Plus)
            {
                keys.Add("hdr10plus");
            }
            else if (info.HdrType == VideoHdrType.Hdr10 && config.ShowHdr10)
            {
                keys.Add("hdr10");
            }
            else if (info.HdrType == VideoHdrType.Hdr && config.ShowHdr)
            {
                keys.Add("hdr");
            }
            else if (info.HdrType == VideoHdrType.Hlg && config.ShowHlg)
            {
                keys.Add("hlg");
            }
        }

        // Audio Badges
        if (config.ShowAudioBadges)
        {
            if (info.AudioCodec == AudioCodecType.DolbyAtmos && config.ShowDolbyAtmos)
            {
                keys.Add("atmos");
            }
            else if (info.AudioCodec == AudioCodecType.DtsX && config.ShowDtsX)
            {
                keys.Add("dtsx");
            }
            else if (info.AudioCodec == AudioCodecType.TrueHd && config.ShowTrueHd)
            {
                keys.Add("truehd");
            }
            else if (info.AudioCodec == AudioCodecType.DtsHdMa && config.ShowDtsHdMa)
            {
                keys.Add("dtshd");
            }
            else if (info.AudioCodec == AudioCodecType.Flac && config.ShowFlac)
            {
                keys.Add("flac");
            }
        }

        return keys;
    }
}
