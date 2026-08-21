using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;
using SocialFeed.Services.Results;

namespace SocialFeed.Services;

public class PostService : IPostService
{
    public const int DefaultLikersPageSize = 20;
    public const int MaxLikersPageSize = 100;

    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;

    public PostService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<PostResponse> CreateAsync(int authorId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = new Post
        {
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(post.Id, authorId, cancellationToken))!;
    }

    /// <summary>
    /// Returns a single post, or null when it does not exist or has been soft deleted
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

        var entry = _db.PostLikes.Add(new PostLike
        {
            PostId = postId,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The composite key rejected a duplicate like, which is the outcome the caller
            // wanted. Detach only the rejected row rather than clearing the whole context.
            entry.State = EntityState.Detached;
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

        post.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);

        return PostMutationResult.Success;
    }

    /// <summary>
    /// Restores a soft deleted post by clearing DeletedAt.
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
    /// Lists the users who liked a post, most recent first. Returns null when the post does not exist or is soft deleted.
    /// </summary>
    public async Task<LikersResponse?> GetLikersAsync(int postId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await _db.Posts.AnyAsync(p => p.Id == postId, cancellationToken))
        {
            return null;
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxLikersPageSize);

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
