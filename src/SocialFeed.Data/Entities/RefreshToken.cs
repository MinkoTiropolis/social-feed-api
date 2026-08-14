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

    /// <summary>
    /// SHA-256 hash of the issued token, never the token itself. A refresh token is a
    /// credential: storing it in plain text would mean read access to the database is enough
    /// to take over live sessions. The value has high entropy already, so a fast hash is
    /// appropriate here — unlike a password, it is not guessable.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Set when the user logs out. A revoked token can no longer be exchanged for a new
    /// access token.
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}
