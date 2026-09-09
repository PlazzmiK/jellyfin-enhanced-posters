namespace EnhancedPosters.Configuration;

/// <summary>
/// Serializable entry defining a rating score threshold and badge color.
/// </summary>
public class RatingTierEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RatingTierEntry"/> class.
    /// </summary>
    public RatingTierEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingTierEntry"/> class with initial values.
    /// </summary>
    /// <param name="minScore">The minimum score.</param>
    /// <param name="maxScore">The maximum score.</param>
    /// <param name="color">The hex color.</param>
    /// <param name="name">The optional display name.</param>
    public RatingTierEntry(float minScore, float maxScore, string color, string name = "")
    {
        MinScore = minScore;
        MaxScore = maxScore;
        Color = color;
        Name = name;
    }

    /// <summary>Gets or sets the minimum score (inclusive).</summary>
    public float MinScore { get; set; }

    /// <summary>Gets or sets the maximum score (inclusive).</summary>
    public float MaxScore { get; set; }

    /// <summary>Gets or sets the hex color for this score tier.</summary>
    public string Color { get; set; } = "#5BC4F0";

    /// <summary>Gets or sets an optional display name or label for this tier.</summary>
    public string Name { get; set; } = string.Empty;
}
