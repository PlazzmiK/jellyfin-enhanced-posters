using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnhancedPosters.Configuration;
using EnhancedPosters.Metadata;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace EnhancedPosters.Storage;

/// <summary>
/// Tracks render stamps and output image attributes to ensure posters are re-rendered when
/// settings change or when underlying artwork is refreshed externally.
/// </summary>
public class RenderStampTracker
{
    private readonly string _stampFilePath;
    private readonly string _outputStampsFilePath;
    private readonly ILogger<RenderStampTracker> _logger;
    private readonly ConcurrentDictionary<string, string> _stamps = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, OutputImageStamp> _outputStamps = new(StringComparer.Ordinal);
    private readonly object _saveLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderStampTracker"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public RenderStampTracker(IApplicationPaths applicationPaths, ILogger<RenderStampTracker> logger)
    {
        _logger = logger;
        var dir = Path.Combine(applicationPaths.PluginConfigurationsPath, "EnhancedPosters");
        var oldDir = Path.Combine(applicationPaths.PluginConfigurationsPath, "PostersEnhanced");
        if (!Directory.Exists(dir) && Directory.Exists(oldDir))
        {
            dir = oldDir;
        }

        _stampFilePath = Path.Combine(dir, "render_stamps.json");
        _outputStampsFilePath = Path.Combine(dir, "output_stamps.json");

        LoadStamps();
    }

    /// <summary>
    /// Determines whether an item needs to be re-rendered based on current configuration and media metadata.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="mediaInfo">The extracted media information.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="sourceFilePath">The pristine source file path.</param>
    /// <returns>True if re-rendering is needed, false to skip.</returns>
    public bool NeedsReRender(
        BaseItem item,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config,
        string? sourceFilePath)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(mediaInfo);
        ArgumentNullException.ThrowIfNull(config);

        var currentStamp = ComputeStamp(item, mediaInfo, config, sourceFilePath);
        var itemIdKey = item.Id.ToString("N");

        if (_stamps.TryGetValue(itemIdKey, out var existingStamp))
        {
            return !string.Equals(existingStamp, currentStamp, StringComparison.Ordinal);
        }

        return true;
    }

    /// <summary>
    /// Checks whether the item's primary poster image was modified externally (e.g. by a metadata refresh
    /// or user image upload in Jellyfin), indicating that new base artwork should replace the pristine backup.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="currentPrimaryPath">The current local path to the primary image.</param>
    /// <param name="backupPath">The path to the existing pristine backup file, if any.</param>
    /// <returns>True if the primary image was externally updated, false otherwise.</returns>
    public bool IsPrimaryImageExternallyModified(
        Guid itemId,
        string? currentPrimaryPath,
        string? backupPath)
    {
        if (string.IsNullOrEmpty(currentPrimaryPath) || !File.Exists(currentPrimaryPath))
        {
            return false;
        }

        var itemIdKey = itemId.ToString("N");
        var currentInfo = new FileInfo(currentPrimaryPath);

        if (_outputStamps.TryGetValue(itemIdKey, out var entry))
        {
            var diffTicks = Math.Abs(currentInfo.LastWriteTimeUtc.Ticks - entry.LastWriteTimeTicks);
            if (diffTicks > TimeSpan.FromSeconds(2).Ticks || currentInfo.Length != entry.FileLength)
            {
                return true;
            }

            return false;
        }

        // Fallback for items with existing backups but no recorded output stamp:
        // If current primary image was written after the backup (+ 1 min tolerance for initial creation),
        // it was updated externally by metadata refresh.
        if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
        {
            var backupInfo = new FileInfo(backupPath);
            if (currentInfo.LastWriteTimeUtc > backupInfo.LastWriteTimeUtc.AddMinutes(1))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Records the output attributes of a newly composited primary image.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    /// <param name="primaryImagePath">The local path to the generated primary image.</param>
    public void RecordOutputImage(Guid itemId, string? primaryImagePath)
    {
        if (string.IsNullOrEmpty(primaryImagePath) || !File.Exists(primaryImagePath))
        {
            return;
        }

        var fileInfo = new FileInfo(primaryImagePath);
        var itemIdKey = itemId.ToString("N");
        _outputStamps[itemIdKey] = new OutputImageStamp
        {
            FilePath = primaryImagePath,
            LastWriteTimeTicks = fileInfo.LastWriteTimeUtc.Ticks,
            FileLength = fileInfo.Length
        };
    }

    /// <summary>
    /// Clears the recorded output image stamp for an item.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    public void ClearOutputImage(Guid itemId)
    {
        _outputStamps.TryRemove(itemId.ToString("N"), out _);
    }

    /// <summary>
    /// Records that an item has been successfully rendered with the current stamp.
    /// </summary>
    /// <param name="item">The library item.</param>
    /// <param name="mediaInfo">The extracted media information.</param>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="sourceFilePath">The pristine source file path.</param>
    public void RecordRender(
        BaseItem item,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config,
        string? sourceFilePath)
    {
        var currentStamp = ComputeStamp(item, mediaInfo, config, sourceFilePath);
        var itemIdKey = item.Id.ToString("N");
        _stamps[itemIdKey] = currentStamp;
    }

    /// <summary>
    /// Clears the recorded render stamp for an item.
    /// </summary>
    /// <param name="itemId">The item identifier.</param>
    public void ClearRender(Guid itemId)
    {
        _stamps.TryRemove(itemId.ToString("N"), out _);
    }

    /// <summary>
    /// Persists recorded stamps and output image tracking to disk.
    /// </summary>
    public void Save()
    {
        lock (_saveLock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_stampFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var jsonStamps = JsonSerializer.Serialize(_stamps);
                File.WriteAllText(_stampFilePath, jsonStamps);

                var jsonOutputs = JsonSerializer.Serialize(_outputStamps);
                File.WriteAllText(_outputStampsFilePath, jsonOutputs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save render stamps to {StampFilePath}", _stampFilePath);
            }
        }
    }

    private void LoadStamps()
    {
        try
        {
            if (File.Exists(_stampFilePath))
            {
                var json = File.ReadAllText(_stampFilePath);
                var loaded = JsonSerializer.Deserialize<ConcurrentDictionary<string, string>>(json);
                if (loaded is not null)
                {
                    foreach (var pair in loaded)
                    {
                        _stamps[pair.Key] = pair.Value;
                    }
                }
            }

            if (File.Exists(_outputStampsFilePath))
            {
                var jsonOutputs = File.ReadAllText(_outputStampsFilePath);
                var loadedOutputs = JsonSerializer.Deserialize<ConcurrentDictionary<string, OutputImageStamp>>(jsonOutputs);
                if (loadedOutputs is not null)
                {
                    foreach (var pair in loadedOutputs)
                    {
                        _outputStamps[pair.Key] = pair.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read existing render stamps from {StampFilePath}", _stampFilePath);
        }
    }

    private static string ComputeStamp(
        BaseItem item,
        ExtractedMediaInfo mediaInfo,
        PluginConfiguration config,
        string? sourceFilePath)
    {
        var sb = new StringBuilder();
        sb.Append(typeof(Plugin).Assembly.GetName().Version?.ToString() ?? "1.0.0.0").Append('|');
        sb.Append(config.ComputeConfigHash()).Append('|');
        sb.Append((int)mediaInfo.Resolution).Append('|');
        sb.Append((int)mediaInfo.HdrType).Append('|');
        sb.Append((int)mediaInfo.AudioCodec).Append('|');
        sb.Append(mediaInfo.Rating.HasValue ? mediaInfo.Rating.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : "none").Append('|');

        if (!string.IsNullOrEmpty(sourceFilePath) && File.Exists(sourceFilePath))
        {
            try
            {
                sb.Append(File.GetLastWriteTimeUtc(sourceFilePath).Ticks);
            }
            catch
            {
                sb.Append(sourceFilePath);
            }
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }
}
