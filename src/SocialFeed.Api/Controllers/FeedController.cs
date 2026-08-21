using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IFeedService _feedService;

    public FeedController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    /// <summary>
    /// Returns a page of all published posts, newest first. Omit the cursor for the first
    /// page, then pass back the nextCursor from the previous response.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FeedResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken,
        [FromQuery] string? cursor = null,
        [FromQuery] int pageSize = FeedService.DefaultPageSize)
    {
        var feed = await _feedService.GetFeedAsync(User.GetUserId(), cursor, pageSize, cancellationToken);

        return Ok(feed);
    }
}
