namespace SocialFeed.Data.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ProfilePicturePath { get; set; }

    public UserRole Role { get; set; } = UserRole.User;

    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public ICollection<Post> Posts { get; set; } = new List<Post>();

    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
