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

    /// <summary>Number of the user's posts. Soft deleted posts are not counted.</summary>
    public int TotalPosts { get; set; }

    /// <summary>Likes received across all of the user's posts, again excluding deleted ones.</summary>
    public int TotalLikes { get; set; }
}
