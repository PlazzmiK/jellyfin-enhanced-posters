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
    /// </summary>
    /// <param name="theme">The theme name.</param>
    /// <param name="badgeKey">The badge key (e.g., "4k", "dv", "hdr", "atmos").</param>
    /// <param name="targetHeight">The desired badge height for scaling.</param>
    /// <returns>An SKBitmap representing the badge, or null.</returns>
    public SKBitmap? GetBadge(string theme, string badgeKey, float targetHeight)
    {
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
                // Fall back to vector generator
            }
        }

        // Generate vector badge dynamically
        return GenerateVectorBadge(badgeKey, targetHeight);
    }

    /// <summary>
    /// Generates a vector badge with anti-aliasing and styling.
    /// </summary>
    /// <param name="badgeKey">The badge identifier.</param>
    /// <param name="targetHeight">The height of the badge in pixels.</param>
    /// <returns>A rendered SKBitmap.</returns>
    public static SKBitmap GenerateVectorBadge(string badgeKey, float targetHeight)
    {
        var height = (int)Math.Max(16, targetHeight);

        return badgeKey.ToLowerInvariant() switch
        {
            "4k" => Render4KBadge(height),
            "1080p" => RenderTextBadge("1080p", height, SKColors.White, new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "720p" => RenderTextBadge("720p", height, SKColors.White, new SKColor(0x33, 0x33, 0x33, 0xCC)),
            "dv" => RenderDolbyVisionBadge(height),
            "hdr" => RenderTextBadge("HDR", height, new SKColor(0xFA, 0xCA, 0x16), new SKColor(0x1F, 0x29, 0x37, 0xCC)),
            "hdr10" => RenderTextBadge("HDR10", height, new SKColor(0x38, 0xBD, 0xF8), new SKColor(0x0F, 0x17, 0x2A, 0xCC)),
            "hdr10plus" => RenderTextBadge("HDR10+", height, new SKColor(0x38, 0xBD, 0xF8), new SKColor(0x0F, 0x17, 0x2A, 0xCC)),
            "hlg" => RenderTextBadge("HLG", height, SKColors.White, new SKColor(0x37, 0x41, 0x51, 0xCC)),
            "atmos" => RenderTextBadge("ATMOS", height, SKColors.White, new SKColor(0x11, 0x18, 0x27, 0xCC)),
            "dtsx" => RenderTextBadge("DTS:X", height, new SKColor(0xF9, 0x73, 0x16), new SKColor(0x18, 0x18, 0x1B, 0xCC)),
            "truehd" => RenderTextBadge("TrueHD", height, SKColors.White, new SKColor(0x1E, 0x29, 0x3B, 0xCC)),
            "dtshd" => RenderTextBadge("DTS-HD", height, new SKColor(0xFB, 0x92, 0x3C), new SKColor(0x18, 0x18, 0x1B, 0xCC)),
            _ => RenderTextBadge(badgeKey.ToUpperInvariant(), height, SKColors.White, new SKColor(0x27, 0x27, 0x2A, 0xCC))
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

    private static SKBitmap RenderDolbyVisionBadge(int height)
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

        var pillRect = new SKRoundRect(new SKRect(0, 0, width, height), height * 0.18f);
        using var bgPaint = new SKPaint
        {
            Color = new SKColor(0x00, 0x00, 0x00, 0xBB),
            IsAntialias = true
        };
        canvas.DrawRoundRect(pillRect, bgPaint);

        // DO in Cyan/Blue
        paint.Color = new SKColor(0x38, 0xBD, 0xF8);
        canvas.DrawText("DO", height * 0.15f, height * 0.78f, paint);

        // DV in Electric Blue
        paint.Color = new SKColor(0x02, 0x84, 0xC7);
        canvas.DrawText("DV", (height * 0.15f) + doWidth + 4f, height * 0.78f, paint);

        return bitmap;
    }

    private static SKBitmap RenderTextBadge(string text, int height, SKColor textColor, SKColor bgColor)
    {
        var fontSize = height * 0.65f;
        using var paint = new SKPaint
        {
            Color = textColor,
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        };

        var textWidth = paint.MeasureText(text);
        var paddingX = height * 0.35f;
        var width = (int)Math.Ceiling(textWidth + (paddingX * 2));

        var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var rect = new SKRoundRect(new SKRect(0, 0, width, height), height * 0.22f);
        using var bgPaint = new SKPaint
        {
            Color = bgColor,
            IsAntialias = true
        };
        canvas.DrawRoundRect(rect, bgPaint);

        canvas.DrawText(text, paddingX, height * 0.73f, paint);
        return bitmap;
    }
}
