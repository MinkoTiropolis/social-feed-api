namespace SocialFeed.Services.Options;

public class PostRetentionOptions
{
    public const string SectionName = "PostRetention";

    public int RetentionDays { get; set; } = 10;

    public int RunIntervalHours { get; set; } = 24;
}
