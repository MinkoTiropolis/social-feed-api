using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public interface IAuthService
{
    /// <summary>Creates a pending account. Returns null when the email is already taken.</summary>
    Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Authenticates a user, distinguishing bad credentials from a pending account.</summary>
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>Issues a new access token. Returns null when the refresh token is not usable.</summary>
    Task<RefreshResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    /// <summary>Revokes the caller's refresh token so the session cannot be extended.</summary>
    Task LogoutAsync(int userId, LogoutRequest request, CancellationToken cancellationToken);
}
