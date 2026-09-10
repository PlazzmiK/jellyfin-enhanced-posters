using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnhancedPosters.Configuration;
using EnhancedPosters.Storage;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace EnhancedPosters.ScheduledTasks;

/// <summary>
/// Scheduled task that re-downloads pristine, clean posters directly from remote providers (TMDb, TheTVDB, btttr.cc)
/// and writes them away as the original / backup files (and restores Jellyfin primary images to clean originals).
/// </summary>
public class EnhancedPostersDownloadCleanPostersTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly PosterBackupManager _backupManager;
    private readonly RenderStampTracker _stampTracker;
    private readonly ILogger<EnhancedPostersDownloadCleanPostersTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedPostersDownloadCleanPostersTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="backupManager">The poster backup manager.</param>
    /// <param name="stampTracker">The render stamp tracker.</param>
    /// <param name="logger">The logger.</param>
    public EnhancedPostersDownloadCleanPostersTask(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        PosterBackupManager backupManager,
        RenderStampTracker stampTracker,
        ILogger<EnhancedPostersDownloadCleanPostersTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _backupManager = backupManager;
        _stampTracker = stampTracker;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Re-download Pristine Original Posters";

    /// <inheritdoc />
    public string Key => "EnhancedPostersDownloadCleanPosters";

    /// <inheritdoc />
    public string Description => "Re-downloads fresh, label-free posters from remote providers (TMDb, TheTVDB, btttr.cc) and saves them as the original / backup poster.";

    /// <inheritdoc />
    public string Category => Plugin.PluginName;

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var items = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            Recursive = true
        });

        if (items.Count == 0)
        {
            _logger.LogInformation("No movies or shows found for clean poster download");
            progress.Report(100);
            return;
        }

        var downloadedCount = 0;
        var skippedCount = 0;

        _logger.LogInformation("Re-downloading pristine clean posters for {ItemCount} items", items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i];

            try
            {
                var refreshed = await _backupManager.ForceRefreshPristineBackupAsync(item, configuration, cancellationToken).ConfigureAwait(false);
                if (refreshed)
                {
                    var backupPath = _backupManager.GetBackupFilePath(item, configuration);
                    if (File.Exists(backupPath))
                    {
                        var stream = File.OpenRead(backupPath);
                        await using (stream.ConfigureAwait(false))
                        {
                            await _providerManager.SaveImage(item, stream, "image/jpeg", ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                            await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    _stampTracker.ClearOutputImage(item.Id);
                    _stampTracker.ClearRender(item.Id);
                    downloadedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to re-download pristine poster for {ItemName} ({ItemId})", item.Name, item.Id);
                skippedCount++;
            }

            progress.Report((i + 1) * 100D / items.Count);
        }

        _stampTracker.Save();

        _logger.LogInformation(
            "Re-download pristine original posters completed: {DownloadedCount} downloaded & saved as original, {SkippedCount} skipped/not found",
            downloadedCount,
            skippedCount);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
