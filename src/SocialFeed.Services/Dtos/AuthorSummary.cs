namespace SocialFeed.Services.Dtos;

public class AuthorSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
