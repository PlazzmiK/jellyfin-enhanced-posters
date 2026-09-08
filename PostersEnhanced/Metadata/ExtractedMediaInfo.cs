using PostersEnhanced.Models;

namespace PostersEnhanced.Metadata;

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
public record ExtractedMediaInfo(
    MediaResolution Resolution,
    VideoHdrType HdrType,
    AudioCodecType AudioCodec,
    float? Rating,
    EditionType Edition = EditionType.None,
    string? CustomEditionName = null,
    bool Is3D = false);
