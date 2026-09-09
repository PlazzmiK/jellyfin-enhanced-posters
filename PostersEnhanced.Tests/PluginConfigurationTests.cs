using System.IO;
using System.Xml.Serialization;
using PostersEnhanced.Configuration;
using Xunit;

namespace PostersEnhanced.Tests;

public class PluginConfigurationTests
{
    [Fact]
    public void PluginConfiguration_XmlSerialization_RoundtripsSuccessfully()
    {
        var config = new PluginConfiguration
        {
            ShowEditionBadges = true,
            ShowImax = true,
            Show3DBadge = true,
            CombineBadgesInPill = true,
            PillBackgroundOpacity = 0.85f,
            PillCornerRadius = 10f,
            RatingCornerRadius = 6f
        };

        config.SetCustomBadge("dv", "data:image/png;base64,iVBORw0KGgo=");

        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var writer = new StringWriter();
        serializer.Serialize(writer, config);

        var xml = writer.ToString();
        Assert.NotEmpty(xml);
        Assert.Contains("<ShowImax>true</ShowImax>", xml);
        Assert.Contains("data:image/png;base64,iVBORw0KGgo=", xml);

        using var reader = new StringReader(xml);
        var deserialized = (PluginConfiguration?)serializer.Deserialize(reader);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.ShowImax);
        Assert.True(deserialized.Show3DBadge);
        Assert.True(deserialized.CombineBadgesInPill);
        Assert.Equal(0.85f, deserialized.PillBackgroundOpacity);
        Assert.Equal(10f, deserialized.PillCornerRadius);
        Assert.Equal("data:image/png;base64,iVBORw0KGgo=", deserialized.GetCustomBadge("dv"));
    }

    [Fact]
    public void SetCustomBadge_UpdatesExistingKey()
    {
        var config = new PluginConfiguration();
        config.SetCustomBadge("imax", "data1");
        Assert.Equal("data1", config.GetCustomBadge("imax"));

        config.SetCustomBadge("imax", "data2");
        Assert.Equal("data2", config.GetCustomBadge("imax"));
        Assert.Single(config.CustomBadges);
    }

    [Fact]
    public void AutoCropToPortraitRatio_DefaultsToTrue_AndAffectsConfigHash()
    {
        var config = new PluginConfiguration();
        Assert.True(config.AutoCropToPortraitRatio);

        var hash1 = config.ComputeConfigHash();
        config.AutoCropToPortraitRatio = false;
        var hash2 = config.ComputeConfigHash();

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GetRatingBackgroundColor_MatchesDynamicTiers()
    {
        var config = new PluginConfiguration();
        config.RatingTiers.Clear();
        config.RatingTiers.Add(new RatingTierEntry(0f, 4.99f, "#FF0000", "Bad"));
        config.RatingTiers.Add(new RatingTierEntry(5f, 7.49f, "#FFA500", "Decent"));
        config.RatingTiers.Add(new RatingTierEntry(7.5f, 10f, "#00FF00", "Great"));

        Assert.Equal("#FF0000", config.GetRatingBackgroundColor(3.5f));
        Assert.Equal("#FFA500", config.GetRatingBackgroundColor(6.0f));
        Assert.Equal("#00FF00", config.GetRatingBackgroundColor(8.8f));
    }

    [Fact]
    public void RatingTiers_XmlSerialization_RoundtripsSuccessfully()
    {
        var config = new PluginConfiguration
        {
            UseGlobalCornerRadiusForRating = false,
            RatingSource = Models.RatingSourcePreference.CombinedAverage,
            ResizeLowResolutionPosters = true
        };
        config.RatingTiers.Clear();
        config.RatingTiers.Add(new RatingTierEntry(1.0f, 2.0f, "#112233", "Tier1"));

        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        using var writer = new StringWriter();
        serializer.Serialize(writer, config);

        var xml = writer.ToString();
        Assert.Contains("<UseGlobalCornerRadiusForRating>false</UseGlobalCornerRadiusForRating>", xml);
        Assert.Contains("<RatingSource>CombinedAverage</RatingSource>", xml);
        Assert.Contains("#112233", xml);

        using var reader = new StringReader(xml);
        var deserialized = (PluginConfiguration?)serializer.Deserialize(reader);

        Assert.NotNull(deserialized);
        Assert.False(deserialized.UseGlobalCornerRadiusForRating);
        Assert.Equal(Models.RatingSourcePreference.CombinedAverage, deserialized.RatingSource);
        Assert.Single(deserialized.RatingTiers);
        Assert.Equal("#112233", deserialized.RatingTiers[0].Color);
    }
}

