using System.ComponentModel.DataAnnotations;

namespace SocialFeed.Services.Dtos;

public class RefreshRequest
{
    [Required]
    [MaxLength(128)]
    public string RefreshToken { get; set; } = string.Empty;
}
