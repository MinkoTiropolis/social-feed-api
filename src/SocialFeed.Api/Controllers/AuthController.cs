using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new account. The account is created pending and cannot log in until a
    /// superuser approves it.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);

        if (response is null)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email already registered",
                Detail = "An account with this email address already exists."
            });
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Exchanges email and password for an access token and a refresh token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);

        switch (result.Outcome)
        {
            case LoginOutcome.Success:
                return Ok(result.Response);

            case LoginOutcome.AccountPending:
                var pending = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Account pending approval",
                    Detail = "This account has been registered but is waiting for a superuser to approve it."
                };
                pending.Extensions["code"] = "account_pending";
                return StatusCode(StatusCodes.Status403Forbidden, pending);

            default:
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Invalid credentials",
                    Detail = "The email or password is incorrect."
                });
        }
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshAsync(request, cancellationToken);

        if (response is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid refresh token",
                Detail = "The refresh token is unknown, expired, or has been revoked."
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Revokes the caller's refresh token, ending the session.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(User.GetUserId(), request, cancellationToken);

        // Always 204, whether or not the token was still active. Logging out twice is not an
        // error, and the response must not reveal whether a token existed.
        return NoContent();
    }
}
