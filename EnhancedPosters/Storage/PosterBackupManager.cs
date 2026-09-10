using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnhancedPosters.Configuration;
using EnhancedPosters.Models;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace EnhancedPosters.Storage;

/// <summary>
/// Manages non-destructive backups of original posters, keeping pristine copies alongside media files.
/// </summary>
public class PosterBackupManager
{
    private readonly IApplicationPaths _applicationPaths;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PosterBackupManager> _logger;
    private readonly IProviderManager? _providerManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PosterBackupManager"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="providerManager">Optional provider manager for remote image lookup.</param>
    public PosterBackupManager(
        IApplicationPaths applicationPaths,
        IHttpClientFactory httpClientFactory,
        ILogger<PosterBackupManager> logger,
        IProviderManager? providerManager = null)
    {
        _applicationPaths = applicationPaths;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _providerManager = providerManager;
    }

    /// <summary>
    /// Gets the fallback backup directory under Jellyfin configuration.
    /// </summary>
    public string FallbackBackupDirectory
    {
        get
        {
            var newDir = Path.Combine(_applicationPaths.PluginConfigurationsPath, "EnhancedPosters", "backups");
            var oldDir = Path.Combine(_applicationPaths.PluginConfigurationsPath, "PostersEnhanced", "backups");
            if (!Directory.Exists(newDir) && Directory.Exists(oldDir))
            {
                return oldDir;
            }

            return newDir;
        }
    }

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

        // 1. If pristine backup already exists on disk (and not forcing remote download), open and return it
        if (File.Exists(backupPath) && config.Source != PosterSource.RemoteProvidersFirst)
        {
            return new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        // 2. Otherwise, acquire the pristine source and create the backup file
        byte[]? sourceBytes = null;

        if (config.Source == PosterSource.RemoteProvidersFirst)
        {
            sourceBytes = await DownloadCleanRemotePosterAsync(item, cancellationToken).ConfigureAwait(false);
        }
        else if (config.Source == PosterSource.BtttrCcClean)
        {
            sourceBytes = await DownloadCleanPosterAsync(item, cancellationToken).ConfigureAwait(false);
        }

        if (sourceBytes is null || sourceBytes.Length == 0)
        {
            if (File.Exists(backupPath))
            {
                return new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            // If not strictly local-only, try clean remote providers before resorting to local file
            if (config.Source != PosterSource.LocalOnly)
            {
                sourceBytes = await DownloadCleanRemotePosterAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (sourceBytes is null || sourceBytes.Length == 0)
            {
                sourceBytes = ReadCurrentLocalPoster(item);
            }
        }

        if (sourceBytes is null || sourceBytes.Length == 0)
        {
            _logger.LogWarning("No source poster available for item {ItemName} ({ItemId})", item.Name, item.Id);
            return null;
        }

        // 3. Save pristine backup so it is never re-downloaded or re-compressed
        await SaveBytesToBackupAsync(item, config, sourceBytes, cancellationToken).ConfigureAwait(false);
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
        return await DownloadBytesFromUrlAsync(url, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a fresh, pristine original poster from Jellyfin's registered remote image providers (TMDb, TheTVDB, Fanart.tv) or btttr.cc.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Image bytes if found and successfully downloaded, otherwise null.</returns>
    public async Task<byte[]?> DownloadCleanRemotePosterAsync(BaseItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        // 1. Query Jellyfin's registered remote image providers (TheMovieDb / TMDb, TheTVDB, Fanart.tv, etc.)
        if (_providerManager is not null)
        {
            try
            {
                var query = new RemoteImageQuery(string.Empty)
                {
                    ImageType = ImageType.Primary,
                    IncludeDisabledProviders = false,
                    IncludeAllLanguages = true
                };

                var remoteImages = await _providerManager.GetAvailableRemoteImages(item, query, cancellationToken).ConfigureAwait(false);
                if (remoteImages is not null)
                {
                    var candidates = remoteImages
                        .Where(img => img.Type == ImageType.Primary && !string.IsNullOrWhiteSpace(img.Url))
                        .ToList();

                    // Prioritize English or neutral language, then highest resolution
                    var chosenImage = candidates.FirstOrDefault(img => string.Equals(img.Language, "en", StringComparison.OrdinalIgnoreCase))
                        ?? candidates.FirstOrDefault(img => string.IsNullOrEmpty(img.Language))
                        ?? candidates.FirstOrDefault();

                    if (chosenImage is not null && !string.IsNullOrWhiteSpace(chosenImage.Url))
                    {
                        var bytes = await DownloadBytesFromUrlAsync(chosenImage.Url, cancellationToken).ConfigureAwait(false);
                        if (bytes is not null && bytes.Length > 0)
                        {
                            _logger.LogInformation("Successfully downloaded pristine poster from {ProviderName} for {ItemName}", chosenImage.ProviderName, item.Name);
                            return bytes;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to query Jellyfin remote image providers for {ItemName}", item.Name);
            }
        }

        // 2. Query btttr.cc clean textless poster via IMDb ID
        var btttrBytes = await DownloadCleanPosterAsync(item, cancellationToken).ConfigureAwait(false);
        if (btttrBytes is not null && btttrBytes.Length > 0)
        {
            return btttrBytes;
        }

        // 3. Query btttr.cc clean textless poster via TMDb ID if available
        var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
        if (!string.IsNullOrWhiteSpace(tmdbId))
        {
            var tmdbUrl = $"https://btttr.cc/none-none-none-none-none/tmdb/poster-clean/{Uri.EscapeDataString(tmdbId)}.jpg";
            var bytes = await DownloadBytesFromUrlAsync(tmdbUrl, cancellationToken).ConfigureAwait(false);
            if (bytes is not null && bytes.Length > 0)
            {
                _logger.LogInformation("Successfully downloaded clean textless poster from btttr.cc (TMDb: {TmdbId}) for {ItemName}", tmdbId, item.Name);
                return bytes;
            }
        }

        return null;
    }

    /// <summary>
    /// Re-downloads a fresh clean poster from remote providers and saves it as the pristine backup, overwriting any previous (potentially contaminated) backup.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if clean poster was successfully downloaded and saved, otherwise false.</returns>
    public async Task<bool> ForceRefreshPristineBackupAsync(
        BaseItem item,
        PluginConfiguration config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(config);

        var cleanBytes = await DownloadCleanRemotePosterAsync(item, cancellationToken).ConfigureAwait(false);
        if (cleanBytes is null || cleanBytes.Length == 0)
        {
            _logger.LogWarning("Unable to find remote clean poster for {ItemName} ({ItemId})", item.Name, item.Id);
            return false;
        }

        return await SaveBytesToBackupAsync(item, config, cleanBytes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Takes the current primary poster set in Jellyfin and establishes it as the pristine backup (poster-original.jpg).
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if current poster was read and saved to backup, otherwise false.</returns>
    public async Task<bool> SnapshotCurrentAsBackupAsync(
        BaseItem item,
        PluginConfiguration config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(config);

        var localBytes = ReadCurrentLocalPoster(item);
        if (localBytes is null || localBytes.Length == 0)
        {
            _logger.LogWarning("No local primary image found for {ItemName} ({ItemId}) to snapshot", item.Name, item.Id);
            return false;
        }

        return await SaveBytesToBackupAsync(item, config, localBytes, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> SaveBytesToBackupAsync(
        BaseItem item,
        PluginConfiguration config,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        var backupPath = GetBackupFilePath(item, config);
        try
        {
            var directory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(backupPath, bytes, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Saved backup for {ItemName} to {BackupPath}", item.Name, backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save backup beside media for {ItemName}. Saving to fallback directory.", item.Name);
            try
            {
                if (!Directory.Exists(FallbackBackupDirectory))
                {
                    Directory.CreateDirectory(FallbackBackupDirectory);
                }

                backupPath = Path.Combine(FallbackBackupDirectory, $"{item.Id}.jpg");
                await File.WriteAllBytesAsync(backupPath, bytes, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to write backup to fallback location for {ItemName}", item.Name);
                return false;
            }
        }
    }

    private async Task<byte[]?> DownloadBytesFromUrlAsync(string url, CancellationToken cancellationToken)
    {
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
            _logger.LogDebug(ex, "Failed to download image from URL {Url}", url);
        }

        return null;
    }
}
