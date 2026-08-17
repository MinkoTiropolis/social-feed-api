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

        return post is null ? PostNotFound(id) : Ok(post);
    }

    /// <summary>
    /// Likes a post. Calling it more than once is not an error.
    /// </summary>
    [HttpPost("{id:int}/like")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Like(int id, CancellationToken cancellationToken)
    {
        var liked = await _postService.LikeAsync(id, User.GetUserId(), cancellationToken);

        return liked ? NoContent() : PostNotFound(id);
    }

    /// <summary>
    /// Removes a like. Calling it when the post was not liked is not an error.
    /// </summary>
    [HttpDelete("{id:int}/like")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unlike(int id, CancellationToken cancellationToken)
    {
        var unliked = await _postService.UnlikeAsync(id, User.GetUserId(), cancellationToken);

        return unliked ? NoContent() : PostNotFound(id);
    }

    /// <summary>
    /// Lists the users who liked a post, most recent first.
    /// </summary>
    [HttpGet("{id:int}/likes")]
    [ProducesResponseType(typeof(LikersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLikers(
        int id,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var likers = await _postService.GetLikersAsync(id, page, pageSize, cancellationToken);

        return likers is null ? PostNotFound(id) : Ok(likers);
    }

    /// <summary>
    /// Soft deletes a post. Only its author or a superuser may do this.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _postService.SoftDeleteAsync(id, User.GetUserId(), User.IsSuperuser(), cancellationToken);

        return MapResult(result, id);
    }

    /// <summary>
    /// Restores a soft deleted post. Only its author or a superuser may do this.
    /// </summary>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _postService.RestoreAsync(id, User.GetUserId(), User.IsSuperuser(), cancellationToken);

        return MapResult(result, id);
    }

    private IActionResult MapResult(PostMutationResult result, int id)
    {
        return result switch
        {
            PostMutationResult.Success => NoContent(),
            PostMutationResult.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Not allowed",
                Detail = "Only the author of a post or a superuser can do this."
            }),
            _ => PostNotFound(id)
        };
    }

    private NotFoundObjectResult PostNotFound(int id)
    {
        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Post not found",
            Detail = $"No post exists with id {id}."
        });
    }
}
