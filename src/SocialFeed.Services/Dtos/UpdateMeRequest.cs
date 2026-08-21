using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

public class UpdateMeRequest
{
    [MinLength(1)]
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    // The stored value is returned verbatim in profiles, feed items and liker lists, so it
    // has to be an http(s) URL rather than any string a client feels like sending.
    [Url]
    [MaxLength(400)]
    public string? ProfilePictureUrl { get; set; }
}
