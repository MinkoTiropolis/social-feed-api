namespace SocialFeed.Data.Entities;

/// <summary>
/// A single user's like on a single post.
/// <para>
/// The primary key is the composite <c>(PostId, UserId)</c>, configured in
/// <c>AppDbContext</c>. That makes a duplicate like impossible in the database rather than
/// something the service layer has to check for first, which would leave a race between the
/// check and the insert.
/// </para>
/// </summary>
public class PostLike
{
    public int PostId { get; set; }

    public Post Post { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
