namespace SocialFeed.Services.Dtos;

public class PostResponse
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public AuthorSummary Author { get; set; } = new();

    public int LikeCount { get; set; }

    public bool LikedByMe { get; set; }
}
