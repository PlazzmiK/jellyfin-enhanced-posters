namespace PostersEnhanced.Storage;

/// <summary>
/// Tracks the output attributes of composited primary images to detect external modifications.
/// </summary>
public class OutputImageStamp
{
    /// <summary>Gets or sets the primary image file path.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the last write time in UTC ticks.</summary>
    public long LastWriteTimeTicks { get; set; }

    /// <summary>Gets or sets the file length in bytes.</summary>
    public long FileLength { get; set; }
}
