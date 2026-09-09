using EnhancedPosters.Models;

namespace EnhancedPosters.Metadata;

/// <summary>
/// Extracted media information for overlay rendering.
/// </summary>
/// <param name="Resolution">Detected media resolution.</param>
/// <param name="HdrType">Detected HDR or color range type.</param>
/// <param name="AudioCodec">Detected audio codec.</param>
/// <param name="Rating">Extracted community or IMDb rating score.</param>
/// <param name="Edition">Detected edition type.</param>
/// <param name="CustomEditionName">Custom edition label if parsed from tags.</param>
/// <param name="Is3D">Value indicating whether media is in 3D format.</param>
/// <param name="HasHdrFallback">Value indicating whether Dolby Vision media has an HDR fallback layer.</param>
/// <param name="HasHdr10PlusFallback">Value indicating whether Dolby Vision media has an HDR10+ fallback layer.</param>
/// <param name="HasTrueHdWithAtmos">Value indicating whether Dolby Atmos audio is delivered via Dolby TrueHD.</param>
public record ExtractedMediaInfo(
    MediaResolution Resolution,
    VideoHdrType HdrType,
    AudioCodecType AudioCodec,
    float? Rating,
    EditionType Edition = EditionType.None,
    string? CustomEditionName = null,
    bool Is3D = false,
    bool HasHdrFallback = false,
    bool HasHdr10PlusFallback = false,
    bool HasTrueHdWithAtmos = false);
