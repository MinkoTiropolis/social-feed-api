namespace SocialFeed.Data.Entities;

public class Post
{
    public int Id { get; set; }

    public int AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the post was soft deleted, or <c>null</c> while it is live.
    /// <para>
    /// This is a timestamp rather than a boolean flag because the retention rule is expressed
    /// in time: the background job hard deletes posts that were soft deleted more than the
    /// configured number of days ago. A boolean could not answer that question.
    /// </para>
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
