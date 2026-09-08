using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PostersEnhanced.Configuration;
using PostersEnhanced.Metadata;
using PostersEnhanced.Models;
using PostersEnhanced.Storage;
using Xunit;

namespace PostersEnhanced.Tests;

public class RenderStampTrackerTests
{
    [Fact]
    public void NeedsReRender_ReturnsTrueInitially_AndFalseAfterRecord()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_enhanced_test_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);

            var movie = new Movie
            {
                Id = System.Guid.NewGuid(),
                Name = "Sample Movie"
            };

            var mediaInfo = new ExtractedMediaInfo(MediaResolution.Uhd4K, VideoHdrType.DolbyVision, AudioCodecType.None, 6.3f);
            var config = new PluginConfiguration();

            // 1. Initial check: should need render
            Assert.True(tracker.NeedsReRender(movie, mediaInfo, config, null));

            // 2. Record render
            tracker.RecordRender(movie, mediaInfo, config, null);

            // 3. Subsequent check: should NOT need render
            Assert.False(tracker.NeedsReRender(movie, mediaInfo, config, null));

            // 4. Change config (e.g. toggle 1080p): should need re-render
            config.Show1080p = !config.Show1080p;
            Assert.True(tracker.NeedsReRender(movie, mediaInfo, config, null));

            // 5. Change media info (e.g. upgraded to HDR10+): should need re-render
            var upgradedMediaInfo = new ExtractedMediaInfo(MediaResolution.Uhd4K, VideoHdrType.Hdr10Plus, AudioCodecType.None, 6.3f);
            Assert.True(tracker.NeedsReRender(movie, upgradedMediaInfo, config, null));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
