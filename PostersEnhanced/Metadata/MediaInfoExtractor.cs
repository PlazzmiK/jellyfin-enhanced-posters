using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using PostersEnhanced.Models;

namespace PostersEnhanced.Metadata;

/// <summary>
/// Extracts media information (resolution, video range, audio codec, rating) from Jellyfin items.
/// </summary>
public static class MediaInfoExtractor
{
    /// <summary>
    /// Extracts media and rating information from a Jellyfin BaseItem.
    /// </summary>
    /// <param name="item">The Jellyfin library item.</param>
    /// <returns>The extracted media information.</returns>
    public static ExtractedMediaInfo Extract(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var resolution = MediaResolution.None;
        var hdrType = VideoHdrType.None;
        var audioCodec = AudioCodecType.None;

        var mediaStreams = item.GetMediaStreams();
        if (mediaStreams is not null && mediaStreams.Count > 0)
        {
            var videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
            if (videoStream is not null)
            {
                resolution = DetectResolution(videoStream);
                hdrType = DetectHdrType(videoStream);
            }

            var audioStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Audio);
            if (audioStream is not null)
            {
                audioCodec = DetectAudioCodec(audioStream);
            }
        }

        var rating = ExtractRating(item);

        return new ExtractedMediaInfo(resolution, hdrType, audioCodec, rating);
    }

    /// <summary>
    /// Detects the media resolution from video stream dimensions.
    /// </summary>
    /// <param name="stream">The video stream.</param>
    /// <returns>The detected resolution.</returns>
    public static MediaResolution DetectResolution(MediaStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var width = stream.Width ?? 0;
        var height = stream.Height ?? 0;

        if (width >= 3800 || height >= 2100)
        {
            return MediaResolution.Uhd4K;
        }

        if (width >= 1900 || height >= 1000)
        {
            return MediaResolution.Fhd1080p;
        }

        if (width >= 1200 || height >= 700)
        {
            return MediaResolution.Hd720p;
        }

        if (width > 0 || height > 0)
        {
            return MediaResolution.Sd;
        }

        return MediaResolution.None;
    }

    /// <summary>
    /// Detects the HDR or color range type from video stream properties.
    /// </summary>
    /// <param name="stream">The video stream.</param>
    /// <returns>The detected HDR type.</returns>
    public static VideoHdrType DetectHdrType(MediaStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var videoRange = stream.VideoRange.ToString();
        var videoRangeType = stream.VideoRangeType.ToString();
        var title = stream.Title ?? string.Empty;
        var comment = stream.Comment ?? string.Empty;

        var combined = $"{videoRange} {videoRangeType} {title} {comment}".ToUpperInvariant();

        if (combined.Contains("DOVI", StringComparison.Ordinal) ||
            combined.Contains("DV", StringComparison.Ordinal) ||
            combined.Contains("DOLBY VISION", StringComparison.Ordinal))
        {
            return VideoHdrType.DolbyVision;
        }

        if (combined.Contains("HDR10+", StringComparison.Ordinal) ||
            combined.Contains("HDR10PLUS", StringComparison.Ordinal))
        {
            return VideoHdrType.Hdr10Plus;
        }

        if (combined.Contains("HDR10", StringComparison.Ordinal))
        {
            return VideoHdrType.Hdr10;
        }

        if (combined.Contains("HLG", StringComparison.Ordinal))
        {
            return VideoHdrType.Hlg;
        }

        if (combined.Contains("HDR", StringComparison.Ordinal))
        {
            return VideoHdrType.Hdr;
        }

        return VideoHdrType.None;
    }

    /// <summary>
    /// Detects the audio codec from audio stream properties.
    /// </summary>
    /// <param name="stream">The audio stream.</param>
    /// <returns>The detected audio codec.</returns>
    public static AudioCodecType DetectAudioCodec(MediaStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var title = stream.Title ?? string.Empty;
        var codec = stream.Codec ?? string.Empty;
        var profile = stream.Profile ?? string.Empty;

        var combined = $"{title} {codec} {profile}".ToUpperInvariant();

        if (combined.Contains("ATMOS", StringComparison.Ordinal))
        {
            return AudioCodecType.DolbyAtmos;
        }

        if (combined.Contains("DTS:X", StringComparison.Ordinal) || combined.Contains("DTSX", StringComparison.Ordinal))
        {
            return AudioCodecType.DtsX;
        }

        if (combined.Contains("TRUEHD", StringComparison.Ordinal))
        {
            return AudioCodecType.TrueHd;
        }

        if (combined.Contains("DTS-HD MA", StringComparison.Ordinal) || combined.Contains("DTSHD", StringComparison.Ordinal))
        {
            return AudioCodecType.DtsHdMa;
        }

        if (combined.Contains("FLAC", StringComparison.Ordinal))
        {
            return AudioCodecType.Flac;
        }

        return AudioCodecType.None;
    }

    /// <summary>
    /// Extracts the community or IMDb rating for a library item.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <returns>The rating score, or null if unavailable.</returns>
    public static float? ExtractRating(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.CommunityRating.HasValue)
        {
            return item.CommunityRating.Value;
        }

        if (item is Series series && series.CommunityRating.HasValue)
        {
            return series.CommunityRating.Value;
        }

        return null;
    }
}
