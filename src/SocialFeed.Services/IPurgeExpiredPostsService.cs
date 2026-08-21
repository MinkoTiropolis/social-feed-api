namespace SocialFeed.Services;

public interface IPurgeExpiredPostsService
{
    /// <summary>
    /// Hard deletes posts soft deleted longer ago than the retention window, and returns how
    /// many rows were removed.
    /// </summary>
    Task<int> PurgeAsync(CancellationToken cancellationToken);
}
