using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

public class CreatePostRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(280)]
    public string Content { get; set; } = string.Empty;
}
