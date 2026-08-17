namespace SocialFeed.Services.Dtos;

/// <summary>
/// The author fields each feed card shows: avatar, name, and the line underneath it.
/// </summary>
public class AuthorSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ProfilePictureUrl { get; set; }
}
