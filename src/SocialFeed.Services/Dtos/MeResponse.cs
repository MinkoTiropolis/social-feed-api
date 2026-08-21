namespace SocialFeed.Services.Dtos;

public class MeResponse
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int TotalPosts { get; set; }

    public int TotalLikes { get; set; }
}
