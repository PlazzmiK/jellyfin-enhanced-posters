using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using SkiaSharp;

namespace PostersEnhanced.Themes;

/// <summary>
/// Manages badge assets, custom theme folders, and vector badge rendering.
/// </summary>
public class ThemeAssetManager
{
    private static readonly SKColor[] HdrGradientColors =
    [
        new SKColor(0xFF, 0xCC, 0x00),
        new SKColor(0xFF, 0x77, 0x00),
        new SKColor(0xFF, 0x33, 0x00)
    ];

    private static readonly float[] HdrGradientPositions = [0.0f, 0.5f, 1.0f];

    private readonly IApplicationPaths _applicationPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeAssetManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The Jellyfin application paths.</param>
    public ThemeAssetManager(IApplicationPaths applicationPaths)
    {
        _applicationPaths = applicationPaths;
        EnsureThemesDirectory();
    }

    /// <summary>
    /// Gets the base path for Posters Enhanced themes.
    /// </summary>
    public string ThemesDirectory => Path.Combine(_applicationPaths.PluginConfigurationsPath, "PostersEnhanced", "themes");

    /// <summary>
    /// Ensures default directories exist.
    /// </summary>
    public void EnsureThemesDirectory()
    {
        try
        {
            var defaultThemeBadges = Path.Combine(ThemesDirectory, "default", "badges");
            if (!Directory.Exists(defaultThemeBadges))
            {
                Directory.CreateDirectory(defaultThemeBadges);
            }
        }
        catch
        {
            // Ignore directory creation errors if permissions are restricted
        }
    }

    /// <summary>
    /// Loads a badge image for the specified key and theme, or generates a crisp vector fallback if not present on disk.
    /// Supports custom uploaded data URIs, disk files, embedded resources, and dynamic vector rendering.
    /// </summary>
    /// <param name="theme">The theme name.</param>
    /// <param name="badgeKey">The badge key (e.g., "4k", "dv", "hdr", "atmos", "3d", "imax").</param>
    /// <param name="targetHeight">The desired badge height for scaling.</param>
    /// <param name="customBadgeDataUrl">Optional Base64 data URI uploaded by the user.</param>
    /// <param name="transparentBg">Whether to render badge with a transparent background (for use inside unified pill).</param>
    /// <returns>An SKBitmap representing the badge, or null.</returns>
    public SKBitmap? GetBadge(string theme, string badgeKey, float targetHeight, string? customBadgeDataUrl = null, bool transparentBg = false)
    {
        // 1. Check custom uploaded data URI from plugin configuration
        if (!string.IsNullOrWhiteSpace(customBadgeDataUrl))
        {
            try
            {
                var commaIdx = customBadgeDataUrl.IndexOf(',', StringComparison.Ordinal);
                var base64 = commaIdx >= 0 ? customBadgeDataUrl[(commaIdx + 1)..] : customBadgeDataUrl;
                var bytes = Convert.FromBase64String(base64);
                using var ms = new MemoryStream(bytes);
                using var original = SKBitmap.Decode(ms);
                if (original is not null)
                {
                    var scale = targetHeight / original.Height;
                    var targetWidth = (int)Math.Max(1, Math.Round(original.Width * scale));
                    return original.Resize(new SKImageInfo(targetWidth, (int)targetHeight), SKFilterQuality.High);
                }
            }
            catch
            {
                // Fall through on corrupt or invalid base64 data
            }
        }

        // 2. Check theme folder on disk
        var customPath = Path.Combine(ThemesDirectory, theme, "badges", $"{badgeKey}.png");
        if (File.Exists(customPath))
        {
            try
            {
                using var stream = File.OpenRead(customPath);
                using var original = SKBitmap.Decode(stream);
                if (original is not null)
                {
                    var scale = targetHeight / original.Height;
                    var targetWidth = (int)Math.Max(1, Math.Round(original.Width * scale));
                    return original.Resize(new SKImageInfo(targetWidth, (int)targetHeight), SKFilterQuality.High);
                }
            }
            catch
            {
                // Fall back to embedded or vector generator
            }
        }

        // 3. Check embedded assembly resources (e.g. 3D glasses badge)
        try
        {
            var keyLower = badgeKey.ToLowerInvariant();
            var resName = $"PostersEnhanced.Assets.Badges.{keyLower}.png";
            using var resStream = typeof(ThemeAssetManager).Assembly.GetManifestResourceStream(resName);
            if (resStream is not null)
            {
                using var original = SKBitmap.Decode(resStream);
                if (original is not null)
                {
                    var scale = targetHeight / original.Height;
                    var targetWidth = (int)Math.Max(1, Math.Round(original.Width * scale));
                    return original.Resize(new SKImageInfo(targetWidth, (int)targetHeight), SKFilterQuality.High);
                }
            }
        }
        catch
        {
            // Fall back to vector generator
        }

        // 4. Generate vector badge dynamically
        return GenerateVectorBadge(badgeKey, targetHeight, transparentBg);
    }

    /// <summary>
    /// Generates a vector badge with anti-aliasing and styling.
    /// </summary>
    /// <param name="badgeKey">The badge identifier.</param>
    /// <param name="targetHeight">The height of the badge in pixels.</param>
    /// <param name="transparentBg">Whether to omit individual capsule backgrounds (for unified pill).</param>
    /// <returns>A rendered SKBitmap.</returns>
    public static SKBitmap GenerateVectorBadge(string badgeKey, float targetHeight, bool transparentBg = false)
    {
        var height = (int)Math.Max(16, targetHeight);

        return badgeKey.ToLowerInvariant() switch
        {
            "4k" => Render4KBadge(height),
            "1080p" => RenderTextBadge("1080p", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "720p" => RenderTextBadge("720p", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "sd" => RenderTextBadge("SD", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "dv" => RenderDolbyVisionBadge(height, transparentBg),
            "hdr" => RenderHdrBadge(height),
            "hdr10" => RenderTextBadge("HDR10", height, new SKColor(0x38, 0xBD, 0xF8), transparentBg ? null : new SKColor(0x0F, 0x17, 0x2A, 0xCC)),
            "hdr10plus" => RenderTextBadge("HDR10+", height, new SKColor(0x38, 0xBD, 0xF8), transparentBg ? null : new SKColor(0x0F, 0x17, 0x2A, 0xCC)),
            "hlg" => RenderTextBadge("HLG", height, SKColors.White, transparentBg ? null : new SKColor(0x37, 0x41, 0x51, 0xCC)),
            "atmos" => RenderTextBadge("ATMOS", height, SKColors.White, transparentBg ? null : new SKColor(0x11, 0x18, 0x27, 0xCC)),
            "dtsx" => RenderTextBadge("DTS:X", height, new SKColor(0xF9, 0x73, 0x16), transparentBg ? null : new SKColor(0x18, 0x18, 0x1B, 0xCC)),
            "truehd" => RenderTextBadge("TrueHD", height, SKColors.White, transparentBg ? null : new SKColor(0x1E, 0x29, 0x3B, 0xCC)),
            "dtshd" => RenderTextBadge("DTS-HD", height, new SKColor(0xFB, 0x92, 0x3C), transparentBg ? null : new SKColor(0x18, 0x18, 0x1B, 0xCC)),
            "flac" => RenderTextBadge("FLAC", height, SKColors.White, transparentBg ? null : new SKColor(0x18, 0x18, 0x1B, 0xCC)),
            "3d" => Render3DBadge(height),
            "imax" => RenderTextBadge("IMAX", height, new SKColor(0x00, 0xA4, 0xE4), transparentBg ? null : new SKColor(0x00, 0x20, 0x40, 0xCC)),
            "extended" => RenderTextBadge("EXTENDED", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "directorscut" => RenderTextBadge("DIRECTOR'S CUT", height, new SKColor(0xF5, 0xC5, 0x18), transparentBg ? null : new SKColor(0x22, 0x1E, 0x10, 0xCC)),
            "theatrical" => RenderTextBadge("THEATRICAL", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "unrated" => RenderTextBadge("UNRATED", height, new SKColor(0xE2, 0x31, 0x33), transparentBg ? null : new SKColor(0x33, 0x10, 0x10, 0xCC)),
            "specialedition" => RenderTextBadge("SPECIAL EDITION", height, new SKColor(0x5B, 0xC4, 0xF0), transparentBg ? null : new SKColor(0x10, 0x25, 0x35, 0xCC)),
            "remastered" => RenderTextBadge("REMASTERED", height, new SKColor(0xF5, 0xC5, 0x18), transparentBg ? null : new SKColor(0x28, 0x24, 0x10, 0xCC)),
            "finalcut" => RenderTextBadge("FINAL CUT", height, SKColors.White, transparentBg ? null : new SKColor(0x33, 0x33, 0x33, 0xCC)),
            _ => RenderTextBadge(badgeKey.ToUpperInvariant(), height, SKColors.White, transparentBg ? null : new SKColor(0x27, 0x27, 0x2A, 0xCC))
        };
    }

    private static SKBitmap Render4KBadge(int height)
    {
        // 4K clean bold typography with subtle shadow
        var fontSize = height * 0.85f;
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var text = "4K";
        var textWidth = paint.MeasureText(text);
        var width = (int)Math.Ceiling(textWidth + (height * 0.2f));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        // Draw soft drop shadow for readability over any poster background
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 0xAA),
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = paint.Typeface
        };
        canvas.DrawText(text, (height * 0.1f) + 2f, (height * 0.82f) + 2f, shadowPaint);
        canvas.DrawText(text, height * 0.1f, height * 0.82f, paint);

        return bitmap;
    }

    private static SKBitmap RenderDolbyVisionBadge(int height, bool transparentBg = false)
    {
        // Renders Dolby Vision icon: [DO][DV] with iconic blue/cyan styling
        var fontSize = height * 0.78f;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var doWidth = paint.MeasureText("DO");
        var dvWidth = paint.MeasureText("DV");
        var width = (int)Math.Ceiling(doWidth + dvWidth + (height * 0.4f));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var startX = height * 0.15f;

        if (!transparentBg)
        {
            var pillRect = new SKRoundRect(new SKRect(0, 0, width, height), height * 0.18f);
            using var bgPaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x00, 0x00, 0xBB),
                IsAntialias = true
            };
            canvas.DrawRoundRect(pillRect, bgPaint);
        }
        else
        {
            // Subtle shadow when in unified pill
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x00, 0x00, 0xAA),
                IsAntialias = true,
                TextSize = fontSize,
                Typeface = paint.Typeface
            };
            canvas.DrawText("DO", startX + 1.5f, (height * 0.78f) + 1.5f, shadowPaint);
            canvas.DrawText("DV", startX + doWidth + 5.5f, (height * 0.78f) + 1.5f, shadowPaint);
        }

        // DO in Cyan/Blue
        paint.Color = new SKColor(0x38, 0xBD, 0xF8);
        canvas.DrawText("DO", startX, height * 0.78f, paint);

        // DV in Electric Blue
        paint.Color = new SKColor(0x02, 0x84, 0xC7);
        canvas.DrawText("DV", startX + doWidth + 4f, height * 0.78f, paint);

        return bitmap;
    }

    private static SKBitmap RenderTextBadge(string text, int height, SKColor textColor, SKColor? bgColor)
    {
        var fontSize = height * (bgColor.HasValue ? 0.65f : 0.75f);
        using var paint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var textWidth = paint.MeasureText(text);
        var paddingX = bgColor.HasValue ? (height * 0.35f) : (height * 0.12f);
        var width = (int)Math.Ceiling(textWidth + (paddingX * 2));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        if (bgColor.HasValue)
        {
            var rect = new SKRoundRect(new SKRect(0, 0, width, height), height * 0.22f);
            using var bgPaint = new SKPaint
            {
                Color = bgColor.Value,
                IsAntialias = true
            };
            canvas.DrawRoundRect(rect, bgPaint);
        }
        else
        {
            // Drop shadow for legibility inside unified transparent pill
            using var shadowPaint = new SKPaint
            {
                Color = new SKColor(0x00, 0x00, 0x00, 0xAA),
                IsAntialias = true,
                TextSize = fontSize,
                Typeface = paint.Typeface
            };
            canvas.DrawText(text, paddingX + 1.5f, (height * 0.78f) + 1.5f, shadowPaint);
        }

        canvas.DrawText(text, paddingX, height * 0.78f, paint);
        return bitmap;
    }

    private static SKBitmap RenderHdrBadge(int height)
    {
        var fontSize = height * 0.78f;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var text = "HDR";
        var textWidth = paint.MeasureText(text);
        var width = (int)Math.Ceiling(textWidth + (height * 0.15f));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(textWidth, 0),
            HdrGradientColors,
            HdrGradientPositions,
            SKShaderTileMode.Clamp);

        paint.Shader = shader;
        canvas.DrawText(text, height * 0.05f, height * 0.78f, paint);
        return bitmap;
    }

    private static SKBitmap Render3DBadge(int height)
    {
        var fontSize = height * 0.78f;
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var text3DWidth = textPaint.MeasureText("3D");
        var glassesWidth = height * 0.95f;
        var width = (int)Math.Ceiling(glassesWidth + text3DWidth + (height * 0.35f));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var lensY = height * 0.28f;
        var lensH = height * 0.44f;
        var lensW = height * 0.38f;

        using var redPaint = new SKPaint { Color = new SKColor(0xFF, 0x45, 0x00), IsAntialias = true };
        using var bluePaint = new SKPaint { Color = new SKColor(0x00, 0x80, 0xFF), IsAntialias = true };
        using var framePaint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = Math.Max(2f, height * 0.06f) };

        var leftRect = new SKRoundRect(new SKRect(height * 0.05f, lensY, (height * 0.05f) + lensW, lensY + lensH), height * 0.08f);
        var rightRect = new SKRoundRect(new SKRect((height * 0.05f) + lensW + (height * 0.06f), lensY, (height * 0.05f) + (lensW * 2f) + (height * 0.06f), lensY + lensH), height * 0.08f);

        canvas.DrawRoundRect(leftRect, redPaint);
        canvas.DrawRoundRect(rightRect, bluePaint);
        canvas.DrawRoundRect(leftRect, framePaint);
        canvas.DrawRoundRect(rightRect, framePaint);

        canvas.DrawText("3D", glassesWidth + (height * 0.2f), height * 0.78f, textPaint);
        return bitmap;
    }
}
