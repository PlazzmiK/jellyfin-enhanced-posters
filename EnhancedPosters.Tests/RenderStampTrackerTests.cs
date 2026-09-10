using System;
using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using EnhancedPosters.Configuration;
using EnhancedPosters.Metadata;
using EnhancedPosters.Models;
using EnhancedPosters.Storage;
using Xunit;

namespace EnhancedPosters.Tests;

public class RenderStampTrackerTests
{
    [Fact]
    public void NeedsReRender_ReturnsTrueInitially_AndFalseAfterRecord()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_enhanced_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
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

    [Fact]
    public void IsPrimaryImageExternallyModified_DetectsModifiedPrimaryFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_enhanced_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);
            var itemId = Guid.NewGuid();

            var primaryPath = Path.Combine(tempDir, "poster.jpg");
            File.WriteAllBytes(primaryPath, [1, 2, 3, 4, 5]);

            // Record initial output
            tracker.RecordOutputImage(itemId, primaryPath);

            // Same file unchanged: should NOT be externally modified
            Assert.False(tracker.IsPrimaryImageExternallyModified(itemId, primaryPath, null));

            // External update: simulate Jellyfin writing a newly downloaded poster
            File.WriteAllBytes(primaryPath, [10, 20, 30, 40, 50, 60, 70, 80]);
            File.SetLastWriteTimeUtc(primaryPath, DateTime.UtcNow.AddMinutes(5));

            // Now it should detect external modification
            Assert.True(tracker.IsPrimaryImageExternallyModified(itemId, primaryPath, null));

            // Persistence check
            tracker.Save();
            var reloadedTracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);
            Assert.True(reloadedTracker.IsPrimaryImageExternallyModified(itemId, primaryPath, null));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void IsPrimaryImageExternallyModified_WithoutOutputStamp_DoesNotOverwriteExistingBackup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_enhanced_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);
            var itemId = Guid.NewGuid();

            var backupPath = Path.Combine(tempDir, "poster-original.jpg");
            var primaryPath = Path.Combine(tempDir, "poster.jpg");

            // Backup written 10 minutes ago
            File.WriteAllBytes(backupPath, [1, 2, 3]);
            File.SetLastWriteTimeUtc(backupPath, DateTime.UtcNow.AddMinutes(-10));

            // Primary written now (could be a composited poster from a prior session)
            File.WriteAllBytes(primaryPath, [4, 5, 6, 7]);
            File.SetLastWriteTimeUtc(primaryPath, DateTime.UtcNow);

            // Without an authenticated output stamp, it must safely return false to protect poster-original.jpg
            Assert.False(tracker.IsPrimaryImageExternallyModified(itemId, primaryPath, backupPath));
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
