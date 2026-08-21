using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;
using SocialFeed.Services.Results;

namespace SocialFeed.Services;

public class PostService : IPostService
{
    private readonly AppDbContext _db;

    public PostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> CreateAsync(int authorId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = new Post
        {
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        // Read it back through the same projection every other endpoint uses, so a newly
        // created post looks identical to the same post seen later in the feed.
        return (await GetByIdAsync(post.Id, authorId, cancellationToken))!;
    }

    /// <summary>
    /// Returns a single post, or null when it does not exist or has been soft deleted. The
    /// global query filter handles the deleted case, so it needs no mention here.
    /// </summary>
    public async Task<PostResponse?> GetByIdAsync(int postId, int currentUserId, CancellationToken cancellationToken)
    {
        return await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                Author = new AuthorSummary
                {
                    Id = p.Author.Id,
                    Name = p.Author.Name,
                    Description = p.Author.Description,
                    ProfilePictureUrl = p.Author.ProfilePicturePath
                },
                LikeCount = p.Likes.Count(),
                LikedByMe = p.Likes.Any(l => l.UserId == currentUserId)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Likes a post. Returns false when the post does not exist or is soft deleted. Liking a
    /// post twice succeeds and leaves a single row.
    /// </summary>
    public async Task<bool> LikeAsync(int postId, int userId, CancellationToken cancellationToken)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
        {
            return false;
        }

        _db.PostLikes.Add(new PostLike
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The composite primary key rejected a second like from the same user. That is
            // the outcome the caller asked for, so it is a success, not an error. Doing it
            // this way rather than checking first also removes the race between the two.
            _db.ChangeTracker.Clear();
        }

        return true;
    }

    /// <summary>
    /// Removes a like. Returns false when the post does not exist or is soft deleted.
    /// Unliking a post that was not liked succeeds and changes nothing.
    /// </summary>
    public async Task<bool> UnlikeAsync(int postId, int userId, CancellationToken cancellationToken)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
        {
            return false;
        }

        var like = await _db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (like is null)
        {
            return true;
        }

        _db.PostLikes.Remove(like);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Soft deletes a post by stamping DeletedAt. The row stays in the database and can be
    /// restored until the purge job removes it.
    /// </summary>
    public async Task<PostMutationResult> SoftDeleteAsync(int postId, int currentUserId, bool isSuperuser, CancellationToken cancellationToken)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
        {
            return PostMutationResult.NotFound;
        }

        if (post.AuthorId != currentUserId && !isSuperuser)
        {
            return PostMutationResult.Forbidden;
        }

        post.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return PostMutationResult.Success;
    }

    /// <summary>
    /// Restores a soft deleted post by clearing DeletedAt.
    /// <para>
    /// This is one of only two places that call IgnoreQueryFilters: the post it needs to find
    /// is precisely the one the global filter is hiding.
    /// </para>
    /// </summary>
    public async Task<PostMutationResult> RestoreAsync(int postId, int currentUserId, bool isSuperuser, CancellationToken cancellationToken)
    {
        var post = await _db.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == postId && p.DeletedAt != null, cancellationToken);

        if (post is null)
        {
            return PostMutationResult.NotFound;
        }

        if (post.AuthorId != currentUserId && !isSuperuser)
        {
            return PostMutationResult.Forbidden;
        }

        post.DeletedAt = null;
        await _db.SaveChangesAsync(cancellationToken);

        return PostMutationResult.Success;
    }

    /// <summary>
    /// Lists the users who liked a post, most recent first. Returns null when the post does
    /// not exist or is soft deleted.
    /// <para>
    /// Offset paging is fine here, unlike in the feed: this list is short, it is opened
    /// deliberately rather than scrolled endlessly, and a like arriving mid-read shifts one
    /// row rather than corrupting an infinite scroll.
    /// </para>
    /// </summary>
    public async Task<LikersResponse?> GetLikersAsync(int postId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
        {
            return null;
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var likes = _db.PostLikes.Where(l => l.PostId == postId);

        var total = await likes.CountAsync(cancellationToken);

        var items = await likes
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuthorSummary
            {
                Id = l.User.Id,
                Name = l.User.Name,
                Description = l.User.Description,
                ProfilePictureUrl = l.User.ProfilePicturePath
            })
            .ToListAsync(cancellationToken);

        return new LikersResponse
        {
            Items = items,
            Total = total,
            HasMore = page * pageSize < total
        };
    }
}
