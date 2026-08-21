using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialFeed.Data;
using SocialFeed.Services.Interfaces;
using SocialFeed.Services.Options;

namespace SocialFeed.Services;

/// <summary>
/// Removes data that has outlived its purpose: posts past the retention window, and refresh
/// tokens that can no longer be exchanged.
/// </summary>
public class PurgeService : IPurgeService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _timeProvider;
    private readonly PostRetentionOptions _options;
    private readonly ILogger<PurgeService> _logger;

    public PurgeService(
        AppDbContext db,
        TimeProvider timeProvider,
        IOptions<PostRetentionOptions> options,
        ILogger<PurgeService> logger)
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
    public async Task<int> PurgeExpiredPostsAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-_options.RetentionDays);

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

    /// <summary>
    /// Deletes refresh tokens that are past their expiry or already revoked. Both are unusable
    /// for a refresh, so removing them changes no behaviour and stops the table growing without
    /// bound.
    /// </summary>
    public async Task<int> PurgeExpiredRefreshTokensAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var purged = await _db.RefreshTokens
            .Where(t => t.ExpiresAt <= now || t.RevokedAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Purge removed {PurgedCount} expired or revoked refresh token(s).", purged);

        return purged;
    }
}
