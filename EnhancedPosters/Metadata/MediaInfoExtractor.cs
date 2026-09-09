using System;
using System.Linq;
using System.Text.RegularExpressions;
using EnhancedPosters.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;

namespace EnhancedPosters.Metadata;

/// <summary>
/// Extracts media information (resolution, video range, audio codec, rating, edition, 3D) from Jellyfin items.
/// </summary>
public static class MediaInfoExtractor
{
    /// <summary>
    /// Extracts media and rating information from a Jellyfin BaseItem.
    /// </summary>
    /// <param name="item">The Jellyfin library item.</param>
    /// <param name="ratingPreference">The rating source preference.</param>
    /// <returns>The extracted media information.</returns>
    public static ExtractedMediaInfo Extract(BaseItem item, RatingSourcePreference ratingPreference = RatingSourcePreference.Community)
    {
        ArgumentNullException.ThrowIfNull(item);

        var resolution = MediaResolution.None;
        var hdrType = VideoHdrType.None;
        var hasHdrFallback = false;
        var hasHdr10PlusFallback = false;
        var audioCodec = AudioCodecType.None;
        var hasTrueHdWithAtmos = false;

        var mediaStreams = item.GetMediaStreams();
        if (mediaStreams is not null && mediaStreams.Count > 0)
        {
            var videoStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Video);
            if (videoStream is not null)
            {
                resolution = DetectResolution(videoStream);
                (hdrType, hasHdrFallback, hasHdr10PlusFallback) = DetectHdrDetails(videoStream);
            }

            var audioStream = mediaStreams.FirstOrDefault(s => s.Type == MediaStreamType.Audio);
            if (audioStream is not null)
            {
                (audioCodec, hasTrueHdWithAtmos) = DetectAudioDetails(audioStream);
            }
        }

        var rating = ExtractRating(item, ratingPreference);
        var (edition, customEditionName) = DetectEdition(item);
        var is3D = Detect3D(item);

        return new ExtractedMediaInfo(resolution, hdrType, audioCodec, rating, edition, customEditionName, is3D, hasHdrFallback, hasHdr10PlusFallback, hasTrueHdWithAtmos);
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
    /// Detects HDR details including color range type and fallback layers (e.g. Dolby Vision with HDR10/HDR10+ fallback).
    /// </summary>
    /// <param name="stream">The video stream.</param>
    /// <returns>A tuple containing HDR type and fallback flags.</returns>
    public static (VideoHdrType HdrType, bool HasHdrFallback, bool HasHdr10PlusFallback) DetectHdrDetails(MediaStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var videoRange = stream.VideoRange.ToString();
        var videoRangeType = stream.VideoRangeType.ToString();
        var title = stream.Title ?? string.Empty;
        var comment = stream.Comment ?? string.Empty;

        var combined = $"{videoRange} {videoRangeType} {title} {comment}".ToUpperInvariant();

        var hasDv = combined.Contains("DOVI", StringComparison.Ordinal) ||
                    combined.Contains("DV", StringComparison.Ordinal) ||
                    combined.Contains("DOLBY VISION", StringComparison.Ordinal);

        var hasHdr10Plus = combined.Contains("HDR10+", StringComparison.Ordinal) ||
                           combined.Contains("HDR10PLUS", StringComparison.Ordinal) ||
                           combined.Contains("DOVIWITHHDR10PLUS", StringComparison.Ordinal);

        var hasHdr10 = combined.Contains("HDR10", StringComparison.Ordinal) ||
                       combined.Contains("DOVIWITHHDR10", StringComparison.Ordinal);

        var hasHlg = combined.Contains("HLG", StringComparison.Ordinal) ||
                     combined.Contains("DOVIWITHHLG", StringComparison.Ordinal);

        var hasHdr = combined.Contains("HDR", StringComparison.Ordinal);

        if (hasDv)
        {
            if (hasHdr10Plus)
            {
                return (VideoHdrType.DolbyVision, HasHdrFallback: true, HasHdr10PlusFallback: true);
            }

            if (hasHdr10 || hasHdr)
            {
                return (VideoHdrType.DolbyVision, HasHdrFallback: true, HasHdr10PlusFallback: false);
            }

            return (VideoHdrType.DolbyVision, HasHdrFallback: false, HasHdr10PlusFallback: false);
        }

        if (hasHdr10Plus)
        {
            return (VideoHdrType.Hdr10Plus, false, false);
        }

        if (hasHdr10)
        {
            return (VideoHdrType.Hdr10, false, false);
        }

        if (hasHlg)
        {
            return (VideoHdrType.Hlg, false, false);
        }

        if (hasHdr)
        {
            return (VideoHdrType.Hdr, false, false);
        }

        return (VideoHdrType.None, false, false);
    }

    /// <summary>
    /// Detects the HDR or color range type from video stream properties.
    /// </summary>
    /// <param name="stream">The video stream.</param>
    /// <returns>The detected HDR type.</returns>
    public static VideoHdrType DetectHdrType(MediaStream stream)
    {
        return DetectHdrDetails(stream).HdrType;
    }

    /// <summary>
    /// Detects audio details including primary codec and whether Dolby Atmos is paired with TrueHD.
    /// </summary>
    /// <param name="stream">The audio stream.</param>
    /// <returns>A tuple containing the detected audio codec and whether Atmos has TrueHD base.</returns>
    public static (AudioCodecType Codec, bool HasTrueHdWithAtmos) DetectAudioDetails(MediaStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var title = stream.Title ?? string.Empty;
        var codec = stream.Codec ?? string.Empty;
        var profile = stream.Profile ?? string.Empty;

        var combined = $"{title} {codec} {profile}".ToUpperInvariant();

        var isAtmos = combined.Contains("ATMOS", StringComparison.Ordinal);
        var isTrueHd = combined.Contains("TRUEHD", StringComparison.Ordinal);

        if (isAtmos)
        {
            return (AudioCodecType.DolbyAtmos, isTrueHd);
        }

        if (combined.Contains("DTS:X", StringComparison.Ordinal) || combined.Contains("DTSX", StringComparison.Ordinal))
        {
            return (AudioCodecType.DtsX, false);
        }

        if (isTrueHd)
        {
            return (AudioCodecType.TrueHd, false);
        }

        if (combined.Contains("DTS-HD MA", StringComparison.Ordinal) || combined.Contains("DTSHD", StringComparison.Ordinal))
        {
            return (AudioCodecType.DtsHdMa, false);
        }

        if (combined.Contains("FLAC", StringComparison.Ordinal))
        {
            return (AudioCodecType.Flac, false);
        }

        return (AudioCodecType.None, false);
    }

    /// <summary>
    /// Detects the audio codec from audio stream properties.
    /// </summary>
    /// <param name="stream">The audio stream.</param>
    /// <returns>The detected audio codec.</returns>
    public static AudioCodecType DetectAudioCodec(MediaStream stream)
    {
        return DetectAudioDetails(stream).Codec;
    }

    /// <summary>
    /// Extracts the rating score for a library item based on source preference.
    /// Supports Community (IMDb/TMDb), Critic (Rotten Tomatoes), and Combined Average.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="preference">The rating source preference.</param>
    /// <returns>The rating score, or null if unavailable.</returns>
    public static float? ExtractRating(BaseItem item, RatingSourcePreference preference = RatingSourcePreference.Community)
    {
        ArgumentNullException.ThrowIfNull(item);

        float? community = item.CommunityRating;
        if (!community.HasValue && item is Series series)
        {
            community = series.CommunityRating;
        }

        float? critic = item.CriticRating;
        if (!critic.HasValue && item is Series sCritic)
        {
            critic = sCritic.CriticRating;
        }

        // Normalize Critic Rating to 10-point scale if it's on a 100-point scale (e.g. Rotten Tomatoes 84 -> 8.4)
        if (critic.HasValue && critic.Value > 10.0f)
        {
            critic = critic.Value / 10.0f;
        }

        return preference switch
        {
            RatingSourcePreference.Critic => critic ?? community,
            RatingSourcePreference.CombinedAverage => (community.HasValue && critic.HasValue)
                ? (float)Math.Round((community.Value + critic.Value) / 2f, 1)
                : (community ?? critic),
            _ => community ?? critic
        };
    }

    /// <summary>
    /// Detects edition information from item metadata, path, or title.
    /// Supports tags like {edition-Imax}, [edition-Extended], or standard keywords.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <returns>A tuple containing the detected edition type and optional custom edition name.</returns>
    public static (EditionType Edition, string? CustomName) DetectEdition(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var path = item.Path ?? string.Empty;
        var name = item.Name ?? string.Empty;
        var originalTitle = item.OriginalTitle ?? string.Empty;

        return ParseEdition(path, name, originalTitle);
    }

    /// <summary>
    /// Parses edition type from path and title strings.
    /// </summary>
    /// <param name="path">The file or folder path.</param>
    /// <param name="name">The item name.</param>
    /// <param name="originalTitle">The original title.</param>
    /// <returns>A tuple containing the detected edition type and optional custom edition name.</returns>
    public static (EditionType Edition, string? CustomName) ParseEdition(string path, string name, string originalTitle = "")
    {
        var combined = $"{path} {name} {originalTitle}";
        if (string.IsNullOrWhiteSpace(combined))
        {
            return (EditionType.None, null);
        }

        // 1. Check for {edition-...} or [edition-...] tags (Jellyfin / Plex standard)
        var match = Regex.Match(combined, @"[\{\[]edition-(?<edition>[^\}\]]+)[\}\]]", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var rawEdition = match.Groups["edition"].Value.Trim();
            var normalized = NormalizeEdition(rawEdition);
            if (normalized != EditionType.None)
            {
                return (normalized, null);
            }

            return (EditionType.Custom, rawEdition.ToUpperInvariant());
        }

        // 2. Keyword matching on combined path and title
        return (NormalizeEdition(combined), null);
    }

    /// <summary>
    /// Normalizes edition name or keyword to standard EditionType enum.
    /// </summary>
    /// <param name="text">The raw text or keywords to analyze.</param>
    /// <returns>The normalized EditionType.</returns>
    public static EditionType NormalizeEdition(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return EditionType.None;
        }

        var upper = text.ToUpperInvariant();

        if (Regex.IsMatch(upper, @"\bIMAX(\s+ENHANCED)?\b"))
        {
            return EditionType.Imax;
        }

        if (Regex.IsMatch(upper, @"\bDIRECTOR'?S?(\s+CUT)?\b") || Regex.IsMatch(upper, @"\bD\.?C\.?\b"))
        {
            return EditionType.DirectorsCut;
        }

        if (Regex.IsMatch(upper, @"\bEXTENDED(\s+(CUT|EDITION))?\b"))
        {
            return EditionType.Extended;
        }

        if (Regex.IsMatch(upper, @"\bTHEATRICAL(\s+(CUT|EDITION))?\b"))
        {
            return EditionType.Theatrical;
        }

        if (Regex.IsMatch(upper, @"\bUNRATED(\s+(CUT|EDITION))?\b"))
        {
            return EditionType.Unrated;
        }

        if (Regex.IsMatch(upper, @"\bSPECIAL\s+EDITION\b"))
        {
            return EditionType.SpecialEdition;
        }

        if (Regex.IsMatch(upper, @"\bREMASTER(ED)?(\s+EDITION)?\b"))
        {
            return EditionType.Remastered;
        }

        if (Regex.IsMatch(upper, @"\bFINAL\s+CUT\b"))
        {
            return EditionType.FinalCut;
        }

        return EditionType.None;
    }

    /// <summary>
    /// Detects if media is 3D format from video stream properties or filename tags ({edition-3D}, [3D], etc.).
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <returns>True if 3D is detected; otherwise false.</returns>
    public static bool Detect3D(BaseItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item is Video video && video.Video3DFormat.HasValue)
        {
            return true;
        }

        var mediaStreams = item.GetMediaStreams();
        if (mediaStreams is not null)
        {
            foreach (var stream in mediaStreams)
            {
                if (stream.Type == MediaStreamType.Video)
                {
                    var comment = stream.Comment ?? string.Empty;
                    var title = stream.Title ?? string.Empty;
                    if (Is3DPathOrTitle($"{comment} {title}"))
                    {
                        return true;
                    }
                }
            }
        }

        var path = item.Path ?? string.Empty;
        var name = item.Name ?? string.Empty;
        var combined = $"{path} {name}";

        return Is3DPathOrTitle(combined);
    }

    /// <summary>
    /// Determines whether a path or title contains 3D identifiers.
    /// </summary>
    /// <param name="text">The string to check.</param>
    /// <returns>True if 3D indicators are found; otherwise false.</returns>
    public static bool Is3DPathOrTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Regex.IsMatch(text, @"[\{\[]edition-3D[\}\]]", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(text, @"[\. _\-\[\(]3D[\. _\-\]\)]", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(text, @"\b(3D-SBS|3D-TAB|3D-OU|3D-MVC|HSBS|HTAB|Half-SBS|Half-OU)\b", RegexOptions.IgnoreCase);
    }
}
