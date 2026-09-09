using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.BaseItemManager;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using PostersEnhanced.Configuration;
using PostersEnhanced.Drawing;
using PostersEnhanced.Metadata;
using PostersEnhanced.Storage;
using PostersEnhanced.Themes;

namespace PostersEnhanced.ScheduledTasks;

/// <summary>
/// Scheduled task that non-destructively overlays media info badges and dynamic rating pills on posters.
/// </summary>
public class PostersEnhancedUpdateTask : IScheduledTask, IConfigurableScheduledTask
{
    private const int ResultUpdated = 0;
    private const int ResultSkipped = 1;
    private const int ResultFailed = 2;

    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IBaseItemManager _baseItemManager;
    private readonly PosterBackupManager _backupManager;
    private readonly RenderStampTracker _stampTracker;
    private readonly ThemeAssetManager _themeManager;
    private readonly ILogger<PostersEnhancedUpdateTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostersEnhancedUpdateTask"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="baseItemManager">The base item manager.</param>
    /// <param name="backupManager">The poster backup manager.</param>
    /// <param name="stampTracker">The render stamp tracker.</param>
    /// <param name="themeManager">The theme asset manager.</param>
    /// <param name="logger">The logger.</param>
    public PostersEnhancedUpdateTask(
        ILibraryManager libraryManager,
        IProviderManager providerManager,
        IBaseItemManager baseItemManager,
        PosterBackupManager backupManager,
        RenderStampTracker stampTracker,
        ThemeAssetManager themeManager,
        ILogger<PostersEnhancedUpdateTask> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _baseItemManager = baseItemManager;
        _backupManager = backupManager;
        _stampTracker = stampTracker;
        _themeManager = themeManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Update Enhanced Posters";

    /// <inheritdoc />
    public string Key => "PostersEnhancedUpdate";

    /// <inheritdoc />
    public string Description => "Non-destructively enhances movie and series posters with media badges and dynamic rating pills.";

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
            _logger.LogInformation("No movies or shows found for Posters Enhanced update");
            progress.Report(100);
            return;
        }

        var updatedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        _logger.LogInformation("Scanning and enhancing posters for {ItemCount} items", items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = items[i];

            var result = await ProcessItemAsync(item, configuration, cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case ResultUpdated:
                    updatedCount++;
                    break;
                case ResultSkipped:
                    skippedCount++;
                    break;
                case ResultFailed:
                    failedCount++;
                    break;
            }

            progress.Report((i + 1) * 100D / items.Count);
        }

        _stampTracker.Save();

        _logger.LogInformation(
            "Posters Enhanced update completed: {UpdatedCount} updated, {SkippedCount} skipped (unchanged), {FailedCount} failed",
            updatedCount,
            skippedCount,
            failedCount);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return [];
    }

    private async Task<int> ProcessItemAsync(BaseItem item, PluginConfiguration configuration, CancellationToken cancellationToken)
    {
        var mediaInfo = MediaInfoExtractor.Extract(item, configuration.RatingSource);
        var backupPath = _backupManager.GetBackupFilePath(item, configuration);
        var currentPrimaryPath = item.GetImagePath(ImageType.Primary);

        // Check if the primary image was modified externally (e.g. metadata refresh or image replacement in Jellyfin)
        var isExternallyModified = _stampTracker.IsPrimaryImageExternallyModified(item.Id, currentPrimaryPath, backupPath);
        if (isExternallyModified)
        {
            _logger.LogInformation("Detected newly refreshed poster for {ItemName}. Updating pristine backup...", item.Name);
            await _backupManager.UpdateBackupFromCurrentPrimaryAsync(item, configuration, cancellationToken).ConfigureAwait(false);
        }
        else if (!_stampTracker.NeedsReRender(item, mediaInfo, configuration, backupPath))
        {
            // Settings, media, and source poster are unchanged
            return ResultSkipped;
        }

        try
        {
            var pristineStream = await _backupManager.GetPristinePosterStreamAsync(item, configuration, cancellationToken).ConfigureAwait(false);
            if (pristineStream is null)
            {
                return ResultSkipped;
            }

            await using (pristineStream.ConfigureAwait(false))
            {
                using var compositedStream = ImageCompositor.Composite(pristineStream, mediaInfo, configuration, _themeManager);
                await _providerManager.SaveImage(
                    item,
                    compositedStream,
                    "image/jpeg",
                    ImageType.Primary,
                    null,
                    cancellationToken).ConfigureAwait(false);

                await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
            }

            var updatedPrimaryPath = item.GetImagePath(ImageType.Primary) ?? currentPrimaryPath;
            _stampTracker.RecordOutputImage(item.Id, updatedPrimaryPath);
            _stampTracker.RecordRender(item, mediaInfo, configuration, backupPath);
            return ResultUpdated;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance poster for {ItemName} ({ItemId})", item.Name, item.Id);
            return ResultFailed;
        }
    }
}
