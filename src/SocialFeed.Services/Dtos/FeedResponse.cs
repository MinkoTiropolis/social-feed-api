namespace SocialFeed.Services.Dtos;

public class FeedResponse
{
    public List<PostResponse> Items { get; set; } = new();

    public string? NextCursor { get; set; }

    public bool HasMore { get; set; }
}
