using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EnhancedPosters.Configuration;
using EnhancedPosters.Storage;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace EnhancedPosters.ScheduledTasks;

/// <summary>
/// Scheduled task that snapshots the current primary posters set in Jellyfin and establishes them as the pristine originals / backups.
/// </summary>
public class EnhancedPostersSnapshotCurrentTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly PosterBackupManager _backupManager;
    private readonly RenderStampTracker _stampTracker;
    private readonly ILogger<EnhancedPostersSnapshotCurrentTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedPostersSnapshotCurrentTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="backupManager">The poster backup manager.</param>
    /// <param name="stampTracker">The render stamp tracker.</param>
    /// <param name="logger">The logger.</param>
    public EnhancedPostersSnapshotCurrentTask(
        ILibraryManager libraryManager,
        PosterBackupManager backupManager,
        RenderStampTracker stampTracker,
        ILogger<EnhancedPostersSnapshotCurrentTask> logger)
    {
        _libraryManager = libraryManager;
        _backupManager = backupManager;
        _stampTracker = stampTracker;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Snapshot Current Posters as Originals";

    /// <inheritdoc />
    public string Key => "EnhancedPostersSnapshotCurrent";

    /// <inheritdoc />
    public string Description => "Copies all current Jellyfin primary posters and saves them as the original / backup files (poster-original.jpg).";

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
            _logger.LogInformation("No movies or shows found to snapshot");
            progress.Report(100);
            return;
        }

        var snapshottedCount = 0;
        var skippedCount = 0;

        _logger.LogInformation("Snapshotting current posters as original backups for {ItemCount} items", items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i];

            try
            {
                var success = await _backupManager.SnapshotCurrentAsBackupAsync(item, configuration, cancellationToken).ConfigureAwait(false);
                if (success)
                {
                    _stampTracker.ClearOutputImage(item.Id);
                    _stampTracker.ClearRender(item.Id);
                    snapshottedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to snapshot current poster for {ItemName} ({ItemId})", item.Name, item.Id);
                skippedCount++;
            }

            progress.Report((i + 1) * 100D / items.Count);
        }

        _stampTracker.Save();

        _logger.LogInformation(
            "Snapshot current posters as originals completed: {SnapshottedCount} snapshotted as original, {SkippedCount} skipped",
            snapshottedCount,
            skippedCount);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
