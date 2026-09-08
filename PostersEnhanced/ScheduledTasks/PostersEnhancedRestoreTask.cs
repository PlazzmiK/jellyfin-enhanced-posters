using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using PostersEnhanced.Configuration;
using PostersEnhanced.Storage;

namespace PostersEnhanced.ScheduledTasks;

/// <summary>
/// Scheduled task that restores all movie and series posters back to their pristine original backups.
/// </summary>
public class PostersEnhancedRestoreTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly PosterBackupManager _backupManager;
    private readonly RenderStampTracker _stampTracker;
    private readonly ILogger<PostersEnhancedRestoreTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostersEnhancedRestoreTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="backupManager">The poster backup manager.</param>
    /// <param name="stampTracker">The render stamp tracker.</param>
    /// <param name="logger">The logger.</param>
    public PostersEnhancedRestoreTask(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        PosterBackupManager backupManager,
        RenderStampTracker stampTracker,
        ILogger<PostersEnhancedRestoreTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _backupManager = backupManager;
        _stampTracker = stampTracker;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Restore Original Posters";

    /// <inheritdoc />
    public string Key => "PostersEnhancedRestore";

    /// <inheritdoc />
    public string Description => "Restores all movie and series posters from their pristine original backups.";

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

        var restoredCount = 0;
        var skippedCount = 0;

        _logger.LogInformation("Restoring original posters for {ItemCount} items", items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i];

            var backupPath = _backupManager.GetBackupFilePath(item, configuration);
            if (File.Exists(backupPath))
            {
                try
                {
                    var stream = File.OpenRead(backupPath);
                    await using (stream.ConfigureAwait(false))
                    {
                        await _providerManager.SaveImage(item, stream, "image/jpeg", ImageType.Primary, null, cancellationToken).ConfigureAwait(false);
                        await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
                    }

                    _stampTracker.ClearOutputImage(item.Id);
                    _stampTracker.ClearRender(item.Id);
                    restoredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to restore poster for {ItemName} ({ItemId})", item.Name, item.Id);
                }
            }
            else
            {
                skippedCount++;
            }

            progress.Report((i + 1) * 100D / items.Count);
        }

        _stampTracker.Save();

        _logger.LogInformation("Restore complete: {RestoredCount} restored, {SkippedCount} skipped (no backup found)", restoredCount, skippedCount);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }
}
