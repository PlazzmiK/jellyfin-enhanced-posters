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

        var baseBitmap = SKBitmap.Decode(posterStream);
        if (baseBitmap is null)
        {
            throw new InvalidOperationException("Failed to decode poster image stream.");
        }

        if (configuration.AutoCropToPortraitRatio)
        {
            baseBitmap = CropToPortraitRatio(baseBitmap);
        }

        using (baseBitmap)
        {
            var width = baseBitmap.Width;
            var height = baseBitmap.Height;

            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            var canvas = surface.Canvas;

            // 1. Draw pristine original poster (guaranteed 2:3 aspect ratio if auto-crop is enabled)
            canvas.DrawBitmap(baseBitmap, 0, 0);

            // 2. Draw Badges (Media, Edition, 3D) grouped by AnchorPosition
            DrawAllBadges(canvas, width, height, mediaInfo, configuration, themeManager);

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
    }

    private static void DrawAllBadges(
        SKCanvas canvas,
        int posterWidth,
        int posterHeight,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config,
        ThemeAssetManager themeManager)
    {
        // Group badges by their target anchor position
        var anchorGroups = new Dictionary<AnchorPosition, (int OffsetX, int OffsetY, float ScalePercent, List<string> Keys)>();

        void AddBadgeKey(AnchorPosition anchor, int offsetX, int offsetY, float scalePercent, string key)
        {
            if (!anchorGroups.TryGetValue(anchor, out var group))
            {
                group = (offsetX, offsetY, scalePercent, new List<string>());
                anchorGroups[anchor] = group;
            }

            if (!group.Keys.Contains(key))
            {
                group.Keys.Add(key);
            }
        }

        // 1. Media Badges (Resolution, Video Range, Audio)
        var mediaKeys = ResolveActiveMediaBadgeKeys(mediaInfo, config);
        foreach (var key in mediaKeys)
        {
            AddBadgeKey(config.MediaBadgesAnchor, config.MediaBadgesOffsetX, config.MediaBadgesOffsetY, config.MediaBadgesScalePercent, key);
        }

        // 2. 3D Badge
        if (config.Show3DBadge && mediaInfo.Is3D)
        {
            AddBadgeKey(config.ThreeDBadgeAnchor, config.ThreeDOffsetX, config.ThreeDOffsetY, config.ThreeDScalePercent, "3d");
        }

        // 3. Edition Badges
        var editionKey = ResolveActiveEditionBadgeKey(mediaInfo, config);
        if (!string.IsNullOrEmpty(editionKey))
        {
            AddBadgeKey(config.EditionBadgesAnchor, config.EditionBadgesOffsetX, config.EditionBadgesOffsetY, config.EditionBadgesScalePercent, editionKey);
        }

        // 4. Render each anchor group
        foreach (var (anchor, (offsetX, offsetY, scalePercent, keys)) in anchorGroups)
        {
            if (keys.Count == 0)
            {
                continue;
            }

            var totalHeight = Math.Max(24f, posterHeight * (scalePercent / 100f));
            var badgeHeight = config.CombineBadgesInPill ? Math.Max(16f, totalHeight - (config.PillPaddingY * 2f)) : totalHeight;

            var loadedBadges = new List<SKBitmap>();
            try
            {
                foreach (var key in keys)
                {
                    var customData = config.GetCustomBadge(key);
                    var bmp = themeManager.GetBadge(config.Theme, key, badgeHeight, customData, transparentBg: config.CombineBadgesInPill);
                    if (bmp is not null)
                    {
                        loadedBadges.Add(bmp);
                    }
                }

                if (loadedBadges.Count == 0)
                {
                    continue;
                }

                // Resolve effective offsets: fallback to offsetY if offsetX <= 0, and scale proportionally to poster resolution
                var effOffsetX = offsetX > 0 ? offsetX : (offsetY > 0 ? offsetY : 24);
                var effOffsetY = offsetY > 0 ? offsetY : 24;

                var scaleX = Math.Max(1f, posterWidth / 1000f);
                var scaleY = Math.Max(1f, posterHeight / 1500f);
                var scaledOffsetX = (int)Math.Round(effOffsetX * scaleX);
                var scaledOffsetY = (int)Math.Round(effOffsetY * scaleY);

                if (config.CombineBadgesInPill)
                {
                    RenderCombinedPill(canvas, posterWidth, posterHeight, anchor, scaledOffsetX, scaledOffsetY, totalHeight, loadedBadges, config);
                }
                else
                {
                    RenderSeparateBadges(canvas, posterWidth, posterHeight, anchor, scaledOffsetX, scaledOffsetY, badgeHeight, loadedBadges, config);
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
    }

    private static void RenderCombinedPill(
        SKCanvas canvas,
        int posterWidth,
        int posterHeight,
        AnchorPosition anchor,
        int offsetX,
        int offsetY,
        float containerHeight,
        List<SKBitmap> badges,
        PluginConfiguration config)
    {
        var itemSpacing = config.PillItemSpacing;
        float totalContentWidth = 0;
        for (var i = 0; i < badges.Count; i++)
        {
            totalContentWidth += badges[i].Width;
            if (i < badges.Count - 1)
            {
                totalContentWidth += itemSpacing;
            }
        }

        var pillWidth = totalContentWidth + (config.PillPaddingX * 2f);
        var pillHeight = containerHeight;

        var (originX, originY) = CalculateAnchorCoordinates(
            anchor,
            posterWidth,
            posterHeight,
            pillWidth,
            pillHeight,
            offsetX,
            offsetY);

        var pillRect = new SKRect(originX, originY, originX + pillWidth, originY + pillHeight);
        var cornerRadius = config.PillCornerRadius;
        var roundRect = new SKRoundRect(pillRect, cornerRadius);

        // Parse pill background color and opacity
        var baseColor = SKColor.TryParse(config.PillBackgroundColor, out var parsed) ? parsed : SKColors.Black;
        var alpha = (byte)Math.Clamp((int)(config.PillBackgroundOpacity * 255f), 0, 255);
        var pillBgColor = new SKColor(baseColor.Red, baseColor.Green, baseColor.Blue, alpha);

        // Drop shadow for contrast
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 0x77),
            IsAntialias = true
        };
        var shadowRect = new SKRoundRect(new SKRect(originX + 1f, originY + 2f, originX + pillWidth + 1f, originY + pillHeight + 2f), cornerRadius);
        canvas.DrawRoundRect(shadowRect, shadowPaint);

        // Draw translucent pill body
        using var bgPaint = new SKPaint
        {
            Color = pillBgColor,
            IsAntialias = true
        };
        canvas.DrawRoundRect(roundRect, bgPaint);

        // Draw each badge centered vertically inside the container pill
        var curX = originX + config.PillPaddingX;
        foreach (var badge in badges)
        {
            var curY = originY + ((pillHeight - badge.Height) / 2f);
            canvas.DrawBitmap(badge, curX, curY);
            curX += badge.Width + itemSpacing;
        }
    }

    private static void RenderSeparateBadges(
        SKCanvas canvas,
        int posterWidth,
        int posterHeight,
        AnchorPosition anchor,
        int offsetX,
        int offsetY,
        float badgeHeight,
        List<SKBitmap> badges,
        PluginConfiguration config)
    {
        var spacing = config.MediaBadgesSpacing;
        float totalWidth;
        float totalHeight;

        if (config.MediaBadgesDirection == BadgeLayoutDirection.Horizontal)
        {
            totalWidth = 0;
            totalHeight = badgeHeight;
            for (var i = 0; i < badges.Count; i++)
            {
                totalWidth += badges[i].Width;
                if (i < badges.Count - 1)
                {
                    totalWidth += spacing;
                }
            }
        }
        else
        {
            totalWidth = 0;
            totalHeight = 0;
            for (var i = 0; i < badges.Count; i++)
            {
                totalWidth = Math.Max(totalWidth, badges[i].Width);
                totalHeight += badges[i].Height;
                if (i < badges.Count - 1)
                {
                    totalHeight += spacing;
                }
            }
        }

        var (originX, originY) = CalculateAnchorCoordinates(
            anchor,
            posterWidth,
            posterHeight,
            totalWidth,
            totalHeight,
            offsetX,
            offsetY);

        var currentX = originX;
        var currentY = originY;

        foreach (var badge in badges)
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
        var fontSize = pillHeight * 0.70f;

        using var textPaint = new SKPaint
        {
            Color = SKColor.TryParse(config.RatingTextColor, out var textColor) ? textColor : SKColors.Black,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var textWidth = textPaint.MeasureText(scoreText);
        var paddingX = config.RatingPaddingX;
        var pillWidth = textWidth + (paddingX * 2f);

        var effOffsetX = config.RatingBadgeOffsetX > 0 ? config.RatingBadgeOffsetX : (config.RatingBadgeOffsetY > 0 ? config.RatingBadgeOffsetY : 24);
        var effOffsetY = config.RatingBadgeOffsetY > 0 ? config.RatingBadgeOffsetY : 24;

        var scaleX = Math.Max(1f, posterWidth / 1000f);
        var scaleY = Math.Max(1f, posterHeight / 1500f);
        var scaledOffsetX = (int)Math.Round(effOffsetX * scaleX);
        var scaledOffsetY = (int)Math.Round(effOffsetY * scaleY);

        var (originX, originY) = CalculateAnchorCoordinates(
            config.RatingBadgeAnchor,
            posterWidth,
            posterHeight,
            pillWidth,
            pillHeight,
            scaledOffsetX,
            scaledOffsetY);

        var pillRect = new SKRect(originX, originY, originX + pillWidth, originY + pillHeight);
        var cornerRadius = config.RatingCornerRadius;
        var roundRect = new SKRoundRect(pillRect, cornerRadius);

        // Resolve background color (score-based tiers or fixed)
        var bgColorHex = config.GetRatingBackgroundColor(score);
        var bgColor = SKColor.TryParse(bgColorHex, out var parsedBgColor) ? parsedBgColor : new SKColor(0xF5, 0xC5, 0x18);

        using var bgPaint = new SKPaint
        {
            Color = bgColor,
            IsAntialias = true
        };

        // Draw soft drop shadow for contrast
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 0x66),
            IsAntialias = true
        };
        var shadowRect = new SKRoundRect(new SKRect(originX + 1f, originY + 2f, originX + pillWidth + 1f, originY + pillHeight + 2f), cornerRadius);
        canvas.DrawRoundRect(shadowRect, shadowPaint);

        // Draw pill body
        canvas.DrawRoundRect(roundRect, bgPaint);

        // Center text horizontally and vertically within pill
        SKRect textBounds = default;
        textPaint.MeasureText(scoreText, ref textBounds);
        var textX = originX + ((pillWidth - textWidth) / 2f);
        var textY = originY + ((pillHeight - textBounds.Height) / 2f) - textBounds.Top;
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

    /// <summary>
    /// Crops a bitmap to the standard 2:3 (1:1.5) portrait aspect ratio expected by Jellyfin cards, if needed.
    /// Trims excess width or height symmetrically from the center.
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <returns>A 2:3 aspect ratio bitmap. If cropping occurs, the original bitmap is disposed and the new cropped bitmap is returned.</returns>
    public static SKBitmap CropToPortraitRatio(SKBitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        const double targetRatio = 2.0 / 3.0;
        var currentRatio = (double)source.Width / source.Height;

        // If already within 0.5% of 2:3 aspect ratio, no crop needed
        if (Math.Abs(currentRatio - targetRatio) < 0.005)
        {
            return source;
        }

        int cropX = 0;
        int cropY = 0;
        int targetWidth = source.Width;
        int targetHeight = source.Height;

        if (currentRatio > targetRatio)
        {
            // Wider than 2:3 (e.g. 513x748 or 1000x1400) - crop left and right equally
            targetWidth = Math.Min(source.Width, Math.Max(1, (int)Math.Round(source.Height * targetRatio)));
            cropX = Math.Max(0, (source.Width - targetWidth) / 2);
            if (cropX + targetWidth > source.Width)
            {
                targetWidth = source.Width - cropX;
            }
        }
        else
        {
            // Taller than 2:3 (e.g. 1000x1600) - crop top and bottom equally
            targetHeight = Math.Min(source.Height, Math.Max(1, (int)Math.Round(source.Width / targetRatio)));
            cropY = Math.Max(0, (source.Height - targetHeight) / 2);
            if (cropY + targetHeight > source.Height)
            {
                targetHeight = source.Height - cropY;
            }
        }

        var croppedBitmap = new SKBitmap(targetWidth, targetHeight, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(croppedBitmap);
        var srcRect = new SKRectI(cropX, cropY, cropX + targetWidth, cropY + targetHeight);
        var dstRect = new SKRect(0, 0, targetWidth, targetHeight);
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(source, srcRect, dstRect, paint);
        canvas.Flush();

        source.Dispose();
        return croppedBitmap;
    }

    private static List<string> ResolveActiveMediaBadgeKeys(ExtractedMediaInfo info, PluginConfiguration config)
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

    private static string? ResolveActiveEditionBadgeKey(ExtractedMediaInfo info, PluginConfiguration config)
    {
        if (!config.ShowEditionBadges || info.Edition == EditionType.None)
        {
            return null;
        }

        return info.Edition switch
        {
            EditionType.Imax when config.ShowImax => "imax",
            EditionType.Extended when config.ShowExtended => "extended",
            EditionType.DirectorsCut when config.ShowDirectorsCut => "directorscut",
            EditionType.Theatrical when config.ShowTheatrical => "theatrical",
            EditionType.Unrated when config.ShowUnrated => "unrated",
            EditionType.SpecialEdition when config.ShowSpecialEdition => "specialedition",
            EditionType.Remastered when config.ShowRemastered => "remastered",
            EditionType.FinalCut => "finalcut",
            EditionType.Custom => string.IsNullOrEmpty(info.CustomEditionName) ? null : info.CustomEditionName.ToLowerInvariant(),
            _ => null
        };
    }
}
