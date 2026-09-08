using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        AutoCropToPortraitRatio = true;

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

        // Rating Tiers (Matched to audience score colors)
        RatingTierRedColor = "#E23133";
        RatingTierOrangeColor = "#EF7C2A";
        RatingTierYellowColor = "#F5C518";
        RatingTierGreenColor = "#5CB85C";
        RatingTierBlueColor = "#5BC4F0";
        RatingFixedColor = "#F5C518";
        RatingTextColor = "#000000";

        RatingCornerRadius = 6f;
        RatingPaddingX = 8f;
        RatingPaddingY = 4f;

        // Edition Badges
        ShowEditionBadges = true;
        ShowImax = true;
        ShowExtended = true;
        ShowDirectorsCut = true;
        ShowTheatrical = true;
        ShowUnrated = true;
        ShowSpecialEdition = true;
        ShowRemastered = true;
        EditionBadgesAnchor = AnchorPosition.TopRight;
        EditionBadgesOffsetX = 24;
        EditionBadgesOffsetY = 24;
        EditionBadgesScalePercent = 4.5f;

        // 3D Badge
        Show3DBadge = true;
        ThreeDBadgeAnchor = AnchorPosition.TopLeft;
        ThreeDOffsetX = 24;
        ThreeDOffsetY = 24;
        ThreeDScalePercent = 4.5f;

        // Combined Dark Transparent Pill Styling
        CombineBadgesInPill = true;
        PillBackgroundColor = "#000000";
        PillBackgroundOpacity = 0.78f;
        PillCornerRadius = 8f;
        PillPaddingX = 10f;
        PillPaddingY = 4f;
        PillItemSpacing = 8f;
    }

    /// <summary>Gets or sets the theme name.</summary>
    public string Theme { get; set; }

    /// <summary>Gets or sets the poster source.</summary>
    public PosterSource Source { get; set; }

    /// <summary>Gets or sets a value indicating whether to save pristine originals next to media files.</summary>
    public bool SaveOriginalBesideMedia { get; set; }

    /// <summary>Gets or sets a value indicating whether posters are automatically cropped to the standard 2:3 (1:1.5) portrait aspect ratio before badges are added.</summary>
    public bool AutoCropToPortraitRatio { get; set; }

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

    /// <summary>Gets or sets a value indicating whether edition badges are enabled.</summary>
    public bool ShowEditionBadges { get; set; }

    /// <summary>Gets or sets a value indicating whether IMAX badges are enabled.</summary>
    public bool ShowImax { get; set; }

    /// <summary>Gets or sets a value indicating whether Extended cut badges are enabled.</summary>
    public bool ShowExtended { get; set; }

    /// <summary>Gets or sets a value indicating whether Director's Cut badges are enabled.</summary>
    public bool ShowDirectorsCut { get; set; }

    /// <summary>Gets or sets a value indicating whether Theatrical cut badges are enabled.</summary>
    public bool ShowTheatrical { get; set; }

    /// <summary>Gets or sets a value indicating whether Unrated badges are enabled.</summary>
    public bool ShowUnrated { get; set; }

    /// <summary>Gets or sets a value indicating whether Special Edition badges are enabled.</summary>
    public bool ShowSpecialEdition { get; set; }

    /// <summary>Gets or sets a value indicating whether Remastered badges are enabled.</summary>
    public bool ShowRemastered { get; set; }

    /// <summary>Gets or sets the anchor position for edition badges.</summary>
    public AnchorPosition EditionBadgesAnchor { get; set; }

    /// <summary>Gets or sets horizontal offset for edition badges.</summary>
    public int EditionBadgesOffsetX { get; set; }

    /// <summary>Gets or sets vertical offset for edition badges.</summary>
    public int EditionBadgesOffsetY { get; set; }

    /// <summary>Gets or sets the scale percentage for edition badges.</summary>
    public float EditionBadgesScalePercent { get; set; }

    /// <summary>Gets or sets a value indicating whether 3D badge is enabled.</summary>
    public bool Show3DBadge { get; set; }

    /// <summary>Gets or sets the anchor position for 3D badge.</summary>
    public AnchorPosition ThreeDBadgeAnchor { get; set; }

    /// <summary>Gets or sets horizontal offset for 3D badge.</summary>
    public int ThreeDOffsetX { get; set; }

    /// <summary>Gets or sets vertical offset for 3D badge.</summary>
    public int ThreeDOffsetY { get; set; }

    /// <summary>Gets or sets the scale percentage for 3D badge.</summary>
    public float ThreeDScalePercent { get; set; }

    /// <summary>Gets or sets a value indicating whether to combine co-located badges in a single dark translucent pill.</summary>
    public bool CombineBadgesInPill { get; set; }

    /// <summary>Gets or sets the background color of the combined pill.</summary>
    public string PillBackgroundColor { get; set; }

    /// <summary>Gets or sets the background opacity (0.0 to 1.0) of the combined pill.</summary>
    public float PillBackgroundOpacity { get; set; }

    /// <summary>Gets or sets the corner radius of the combined pill.</summary>
    public float PillCornerRadius { get; set; }

    /// <summary>Gets or sets the horizontal padding inside the combined pill.</summary>
    public float PillPaddingX { get; set; }

    /// <summary>Gets or sets the vertical padding inside the combined pill.</summary>
    public float PillPaddingY { get; set; }

    /// <summary>Gets or sets the item spacing inside the combined pill.</summary>
    public float PillItemSpacing { get; set; }

    /// <summary>Gets custom user-uploaded badge image entries (Key and Base64 Data URL).</summary>
    public Collection<CustomBadgeEntry> CustomBadges { get; } = new Collection<CustomBadgeEntry>();

    /// <summary>
    /// Gets a custom badge data URI by key, or null if not found.
    /// </summary>
    /// <param name="key">The badge key.</param>
    /// <returns>Base64 data URI string or null.</returns>
    public string? GetCustomBadge(string key)
    {
        if (CustomBadges is null || CustomBadges.Count == 0 || string.IsNullOrEmpty(key))
        {
            return null;
        }

        foreach (var entry in CustomBadges)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Data;
            }
        }

        return null;
    }

    /// <summary>
    /// Sets or updates a custom badge data URI by key.
    /// </summary>
    /// <param name="key">The badge key.</param>
    /// <param name="data">The base64 data URI.</param>
    public void SetCustomBadge(string key, string data)
    {
        for (var i = 0; i < CustomBadges.Count; i++)
        {
            if (string.Equals(CustomBadges[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(data))
                {
                    CustomBadges.RemoveAt(i);
                }
                else
                {
                    CustomBadges[i].Data = data;
                }

                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(data))
        {
            CustomBadges.Add(new CustomBadgeEntry { Key = key, Data = data });
        }
    }

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
        sb.Append(AutoCropToPortraitRatio).Append('|');
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

        // Edition
        sb.Append(ShowEditionBadges).Append('|');
        sb.Append(ShowImax).Append('|');
        sb.Append(ShowExtended).Append('|');
        sb.Append(ShowDirectorsCut).Append('|');
        sb.Append(ShowTheatrical).Append('|');
        sb.Append(ShowUnrated).Append('|');
        sb.Append(ShowSpecialEdition).Append('|');
        sb.Append(ShowRemastered).Append('|');
        sb.Append((int)EditionBadgesAnchor).Append('|');
        sb.Append(EditionBadgesOffsetX).Append('|');
        sb.Append(EditionBadgesOffsetY).Append('|');
        sb.Append(EditionBadgesScalePercent.ToString("F2", CultureInfo.InvariantCulture)).Append('|');

        // 3D
        sb.Append(Show3DBadge).Append('|');
        sb.Append((int)ThreeDBadgeAnchor).Append('|');
        sb.Append(ThreeDOffsetX).Append('|');
        sb.Append(ThreeDOffsetY).Append('|');
        sb.Append(ThreeDScalePercent.ToString("F2", CultureInfo.InvariantCulture)).Append('|');

        // Pill Styling
        sb.Append(CombineBadgesInPill).Append('|');
        sb.Append(PillBackgroundColor).Append('|');
        sb.Append(PillBackgroundOpacity.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(PillCornerRadius.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(PillPaddingX.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(PillPaddingY.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
        sb.Append(PillItemSpacing.ToString("F2", CultureInfo.InvariantCulture)).Append('|');

        // Rating
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
        sb.Append(RatingPaddingY.ToString("F2", CultureInfo.InvariantCulture)).Append('|');

        // Custom badges
        if (CustomBadges is not null)
        {
            foreach (var b in CustomBadges)
            {
                sb.Append(b.Key).Append(':').Append(b.Data.Length).Append('|');
            }
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
