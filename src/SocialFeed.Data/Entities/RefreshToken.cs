namespace SocialFeed.Data.Entities;

/// <summary>
/// A refresh token issued to a user, persisted so that it can be revoked.
/// <para>
/// Access tokens are stateless JWTs and stay valid until they expire, so logout can only mean
/// something if the refresh token lives server side. Logging out revokes the row, which stops
/// the session from being extended; the short access token lifetime closes the remaining gap.
/// </para>
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
