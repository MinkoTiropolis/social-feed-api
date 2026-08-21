namespace SocialFeed.Services.Dtos;

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; set; }
}
