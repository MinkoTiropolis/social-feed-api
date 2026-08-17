namespace SocialFeed.Services.Dtos;

public class FeedResponse
{
    public List<PostResponse> Items { get; set; } = new();

    /// <summary>
    /// Pass this back as the cursor to get the next page. Null on the last page.
    /// </summary>
    public string? NextCursor { get; set; }

    public bool HasMore { get; set; }
}
