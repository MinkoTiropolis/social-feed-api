using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services;
using SocialFeed.Services.Options;

namespace SocialFeed.Tests;

public class PurgeExpiredPostsServiceTests
{
    private static readonly DateTime Now = new(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The rule is "hard delete posts soft deleted 10 or more days ago", so 10 is inside the
    /// purge and 9 is not. These three cases are the entire point of injecting a clock.
    /// </summary>
    [Theory]
    [InlineData(11, true)]
    [InlineData(10, true)]
    [InlineData(9, false)]
    public async Task Purge_removes_posts_past_the_retention_window_only(int daysSinceDeletion, bool shouldBePurged)
    {
        using var db = new TestDatabase();
        var author = AddAuthor(db.Context);

        db.Context.Posts.Add(new Post
        {
            Author = author,
            Content = "A soft deleted post.",
            CreatedAt = Now.AddDays(-30),
            DeletedAt = Now.AddDays(-daysSinceDeletion)
        });

        await db.Context.SaveChangesAsync();

        var purged = await CreateService(db.Context).PurgeAsync(CancellationToken.None);

        var remaining = await db.Context.Posts.IgnoreQueryFilters().CountAsync();

        Assert.Equal(shouldBePurged ? 1 : 0, purged);
        Assert.Equal(shouldBePurged ? 0 : 1, remaining);
    }

    [Fact]
    public async Task Purge_leaves_live_posts_alone()
    {
        using var db = new TestDatabase();
        var author = AddAuthor(db.Context);

        db.Context.Posts.Add(new Post
        {
            Author = author,
            Content = "Never deleted.",
            CreatedAt = Now.AddDays(-365)
        });

        await db.Context.SaveChangesAsync();

        var purged = await CreateService(db.Context).PurgeAsync(CancellationToken.None);

        Assert.Equal(0, purged);
        Assert.Equal(1, await db.Context.Posts.CountAsync());
    }

    [Fact]
    public async Task Purge_takes_the_likes_of_a_purged_post_with_it()
    {
        using var db = new TestDatabase();
        var author = AddAuthor(db.Context);

        var post = new Post
        {
            Author = author,
            Content = "Liked, then deleted long ago.",
            CreatedAt = Now.AddDays(-30),
            DeletedAt = Now.AddDays(-11)
        };

        db.Context.Posts.Add(post);
        db.Context.PostLikes.Add(new PostLike { Post = post, User = author, CreatedAt = Now.AddDays(-29) });

        await db.Context.SaveChangesAsync();
        Assert.Equal(1, await db.Context.PostLikes.IgnoreQueryFilters().CountAsync());

        await CreateService(db.Context).PurgeAsync(CancellationToken.None);

        // The cascade is a database rule, not something the service does, so this asserts the
        // foreign key is configured the way FR-D7 needs.
        Assert.Equal(0, await db.Context.PostLikes.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Purge_respects_a_configured_retention_window()
    {
        using var db = new TestDatabase();
        var author = AddAuthor(db.Context);

        db.Context.Posts.Add(new Post
        {
            Author = author,
            Content = "Deleted three days ago.",
            CreatedAt = Now.AddDays(-30),
            DeletedAt = Now.AddDays(-3)
        });

        await db.Context.SaveChangesAsync();

        // Not purged under the default ten days, purged under two.
        Assert.Equal(0, await CreateService(db.Context).PurgeAsync(CancellationToken.None));
        Assert.Equal(1, await CreateService(db.Context, retentionDays: 2).PurgeAsync(CancellationToken.None));
    }

    private static User AddAuthor(AppDbContext context)
    {
        var author = new User
        {
            Email = "author@sharebook.local",
            PasswordHash = "irrelevant",
            Name = "Author",
            Status = AccountStatus.Approved,
            CreatedAt = Now.AddDays(-100)
        };

        context.Users.Add(author);

        return author;
    }

    private static PurgeExpiredPostsService CreateService(AppDbContext context, int retentionDays = 10)
    {
        return new PurgeExpiredPostsService(
            context,
            new FixedTimeProvider(Now),
            Options.Create(new PostRetentionOptions { RetentionDays = retentionDays }),
            NullLogger<PurgeExpiredPostsService>.Instance);
    }
}
