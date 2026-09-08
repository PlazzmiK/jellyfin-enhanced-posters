namespace PostersEnhanced.Configuration;

/// <summary>
/// Serializable key-value pair for custom uploaded badge images.
/// </summary>
public class CustomBadgeEntry
{
    /// <summary>Gets or sets the badge key (e.g. "dv", "hdr", "4k", "3d").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the image data URI (e.g. data:image/png;base64,...).</summary>
    public string Data { get; set; } = string.Empty;
}
