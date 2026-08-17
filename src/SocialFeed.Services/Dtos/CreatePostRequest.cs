using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

/// <summary>
/// Only the content. The author is taken from the caller's token, never from the request.
/// </summary>
public class CreatePostRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(280)]
    public string Content { get; set; } = string.Empty;
}
