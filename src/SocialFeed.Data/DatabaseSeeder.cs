using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialFeed.Data.Entities;

namespace SocialFeed.Data;

/// <summary>
/// Fills an empty database with a superuser, a few approved users, one account still waiting
/// for approval, and some posts and likes, so the feed is not empty on first run.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var admin = CreateUser(passwordHasher, "admin@sharebook.local", "Admin123!", "Ivaylo Bachvarov", "CTO, Waracle", UserRole.Superuser, AccountStatus.Approved, now);
        var daniel = CreateUser(passwordHasher, "daniel@sharebook.local", "User123!", "Daniel Goshev", "Software Developer, Waracle", UserRole.User, AccountStatus.Approved, now);
        var maria = CreateUser(passwordHasher, "maria@sharebook.local", "User123!", "Maria Petrova", "Product Designer, Waracle", UserRole.User, AccountStatus.Approved, now);

        // Left pending on purpose: it lets a reviewer see a blocked login and then the
        // superuser approval flow without registering an account first.
        var pending = CreateUser(passwordHasher, "pending@sharebook.local", "User123!", "Georgi Ivanov", "QA Engineer, Waracle", UserRole.User, AccountStatus.Pending, now);

        db.Users.AddRange(admin, daniel, maria, pending);

        var posts = new List<Post>
        {
            new() { Author = daniel, Content = "Despite our total project numbers only increasing by 2% compared to last month, the 58 projects we are working on contain a significant increase in deliverables.", CreatedAt = now.AddDays(-3) },
            new() { Author = maria, Content = "Spent the morning reworking the profile screen. Fewer fields, bigger tap targets, and the description finally has room to breathe.", CreatedAt = now.AddDays(-2) },
            new() { Author = daniel, Content = "Reminder that a deleted post here is only hidden, not gone. It stays recoverable for ten days.", CreatedAt = now.AddHours(-20) },
            new() { Author = admin, Content = "Welcome to sharebook. Say hello, share what you are working on, and be kind to each other.", CreatedAt = now.AddMinutes(-20) }
        };

        db.Posts.AddRange(posts);

        db.PostLikes.AddRange(
            new PostLike { Post = posts[0], User = maria, CreatedAt = now.AddDays(-2) },
            new PostLike { Post = posts[0], User = admin, CreatedAt = now.AddDays(-2) },
            new PostLike { Post = posts[1], User = daniel, CreatedAt = now.AddDays(-1) },
            new PostLike { Post = posts[3], User = daniel, CreatedAt = now.AddMinutes(-10) },
            new PostLike { Post = posts[3], User = maria, CreatedAt = now.AddMinutes(-5) });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(
        IPasswordHasher<User> passwordHasher,
        string email,
        string password,
        string name,
        string description,
        UserRole role,
        AccountStatus status,
        DateTime createdAt)
    {
        var user = new User
        {
            Email = email,
            Name = name,
            Description = description,
            Role = role,
            Status = status,
            CreatedAt = createdAt
        };

        user.PasswordHash = passwordHasher.HashPassword(user, password);

        return user;
    }
}
