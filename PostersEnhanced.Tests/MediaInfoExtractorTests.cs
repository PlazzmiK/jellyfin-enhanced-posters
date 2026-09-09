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

    [Theory]
    [InlineData("Movie Name (2023) {tmdb-123456} {edition-Imax} [BLURAY-2160P].mkv", EditionType.Imax, null)]
    [InlineData("Avatar {edition-Extended Cut}.mkv", EditionType.Extended, null)]
    [InlineData("Blade Runner [edition-Director's Cut].mkv", EditionType.DirectorsCut, null)]
    [InlineData("Aliens {edition-Theatrical}.mkv", EditionType.Theatrical, null)]
    [InlineData("Deadpool [edition-Unrated].mkv", EditionType.Unrated, null)]
    [InlineData("Star Wars {edition-Special Edition}.mkv", EditionType.SpecialEdition, null)]
    [InlineData("Terminator 2 [edition-Remastered].mkv", EditionType.Remastered, null)]
    [InlineData("Dune (1984) {edition-Spicediver Cut}.mkv", EditionType.Custom, "SPICEDIVER CUT")]
    [InlineData("Interstellar.IMAX.2160p.mkv", EditionType.Imax, null)]
    [InlineData("Lord of the Rings Extended.mkv", EditionType.Extended, null)]
    [InlineData("Standard Movie (2022).mkv", EditionType.None, null)]
    public void ParseEdition_ReturnsExpectedEdition(string path, EditionType expectedType, string? expectedCustom)
    {
        var (edition, custom) = MediaInfoExtractor.ParseEdition(path, string.Empty);
        Assert.Equal(expectedType, edition);
        Assert.Equal(expectedCustom, custom);
    }

    [Theory]
    [InlineData("Avatar (2009) {edition-3D}.mkv", true)]
    [InlineData("Gravity (2013) [3D].mkv", true)]
    [InlineData("Dredd.3D.2012.1080p.mkv", true)]
    [InlineData("Tron.Legacy.3D-SBS.mkv", true)]
    [InlineData("Hugo (2011) Half-SBS.mkv", true)]
    [InlineData("Inception (2010) 1080p.mkv", false)]
    public void Is3DPathOrTitle_ReturnsExpected(string path, bool expected3D)
    {
        var actual = MediaInfoExtractor.Is3DPathOrTitle(path);
        Assert.Equal(expected3D, actual);
    }

    [Fact]
    public void ExtractRating_WithCommunityPreference_ReturnsCommunityScore()
    {
        var movie = new MediaBrowser.Controller.Entities.Movies.Movie
        {
            CommunityRating = 7.8f,
            CriticRating = 85f
        };

        var score = MediaInfoExtractor.ExtractRating(movie, RatingSourcePreference.Community);
        Assert.Equal(7.8f, score);
    }

    [Fact]
    public void ExtractRating_WithCriticPreference_Normalizes100PointScale()
    {
        var movie = new MediaBrowser.Controller.Entities.Movies.Movie
        {
            CommunityRating = 7.8f,
            CriticRating = 85f
        };

        var score = MediaInfoExtractor.ExtractRating(movie, RatingSourcePreference.Critic);
        Assert.Equal(8.5f, score);
    }

    [Fact]
    public void ExtractRating_WithCombinedAverage_AveragesBothScores()
    {
        var movie = new MediaBrowser.Controller.Entities.Movies.Movie
        {
            CommunityRating = 7.5f,
            CriticRating = 85f // Normalized to 8.5
        };

        var score = MediaInfoExtractor.ExtractRating(movie, RatingSourcePreference.CombinedAverage);
        // (7.5 + 8.5) / 2 = 8.0
        Assert.Equal(8.0f, score);
    }
}

