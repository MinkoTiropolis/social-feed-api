namespace SocialFeed.Services.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Signing key. The value in appsettings.json is for local development only; a real
    /// deployment overrides it with the Jwt__Key environment variable.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Access tokens are short lived because they cannot be revoked once issued.
    /// </summary>
    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}
