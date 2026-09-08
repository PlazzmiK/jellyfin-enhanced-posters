using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Model.Plugins;
using PostersEnhanced.Models;

namespace PostersEnhanced.Configuration;

/// <summary>
/// Plugin configuration model for Posters Enhanced.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Theme = "default";
        Source = PosterSource.LocalFirst;
        SaveOriginalBesideMedia = true;

        // Resolution Badges
        ShowResolutionBadges = true;
        Show4K = true;
        Show1080p = false;
        Show720p = true;
        ShowSD = false;

        // Video Range / HDR Badges
        ShowVideoRangeBadges = true;
        ShowDolbyVision = true;
        ShowHdr10Plus = true;
        ShowHdr10 = true;
        ShowHdr = true;
        ShowHlg = false;

        // Audio Badges
        ShowAudioBadges = false;
        ShowDolbyAtmos = true;
        ShowTrueHd = false;
        ShowDtsX = true;
        ShowDtsHdMa = false;
        ShowFlac = false;

        // Media Badges Layout
        MediaBadgesAnchor = AnchorPosition.BottomLeft;
        MediaBadgesOffsetX = 24;
        MediaBadgesOffsetY = 24;
        MediaBadgesScalePercent = 4.5f;
        MediaBadgesSpacing = 8;
        MediaBadgesDirection = BadgeLayoutDirection.Horizontal;

        // Rating Badge Layout
        ShowRatingBadge = true;
        RatingBadgeAnchor = AnchorPosition.BottomRight;
        RatingBadgeOffsetX = 24;
        RatingBadgeOffsetY = 24;
        RatingBadgeScalePercent = 4.5f;
        RatingColorMode = RatingColorMode.DynamicTiers;

        // Rating Tiers
        RatingTierRedColor = "#E53935";
        RatingTierOrangeColor = "#FB8C00";
        RatingTierYellowColor = "#F5C518";
        RatingTierGreenColor = "#43A047";
        RatingTierBlueColor = "#00BCD4";
        RatingFixedColor = "#F5C518";
        RatingTextColor = "#000000";

        RatingCornerRadius = 8f;
        RatingPaddingX = 12f;
        RatingPaddingY = 6f;
    }

    /// <summary>Gets or sets the theme name.</summary>
    public string Theme { get; set; }

    /// <summary>Gets or sets the poster source.</summary>
    public PosterSource Source { get; set; }

    /// <summary>Gets or sets a value indicating whether to save pristine originals next to media files.</summary>
    public bool SaveOriginalBesideMedia { get; set; }

    /// <summary>Gets or sets a value indicating whether resolution badges are enabled.</summary>
    public bool ShowResolutionBadges { get; set; }

    /// <summary>Gets or sets a value indicating whether 4K badges are enabled.</summary>
    public bool Show4K { get; set; }

    /// <summary>Gets or sets a value indicating whether 1080p badges are enabled.</summary>
    public bool Show1080p { get; set; }

    /// <summary>Gets or sets a value indicating whether 720p badges are enabled.</summary>
    public bool Show720p { get; set; }

    /// <summary>Gets or sets a value indicating whether SD badges are enabled.</summary>
    public bool ShowSD { get; set; }

    /// <summary>Gets or sets a value indicating whether video range badges are enabled.</summary>
    public bool ShowVideoRangeBadges { get; set; }

    /// <summary>Gets or sets a value indicating whether Dolby Vision badges are enabled.</summary>
    public bool ShowDolbyVision { get; set; }

    /// <summary>Gets or sets a value indicating whether HDR10+ badges are enabled.</summary>
    public bool ShowHdr10Plus { get; set; }

    /// <summary>Gets or sets a value indicating whether HDR10 badges are enabled.</summary>
    public bool ShowHdr10 { get; set; }

    /// <summary>Gets or sets a value indicating whether generic HDR badges are enabled.</summary>
    public bool ShowHdr { get; set; }

    /// <summary>Gets or sets a value indicating whether HLG badges are enabled.</summary>
    public bool ShowHlg { get; set; }

    /// <summary>Gets or sets a value indicating whether audio badges are enabled.</summary>
    public bool ShowAudioBadges { get; set; }

    /// <summary>Gets or sets a value indicating whether Dolby Atmos badges are enabled.</summary>
    public bool ShowDolbyAtmos { get; set; }

    /// <summary>Gets or sets a value indicating whether TrueHD badges are enabled.</summary>
    public bool ShowTrueHd { get; set; }

    /// <summary>Gets or sets a value indicating whether DTS:X badges are enabled.</summary>
    public bool ShowDtsX { get; set; }

    /// <summary>Gets or sets a value indicating whether DTS-HD MA badges are enabled.</summary>
    public bool ShowDtsHdMa { get; set; }

    /// <summary>Gets or sets a value indicating whether FLAC badges are enabled.</summary>
    public bool ShowFlac { get; set; }

    /// <summary>Gets or sets the anchor position for media badges.</summary>
    public AnchorPosition MediaBadgesAnchor { get; set; }

    /// <summary>Gets or sets the X offset for media badges in pixels.</summary>
    public int MediaBadgesOffsetX { get; set; }

    /// <summary>Gets or sets the Y offset for media badges in pixels.</summary>
    public int MediaBadgesOffsetY { get; set; }

    /// <summary>Gets or sets the scale percentage of media badges relative to poster height.</summary>
    public float MediaBadgesScalePercent { get; set; }

    /// <summary>Gets or sets the pixel spacing between media badges.</summary>
    public int MediaBadgesSpacing { get; set; }

    /// <summary>Gets or sets the layout direction for media badges.</summary>
    public BadgeLayoutDirection MediaBadgesDirection { get; set; }

    /// <summary>Gets or sets a value indicating whether the rating badge is enabled.</summary>
    public bool ShowRatingBadge { get; set; }

    /// <summary>Gets or sets the anchor position for the rating badge.</summary>
    public AnchorPosition RatingBadgeAnchor { get; set; }

    /// <summary>Gets or sets the X offset for the rating badge in pixels.</summary>
    public int RatingBadgeOffsetX { get; set; }

    /// <summary>Gets or sets the Y offset for the rating badge in pixels.</summary>
    public int RatingBadgeOffsetY { get; set; }

    /// <summary>Gets or sets the scale percentage of the rating badge relative to poster height.</summary>
    public float RatingBadgeScalePercent { get; set; }

    /// <summary>Gets or sets the rating color mode.</summary>
    public RatingColorMode RatingColorMode { get; set; }

    /// <summary>Gets or sets the color for ratings below 5.0.</summary>
    public string RatingTierRedColor { get; set; }

    /// <summary>Gets or sets the color for ratings from 5.0 to 5.9.</summary>
    public string RatingTierOrangeColor { get; set; }

    /// <summary>Gets or sets the color for ratings from 6.0 to 6.9.</summary>
    public string RatingTierYellowColor { get; set; }

    /// <summary>Gets or sets the color for ratings from 7.0 to 7.9.</summary>
    public string RatingTierGreenColor { get; set; }

    /// <summary>Gets or sets the color for ratings 8.0 and above.</summary>
    public string RatingTierBlueColor { get; set; }

    /// <summary>Gets or sets the fixed color for ratings when using FixedColor mode.</summary>
    public string RatingFixedColor { get; set; }

    /// <summary>Gets or sets the text color for ratings.</summary>
    public string RatingTextColor { get; set; }

    /// <summary>Gets or sets the corner radius for the rating pill.</summary>
    public float RatingCornerRadius { get; set; }

    /// <summary>Gets or sets the horizontal padding for the rating pill.</summary>
    public float RatingPaddingX { get; set; }

    /// <summary>Gets or sets the vertical padding for the rating pill.</summary>
    public float RatingPaddingY { get; set; }

    /// <summary>
    /// Gets the background color for a given score based on the configuration.
    /// </summary>
    /// <param name="score">The rating score.</param>
    /// <returns>A hex color string.</returns>
    public string GetRatingBackgroundColor(float score)
    {
        if (RatingColorMode == RatingColorMode.FixedColor)
        {
            return RatingFixedColor;
        }

        if (score < 5.0f)
        {
            return RatingTierRedColor;
        }

        if (score < 6.0f)
        {
            return RatingTierOrangeColor;
        }

        if (score < 7.0f)
        {
            return RatingTierYellowColor;
        }

        if (score < 8.0f)
        {
            return RatingTierGreenColor;
        }

        return RatingTierBlueColor;
    }

    /// <summary>
    /// Computes a hash of the visual configuration settings to detect when re-rendering is needed.
    /// </summary>
    /// <returns>A hexadecimal hash string.</returns>
    public string ComputeConfigHash()
    {
        var sb = new StringBuilder();
        sb.Append(Theme).Append('|');
        sb.Append(ShowResolutionBadges).Append('|');
        sb.Append(Show4K).Append('|');
        sb.Append(Show1080p).Append('|');
        sb.Append(Show720p).Append('|');
        sb.Append(ShowSD).Append('|');
        sb.Append(ShowVideoRangeBadges).Append('|');
        sb.Append(ShowDolbyVision).Append('|');
        sb.Append(ShowHdr10Plus).Append('|');
        sb.Append(ShowHdr10).Append('|');
        sb.Append(ShowHdr).Append('|');
        sb.Append(ShowHlg).Append('|');
        sb.Append(ShowAudioBadges).Append('|');
        sb.Append(ShowDolbyAtmos).Append('|');
        sb.Append(ShowTrueHd).Append('|');
        sb.Append(ShowDtsX).Append('|');
        sb.Append(ShowDtsHdMa).Append('|');
        sb.Append(ShowFlac).Append('|');
        sb.Append((int)MediaBadgesAnchor).Append('|');
        sb.Append(MediaBadgesOffsetX).Append('|');
        sb.Append(MediaBadgesOffsetY).Append('|');
        sb.Append(MediaBadgesScalePercent.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(MediaBadgesSpacing).Append('|');
        sb.Append((int)MediaBadgesDirection).Append('|');
        sb.Append(ShowRatingBadge).Append('|');
        sb.Append((int)RatingBadgeAnchor).Append('|');
        sb.Append(RatingBadgeOffsetX).Append('|');
        sb.Append(RatingBadgeOffsetY).Append('|');
        sb.Append(RatingBadgeScalePercent.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append((int)RatingColorMode).Append('|');
        sb.Append(RatingTierRedColor).Append('|');
        sb.Append(RatingTierOrangeColor).Append('|');
        sb.Append(RatingTierYellowColor).Append('|');
        sb.Append(RatingTierGreenColor).Append('|');
        sb.Append(RatingTierBlueColor).Append('|');
        sb.Append(RatingFixedColor).Append('|');
        sb.Append(RatingTextColor).Append('|');
        sb.Append(RatingCornerRadius.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(RatingPaddingX.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(RatingPaddingY.ToString("F2", CultureInfo.InvariantCulture));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
