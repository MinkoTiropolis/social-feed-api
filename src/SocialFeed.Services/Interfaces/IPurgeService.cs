namespace SocialFeed.Services.Interfaces;

public interface IPurgeService
{
    /// <summary>
    /// Hard deletes posts soft deleted longer ago than the retention window, and returns how many rows were removed.
    /// </summary>
    Task<int> PurgeExpiredPostsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes refresh tokens that can no longer be used, and returns how many rows were removed.
    /// </summary>
    Task<int> PurgeExpiredRefreshTokensAsync(CancellationToken cancellationToken);
}
