using Microsoft.Extensions.Options;
using SocialFeed.Services;
using SocialFeed.Services.Interfaces;
using SocialFeed.Services.Options;

namespace SocialFeed.Api;

/// <summary>
/// Runs the purge on a schedule for as long as the application is running.
/// </summary>
public class PurgeExpiredPostsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PostRetentionOptions _options;
    private readonly ILogger<PurgeExpiredPostsWorker> _logger;

    public PurgeExpiredPostsWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<PostRetentionOptions> options,
        ILogger<PurgeExpiredPostsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.RunIntervalHours));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var purgeService = scope.ServiceProvider.GetRequiredService<IPurgeExpiredPostsService>();

                await purgeService.PurgeAsync(stoppingToken);
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
