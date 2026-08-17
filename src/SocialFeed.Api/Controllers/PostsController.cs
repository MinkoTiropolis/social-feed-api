using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly PostService _postService;

    public PostsController(PostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// Creates a post authored by the signed-in user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = await _postService.CreateAsync(User.GetUserId(), request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    /// <summary>
    /// Returns a single post. Soft deleted posts are reported as not found.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var post = await _postService.GetByIdAsync(id, User.GetUserId(), cancellationToken);

        if (post is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Post not found",
                Detail = $"No post exists with id {id}."
            });
        }

        return Ok(post);
    }
}
