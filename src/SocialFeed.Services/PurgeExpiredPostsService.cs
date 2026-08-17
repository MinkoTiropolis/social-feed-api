using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialFeed.Data;

namespace SocialFeed.Services;

/// <summary>
/// Removes posts that were soft deleted longer ago than the retention window.
/// <para>
/// The time comes from an injected <see cref="TimeProvider"/> rather than DateTime.UtcNow, so
/// a test can place a post exactly 9, 10 or 11 days in the past and assert what happens at the
/// boundary. Calling the clock directly would make that untestable.
/// </para>
/// </summary>
public class PurgeExpiredPostsService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly PostRetentionOptions _options;
    private readonly ILogger<PurgeExpiredPostsService> _logger;

    public PurgeExpiredPostsService(
        AppDbContext db,
        TimeProvider timeProvider,
        IOptions<PostRetentionOptions> options,
        ILogger<PurgeExpiredPostsService> logger)
    {
        _db = db;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Deletes expired posts and returns how many rows went. Their likes go with them through
    /// the cascade configured on the foreign key, so no orphan rows are left behind.
    /// </summary>
    public async Task<int> PurgeAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);

        // IgnoreQueryFilters is required: the rows to delete are exactly the ones the global
        // filter hides.
        var purged = await _db.Posts
            .IgnoreQueryFilters()
            .Where(p => p.DeletedAt != null && p.DeletedAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Purge removed {PurgedCount} post(s) soft deleted before {Cutoff:u}.",
            purged,
            cutoff);

        return purged;
    }
}
