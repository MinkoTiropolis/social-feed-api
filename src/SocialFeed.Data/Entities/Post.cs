namespace SocialFeed.Data.Entities;

public class Post
{
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
}
