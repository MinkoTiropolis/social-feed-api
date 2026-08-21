using SocialFeed.Data.Entities;

namespace SocialFeed.Services;

public interface ITokenService
{
    /// <summary>Creates a signed, short-lived JWT for the given user.</summary>
    string CreateAccessToken(User user);

    /// <summary>Creates a random refresh token. This is the value handed to the client.</summary>
    string CreateRefreshToken();

    /// <summary>Hashes a refresh token for storage and lookup. Only the hash is persisted.</summary>
    string HashRefreshToken(string token);
}
