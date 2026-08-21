namespace SocialFeed.Services.Options;

public class PostRetentionOptions
{
    public const string SectionName = "PostRetention";

    /// <summary>
    /// How long a soft deleted post can still be restored. After this it is removed for good.
    /// </summary>
    public int RetentionDays { get; set; } = 10;

    /// <summary>
    /// How often the purge runs.
    /// </summary>
    public int RunIntervalHours { get; set; } = 24;
}
