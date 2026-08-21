using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

public class UpdateMeRequest
{
    [MinLength(1)]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Url]
    [MaxLength(400)]
    public string? ProfilePictureUrl { get; set; }
}
