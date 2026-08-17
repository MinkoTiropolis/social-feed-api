using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

/// <summary>
/// The fields a user may change about themselves.
/// <para>
/// There is deliberately no Email, Password, Role or Status property here. Those cannot be
/// changed through this endpoint because the request model has nowhere to put them, rather
/// than because some code remembers to ignore them.
/// </para>
/// <para>
/// A field left out of the request, or sent as null, is left unchanged.
/// </para>
/// </summary>
public class UpdateMeRequest
{
    [MinLength(1)]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(400)]
    public string? ProfilePictureUrl { get; set; }
}
