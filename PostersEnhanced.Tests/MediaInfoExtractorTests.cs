using MediaBrowser.Model.Entities;
using PostersEnhanced.Metadata;
using PostersEnhanced.Models;
using Xunit;

namespace PostersEnhanced.Tests;

public class MediaInfoExtractorTests
{
    [Theory]
    [InlineData(3840, 2160, MediaResolution.Uhd4K)]
    [InlineData(4096, 2160, MediaResolution.Uhd4K)]
    [InlineData(1920, 1080, MediaResolution.Fhd1080p)]
    [InlineData(1280, 720, MediaResolution.Hd720p)]
    [InlineData(720, 480, MediaResolution.Sd)]
    [InlineData(0, 0, MediaResolution.None)]
    public void DetectResolution_ReturnsExpectedResolution(int width, int height, MediaResolution expected)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            Width = width,
            Height = height
        };

        var actual = MediaInfoExtractor.DetectResolution(stream);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("DOVI", VideoHdrType.DolbyVision)]
    [InlineData("Dolby Vision", VideoHdrType.DolbyVision)]
    [InlineData("HDR10+", VideoHdrType.Hdr10Plus)]
    [InlineData("HDR10Plus", VideoHdrType.Hdr10Plus)]
    [InlineData("HDR10", VideoHdrType.Hdr10)]
    [InlineData("HLG", VideoHdrType.Hlg)]
    [InlineData("HDR", VideoHdrType.Hdr)]
    [InlineData("SDR", VideoHdrType.None)]
    public void DetectHdrType_ReturnsExpectedType(string titleOrComment, VideoHdrType expected)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Video,
            Title = titleOrComment
        };

        var actual = MediaInfoExtractor.DetectHdrType(stream);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Dolby Atmos", "", AudioCodecType.DolbyAtmos)]
    [InlineData("Surround 5.1 Atmos", "eac3", AudioCodecType.DolbyAtmos)]
    [InlineData("DTS:X 7.1", "dts", AudioCodecType.DtsX)]
    [InlineData("", "truehd", AudioCodecType.TrueHd)]
    [InlineData("DTS-HD MA 5.1", "dts", AudioCodecType.DtsHdMa)]
    [InlineData("", "flac", AudioCodecType.Flac)]
    [InlineData("Stereo", "aac", AudioCodecType.None)]
    public void DetectAudioCodec_ReturnsExpectedCodec(string title, string codec, AudioCodecType expected)
    {
        var stream = new MediaStream
        {
            Type = MediaStreamType.Audio,
            Title = title,
            Codec = codec
        };

        var actual = MediaInfoExtractor.DetectAudioCodec(stream);
        Assert.Equal(expected, actual);
    }
}
