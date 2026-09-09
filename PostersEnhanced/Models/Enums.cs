namespace PostersEnhanced.Models;

/// <summary>
/// 9-point grid anchor positions for overlays.
/// </summary>
public enum AnchorPosition
{
    /// <summary>Top-Left anchor.</summary>
    TopLeft,

    /// <summary>Top-Center anchor.</summary>
    TopCenter,

    /// <summary>Top-Right anchor.</summary>
    TopRight,

    /// <summary>Center-Left anchor.</summary>
    CenterLeft,

    /// <summary>Center anchor.</summary>
    Center,

    /// <summary>Center-Right anchor.</summary>
    CenterRight,

    /// <summary>Bottom-Left anchor.</summary>
    BottomLeft,

    /// <summary>Bottom-Center anchor.</summary>
    BottomCenter,

    /// <summary>Bottom-Right anchor.</summary>
    BottomRight
}

/// <summary>
/// Coloring modes for rating badges.
/// </summary>
public enum RatingColorMode
{
    /// <summary>
    /// Dynamic color tiers based on rating score:
    /// &lt; 5.0: Red, 5.0–5.9: Orange, 6.0–6.9: Yellow, 7.0–7.9: Green, &gt;= 8.0: Blue / Diamond.
    /// </summary>
    DynamicTiers,

    /// <summary>
    /// Single fixed color for all ratings.
    /// </summary>
    FixedColor
}

/// <summary>
/// Source preference for rating scores.
/// </summary>
public enum RatingSourcePreference
{
    /// <summary>Community rating (IMDb / TMDb).</summary>
    Community,

    /// <summary>Critic rating (Rotten Tomatoes / Metacritic).</summary>
    Critic,

    /// <summary>Combined average of all available ratings.</summary>
    CombinedAverage
}

/// <summary>
/// Source options for base poster artwork.
/// </summary>
public enum PosterSource
{
    /// <summary>Use existing local poster from Jellyfin library.</summary>
    LocalFirst,

    /// <summary>Only use local poster; skip if not present.</summary>
    LocalOnly,

    /// <summary>Fetch clean textless poster from btttr.cc if IMDb ID is available.</summary>
    BtttrCcClean
}

/// <summary>
/// Orientation for stacking multiple badges.
/// </summary>
public enum BadgeLayoutDirection
{
    /// <summary>Place badges side by side horizontally.</summary>
    Horizontal,

    /// <summary>Stack badges on top of each other vertically.</summary>
    Vertical
}

/// <summary>
/// Detected media resolution.
/// </summary>
public enum MediaResolution
{
    /// <summary>Unknown or unspecified resolution.</summary>
    None,

    /// <summary>4K Ultra HD (3840x2160 or higher).</summary>
    Uhd4K,

    /// <summary>Full HD 1080p (1920x1080).</summary>
    Fhd1080p,

    /// <summary>HD 720p (1280x720).</summary>
    Hd720p,

    /// <summary>Standard Definition.</summary>
    Sd
}

/// <summary>
/// Detected video HDR / color range type.
/// </summary>
public enum VideoHdrType
{
    /// <summary>Standard dynamic range.</summary>
    None,

    /// <summary>Dolby Vision (with or without HDR fallback).</summary>
    DolbyVision,

    /// <summary>HDR10+ (dynamic metadata).</summary>
    Hdr10Plus,

    /// <summary>HDR10.</summary>
    Hdr10,

    /// <summary>Generic HDR.</summary>
    Hdr,

    /// <summary>Hybrid Log-Gamma.</summary>
    Hlg
}

/// <summary>
/// Detected audio codec or technology.
/// </summary>
public enum AudioCodecType
{
    /// <summary>None or unselected.</summary>
    None,

    /// <summary>Dolby Atmos.</summary>
    DolbyAtmos,

    /// <summary>Dolby TrueHD.</summary>
    TrueHd,

    /// <summary>DTS:X.</summary>
    DtsX,

    /// <summary>DTS-HD Master Audio.</summary>
    DtsHdMa,

    /// <summary>Standard DTS.</summary>
    Dts,

    /// <summary>Dolby Digital Plus (E-AC-3).</summary>
    Eac3,

    /// <summary>Dolby Digital (AC-3).</summary>
    Ac3,

    /// <summary>Free Lossless Audio Codec.</summary>
    Flac,

    /// <summary>Advanced Audio Coding.</summary>
    Aac
}

/// <summary>
/// Detected movie/show edition or cut type.
/// </summary>
public enum EditionType
{
    /// <summary>Standard / unspecified edition.</summary>
    None,

    /// <summary>IMAX or IMAX Enhanced edition.</summary>
    Imax,

    /// <summary>Extended edition or cut.</summary>
    Extended,

    /// <summary>Director's Cut.</summary>
    DirectorsCut,

    /// <summary>Theatrical version.</summary>
    Theatrical,

    /// <summary>Unrated cut.</summary>
    Unrated,

    /// <summary>Special edition.</summary>
    SpecialEdition,

    /// <summary>Remastered edition.</summary>
    Remastered,

    /// <summary>Final Cut.</summary>
    FinalCut,

    /// <summary>Custom or other edition parsed from tags.</summary>
    Custom
}
