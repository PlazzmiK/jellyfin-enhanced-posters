using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using PostersEnhanced.Configuration;
using PostersEnhanced.Models;

namespace PostersEnhanced.Storage;

/// <summary>
/// Manages non-destructive backups of original posters, keeping pristine copies alongside media files.
/// </summary>
public class PosterBackupManager
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PosterBackupManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PosterBackupManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public PosterBackupManager(
        IApplicationPaths applicationPaths,
        IHttpClientFactory httpClientFactory,
        ILogger<PosterBackupManager> logger)
    {
        _applicationPaths = applicationPaths;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the fallback backup directory under Jellyfin configuration.
    /// </summary>
    public string FallbackBackupDirectory => Path.Combine(_applicationPaths.PluginConfigurationsPath, "PostersEnhanced", "backups");

    /// <summary>
    /// Gets the pristine original poster stream for an item, creating a backup if one does not exist.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream containing the pristine original image bytes.</returns>
    public async Task<Stream?> GetPristinePosterStreamAsync(
        BaseItem item,
        PluginConfiguration config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(config);

        var backupPath = GetBackupFilePath(item, config);

        // 1. If pristine backup already exists on disk, open and return it
        if (File.Exists(backupPath))
        {
            return new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // 2. Otherwise, acquire the pristine source and create the backup file
        byte[]? sourceBytes = null;

        if (config.Source == PosterSource.BtttrCcClean)
        {
            sourceBytes = await DownloadCleanPosterAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (sourceBytes is null || sourceBytes.Length == 0)
        {
            sourceBytes = ReadCurrentLocalPoster(item);
        }

        if (sourceBytes is null || sourceBytes.Length == 0)
        {
            _logger.LogWarning("No source poster available for item {ItemName} ({ItemId})", item.Name, item.Id);
            return null;
        }

        // 3. Save pristine backup so it is never re-downloaded or re-compressed
        try
        {
            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(backupPath, sourceBytes, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Saved pristine poster backup for {ItemName} to {BackupPath}", item.Name, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save poster backup beside media for {ItemName}. Retrying in fallback location.", item.Name);

            // Retry in fallback directory
            try
            {
                if (!Directory.Exists(FallbackBackupDirectory))
                {
                    Directory.CreateDirectory(FallbackBackupDirectory);
                }

                backupPath = Path.Combine(FallbackBackupDirectory, $"{item.Id}.jpg");
                await File.WriteAllBytesAsync(backupPath, sourceBytes, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to write backup to fallback path for {ItemName}", item.Name);
            }
        }

        return new MemoryStream(sourceBytes);
    }

    /// <summary>
    /// Overwrites the pristine backup with the item's current primary image (used when an external metadata refresh occurs).
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully updated, false otherwise.</returns>
    public async Task<bool> UpdateBackupFromCurrentPrimaryAsync(
        BaseItem item,
        PluginConfiguration config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(config);

        var primaryPath = item.GetImagePath(ImageType.Primary);
        if (string.IsNullOrEmpty(primaryPath) || !File.Exists(primaryPath))
        {
            return false;
        }

        byte[] bytes;
        try
        {
            bytes = await File.ReadAllBytesAsync(primaryPath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read newly refreshed primary image at {PrimaryPath} for {ItemName}", primaryPath, item.Name);
            return false;
        }

        if (bytes.Length == 0)
        {
            return false;
        }

        var backupPath = GetBackupFilePath(item, config);
        try
        {
            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(backupPath, bytes, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated pristine poster backup for {ItemName} with newly refreshed artwork at {BackupPath}", item.Name, backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write updated backup at {BackupPath} for {ItemName}", backupPath, item.Name);
            return false;
        }
    }

    /// <summary>
    /// Determines the target backup file path for an item.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <returns>The file path where the original should be stored.</returns>
    public string GetBackupFilePath(BaseItem item, PluginConfiguration config)
    {
        if (config.SaveOriginalBesideMedia)
        {
            string? mediaDir = null;

            if (item is Movie movie && !string.IsNullOrWhiteSpace(movie.Path))
            {
                mediaDir = Path.GetDirectoryName(movie.Path);
            }
            else if (item is Series series && !string.IsNullOrWhiteSpace(series.Path))
            {
                mediaDir = series.Path;
            }
            else if (!string.IsNullOrWhiteSpace(item.Path))
            {
                mediaDir = Directory.Exists(item.Path) ? item.Path : Path.GetDirectoryName(item.Path);
            }

            if (!string.IsNullOrWhiteSpace(mediaDir) && Directory.Exists(mediaDir))
            {
                // Check if existing backup exists with other extensions
                var preferred = Path.Combine(mediaDir, "poster-original.jpg");
                if (File.Exists(preferred))
                {
                    return preferred;
                }

                var png = Path.Combine(mediaDir, "poster-original.png");
                if (File.Exists(png))
                {
                    return png;
                }

                return preferred;
            }
        }

        return Path.Combine(FallbackBackupDirectory, $"{item.Id}.jpg");
    }

    private static byte[]? ReadCurrentLocalPoster(BaseItem item)
    {
        var localPath = item.GetImagePath(ImageType.Primary);
        if (!string.IsNullOrEmpty(localPath) && File.Exists(localPath))
        {
            try
            {
                return File.ReadAllBytes(localPath);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private async Task<byte[]?> DownloadCleanPosterAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var imdbId = item.GetProviderId(MetadataProvider.Imdb);
        if (string.IsNullOrWhiteSpace(imdbId))
        {
            return null;
        }

        var url = $"https://btttr.cc/none-none-none-none-none/imdb/poster-clean/{Uri.EscapeDataString(imdbId)}.jpg";
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to fetch btttr.cc clean poster for {ImdbId}", imdbId);
        }

        return null;
    }
}
