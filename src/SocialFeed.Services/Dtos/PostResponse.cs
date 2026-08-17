namespace SocialFeed.Services.Dtos;

public class PostResponse
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public AuthorSummary Author { get; set; } = new();

    public int LikeCount { get; set; }

    /// <summary>
    /// Whether the caller has liked this post. The UI needs it to draw the button as "Like"
    /// or "Liked".
    /// </summary>
    public bool LikedByMe { get; set; }
}
