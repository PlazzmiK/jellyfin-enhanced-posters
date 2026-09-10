using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnhancedPosters.Configuration;
using EnhancedPosters.ScheduledTasks;
using EnhancedPosters.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EnhancedPosters.Tests;

public class PosterBackupManagerTests
{
    [Fact]
    public async Task SnapshotCurrentAsBackupAsync_WhenPrimaryImageExists_SavesOriginalBesideMedia()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_test_snap_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var mockHttpFactory = new Mock<IHttpClientFactory>();
            var backupManager = new PosterBackupManager(
                mockPaths.Object,
                mockHttpFactory.Object,
                NullLogger<PosterBackupManager>.Instance);

            var movieFilePath = Path.Combine(tempDir, "Movie (2020).mkv");
            File.WriteAllBytes(movieFilePath, [0]);

            var primaryImagePath = Path.Combine(tempDir, "poster.jpg");
            var expectedBytes = new byte[] { 10, 20, 30, 40, 50 };
            File.WriteAllBytes(primaryImagePath, expectedBytes);

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Name = "Test Movie",
                Path = movieFilePath
            };
            movie.SetImage(new ItemImageInfo { Path = primaryImagePath, Type = ImageType.Primary }, 0);

            var config = new PluginConfiguration
            {
                SaveOriginalBesideMedia = true
            };

            var success = await backupManager.SnapshotCurrentAsBackupAsync(movie, config, CancellationToken.None);

            Assert.True(success);

            var backupPath = Path.Combine(tempDir, "poster-original.jpg");
            Assert.True(File.Exists(backupPath));

            var savedBytes = await File.ReadAllBytesAsync(backupPath);
            Assert.Equal(expectedBytes, savedBytes);
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
    public async Task SnapshotCurrentTask_RunsAndSnapshotsItems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_test_task_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var mockHttpFactory = new Mock<IHttpClientFactory>();
            var backupManager = new PosterBackupManager(
                mockPaths.Object,
                mockHttpFactory.Object,
                NullLogger<PosterBackupManager>.Instance);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);

            var movieFilePath = Path.Combine(tempDir, "Film (2022).mkv");
            File.WriteAllBytes(movieFilePath, [0]);

            var primaryImagePath = Path.Combine(tempDir, "poster.jpg");
            File.WriteAllBytes(primaryImagePath, [1, 2, 3]);

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Name = "Film",
                Path = movieFilePath
            };
            movie.SetImage(new ItemImageInfo { Path = primaryImagePath, Type = ImageType.Primary }, 0);

            var mockLibraryManager = new Mock<ILibraryManager>();
            mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie });

            var task = new EnhancedPostersSnapshotCurrentTask(
                mockLibraryManager.Object,
                backupManager,
                tracker,
                NullLogger<EnhancedPostersSnapshotCurrentTask>.Instance);

            var progress = new Mock<IProgress<double>>();
            await task.ExecuteAsync(progress.Object, CancellationToken.None);

            var backupPath = Path.Combine(tempDir, "poster-original.jpg");
            Assert.True(File.Exists(backupPath));
            progress.Verify(p => p.Report(It.Is<double>(val => val >= 100D)), Times.AtLeastOnce());
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
    public async Task DownloadCleanPostersTask_WhenNoRemoteImageFound_HandlesGracefully()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "posters_test_cleantask_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var mockPaths = new Mock<IApplicationPaths>();
            mockPaths.Setup(p => p.PluginConfigurationsPath).Returns(tempDir);

            var mockHttpFactory = new Mock<IHttpClientFactory>();
            var mockProviderManager = new Mock<IProviderManager>();

            var backupManager = new PosterBackupManager(
                mockPaths.Object,
                mockHttpFactory.Object,
                NullLogger<PosterBackupManager>.Instance,
                mockProviderManager.Object);

            var tracker = new RenderStampTracker(mockPaths.Object, NullLogger<RenderStampTracker>.Instance);

            var movie = new Movie
            {
                Id = Guid.NewGuid(),
                Name = "No Remote Image Movie",
                Path = Path.Combine(tempDir, "movie.mkv")
            };

            var mockLibraryManager = new Mock<ILibraryManager>();
            mockLibraryManager
                .Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { movie });

            var task = new EnhancedPostersDownloadCleanPostersTask(
                mockLibraryManager.Object,
                mockProviderManager.Object,
                backupManager,
                tracker,
                NullLogger<EnhancedPostersDownloadCleanPostersTask>.Instance);

            var progress = new Mock<IProgress<double>>();
            await task.ExecuteAsync(progress.Object, CancellationToken.None);

            progress.Verify(p => p.Report(It.Is<double>(val => val >= 100D)), Times.AtLeastOnce());
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
