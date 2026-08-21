using Microsoft.Extensions.Options;
using SocialFeed.Services;
using SocialFeed.Services.Interfaces;
using SocialFeed.Services.Options;

namespace SocialFeed.Api;

/// <summary>
/// Runs the purge on a schedule for as long as the application is running.
/// </summary>
public class PurgeWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PostRetentionOptions _options;
    private readonly ILogger<PurgeWorker> _logger;

    public PurgeWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PostRetentionOptions> options,
        ILogger<PurgeWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer rejects a period of zero or less, which would kill the worker at
        // startup over a configuration typo. An hour is the shortest sensible floor.
        var interval = TimeSpan.FromHours(Math.Max(_options.RunIntervalHours, 1));

        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var purgeService = scope.ServiceProvider.GetRequiredService<IPurgeService>();

                await purgeService.PurgeExpiredPostsAsync(stoppingToken);
                await purgeService.PurgeExpiredRefreshTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The scheduled purge failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
