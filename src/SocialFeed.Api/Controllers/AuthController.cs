using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
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
}
