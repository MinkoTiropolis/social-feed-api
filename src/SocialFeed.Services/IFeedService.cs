using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public interface IFeedService
{
    /// <summary>A page of the global feed, newest first, paged by opaque cursor.</summary>
    Task<FeedResponse> GetFeedAsync(int currentUserId, string? cursor, int pageSize, CancellationToken cancellationToken);
}
