namespace SocialFeed.Services.Dtos;

public class LikersResponse
{
    public List<AuthorSummary> Items { get; set; } = new();

    public int Total { get; set; }

    public bool HasMore { get; set; }
}
