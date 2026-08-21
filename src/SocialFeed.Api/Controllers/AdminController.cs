using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialFeed.Services;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Api.Controllers;

[ApiController]
[Route("admin/users")]
[Authorize(Policy = AuthorizationPolicies.SuperuserOnly)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// Lists accounts waiting for approval, oldest first.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PendingUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetPendingUsersAsync(cancellationToken));
    }

    /// <summary>
    /// Approves an account so its owner can log in.
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var approved = await _adminService.ApproveUserAsync(id, cancellationToken);

        if (!approved)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "User not found",
                Detail = $"No user exists with id {id}."
            });
        }

        return NoContent();
    }
}
