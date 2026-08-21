using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IProfileService _profileService;

    public MeController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    /// <summary>
    /// Returns the signed-in user's profile, including total posts and total likes received.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var me = await _profileService.GetMeAsync(User.GetUserId(), cancellationToken);

        return me is null ? UserNotFound() : Ok(me);
    }

    /// <summary>
    /// Updates the signed-in user's name, description and profile picture. Email and password cannot be changed here.
    /// </summary>
    [HttpPatch]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(UpdateMeRequest request, CancellationToken cancellationToken)
    {
        var me = await _profileService.UpdateMeAsync(User.GetUserId(), request, cancellationToken);

        return me is null ? UserNotFound() : Ok(me);
    }

    private NotFoundObjectResult UserNotFound()
    {
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "User not found",
            Detail = "The account this token belongs to no longer exists."
        });
    }
}
