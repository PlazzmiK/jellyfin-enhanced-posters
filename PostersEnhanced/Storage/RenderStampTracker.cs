using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;
using PostersEnhanced.Configuration;
using PostersEnhanced.Metadata;

namespace PostersEnhanced.Storage;

/// <summary>
/// Tracks render stamps to ensure posters are only re-rendered when settings or media change.
/// </summary>
public class RenderStampTracker
{
    private readonly string _stampFilePath;
    private readonly ILogger<RenderStampTracker> _logger;
    private readonly ConcurrentDictionary<string, string> _stamps = new(StringComparer.Ordinal);
    private readonly object _saveLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderStampTracker"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">The logger.</param>
    public RenderStampTracker(IApplicationPaths applicationPaths, ILogger<RenderStampTracker> logger)
    {
        _logger = logger;
        var dir = Path.Combine(applicationPaths.PluginConfigurationsPath, "PostersEnhanced");
        _stampFilePath = Path.Combine(dir, "render_stamps.json");

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
    /// Persists recorded stamps to disk.
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

                var json = JsonSerializer.Serialize(_stamps);
                File.WriteAllText(_stampFilePath, json);
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
