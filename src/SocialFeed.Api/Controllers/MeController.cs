using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly ProfileService _profileService;

    public MeController(ProfileService profileService)
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

        return me is null ? NotFound() : Ok(me);
    }
}
