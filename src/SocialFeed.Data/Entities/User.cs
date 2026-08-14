namespace SocialFeed.Data.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Salted hash of the password. Never leaves the data layer and never appears in a response.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short free-text line shown under the name, for example "Software Developer, Waracle".
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Storage key for the uploaded picture, not a URL. The absolute URL is built by the
    /// service layer so the storage implementation can change without a data migration.
    /// </summary>
    public string? ProfilePicturePath { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>
    /// New registrations are sandboxed: an account stays <see cref="AccountStatus.Pending"/>
    /// and cannot log in until a superuser approves it.
    /// </summary>
    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();

    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
}
