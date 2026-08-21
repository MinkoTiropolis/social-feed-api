using System.Security.Claims;
using SocialFeed.Data.Entities;

namespace SocialFeed.Api;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The signed-in user's id, taken from the token. Endpoints read the caller's identity
    /// from here rather than from anything the client sends in a request body.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not carry a valid user id.");
    }

    public static bool IsSuperuser(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(nameof(UserRole.Superuser));
    }
}
