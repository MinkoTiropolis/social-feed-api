using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Services;

public class FeedService : IFeedService
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private readonly AppDbContext _db;

    public FeedService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns a page of the global feed, newest first.
    /// </summary>
    public async Task<FeedResponse> GetFeedAsync(int currentUserId, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.Posts.AsNoTracking();

        var position = FeedCursor.Decode(cursor);

        if (position is not null)
        {
            query = query.Where(p =>
                p.CreatedAt < position.CreatedAt
                || (p.CreatedAt == position.CreatedAt && p.Id < position.Id));
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(pageSize + 1)
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
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;

        if (hasMore)
        {
            items.RemoveAt(pageSize);
        }

        var last = items.LastOrDefault();

        return new FeedResponse
        {
            Items = items,
            HasMore = hasMore,
            NextCursor = hasMore && last is not null
                ? new FeedCursor(last.CreatedAt, last.Id).Encode()
                : null
        };
    }
}
