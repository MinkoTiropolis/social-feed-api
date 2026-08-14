using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        TokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    /// <summary>
    /// Creates a new account. Returns null when the email is already taken.
    /// </summary>
    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Role = UserRole.User,
            Status = AccountStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two registrations for the same address can pass the check above at the same
            // time. The unique index on Email rejects the second one, and it means exactly
            // what the check meant: the address is taken.
            return null;
        }

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            Status = user.Status.ToString()
        };
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials, null);
        }

        var passwordCheck = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordCheck == PasswordVerificationResult.Failed)
        {
            return new LoginResult(LoginOutcome.InvalidCredentials, null);
        }

        // Deliberately after the password check. Reporting "pending" to someone who does not
        // know the password would tell them the account exists.
        if (user.Status != AccountStatus.Approved)
        {
            return new LoginResult(LoginOutcome.AccountPending, null);
        }

        var now = DateTime.UtcNow;
        var refreshToken = TokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenService.HashRefreshToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResult(LoginOutcome.Success, new LoginResponse
        {
            AccessToken = _tokenService.CreateAccessToken(user),
            AccessTokenExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenMinutes),
            RefreshToken = refreshToken
        });
    }

    /// <summary>
    /// Issues a new access token for a valid refresh token. Returns null when the token is
    /// unknown, revoked, expired, or its owner is no longer approved.
    /// </summary>
    public async Task<RefreshResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenService.HashRefreshToken(request.RefreshToken);
        var now = DateTime.UtcNow;

        var storedToken = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= now)
        {
            return null;
        }

        // Approval can be withdrawn after a token was issued, so it is checked on every
        // refresh rather than only at login.
        if (storedToken.User.Status != AccountStatus.Approved)
        {
            return null;
        }

        return new RefreshResponse
        {
            AccessToken = _tokenService.CreateAccessToken(storedToken.User),
            AccessTokenExpiresAt = now.AddMinutes(_jwtOptions.AccessTokenMinutes)
        };
    }
}
