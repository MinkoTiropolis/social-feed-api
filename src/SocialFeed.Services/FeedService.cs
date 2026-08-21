using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Services.Dtos;

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
    /// <para>
    /// Paging is keyset, not offset. With OFFSET/FETCH, a post created while someone is
    /// scrolling shifts every later row down by one, so the next page repeats an item the
    /// reader has already seen and skips another. Seeking from the last item the client
    /// actually received is stable regardless of what gets inserted above it.
    /// </para>
    /// </summary>
    public async Task<FeedResponse> GetFeedAsync(int currentUserId, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        // Over-sized requests are clamped rather than rejected: the client still gets a
        // useful page instead of an error.
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = _db.Posts.AsNoTracking();

        var position = FeedCursor.Decode(cursor);

        if (position is not null)
        {
            // Strictly "older than the last item seen". The Id comparison breaks ties when
            // two posts share a timestamp, which is what stops a post being skipped or
            // repeated at a page boundary.
            query = query.Where(p =>
                p.CreatedAt < position.CreatedAt
                || (p.CreatedAt == position.CreatedAt && p.Id < position.Id));
        }

        // One more row than asked for: if it comes back, there is another page. That answers
        // hasMore without a second COUNT query over the whole table.
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
