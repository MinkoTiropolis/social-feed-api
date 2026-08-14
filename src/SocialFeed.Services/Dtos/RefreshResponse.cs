namespace SocialFeed.Services.Dtos;

/// <summary>
/// Only a new access token. The refresh token the client already holds stays valid until it
/// expires or the user logs out.
/// </summary>
public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; set; }
}
