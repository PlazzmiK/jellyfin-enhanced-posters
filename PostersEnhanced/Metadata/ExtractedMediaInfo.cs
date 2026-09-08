using PostersEnhanced.Models;

namespace PostersEnhanced.Metadata;

/// <summary>
/// Extracted media information for overlay rendering.
/// </summary>
/// <param name="Resolution">Detected media resolution.</param>
/// <param name="HdrType">Detected HDR or color range type.</param>
/// <param name="AudioCodec">Detected audio codec.</param>
/// <param name="Rating">Extracted community or IMDb rating score.</param>
public record ExtractedMediaInfo(
    MediaResolution Resolution,
    VideoHdrType HdrType,
    AudioCodecType AudioCodec,
    float? Rating);
