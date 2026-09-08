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
}
