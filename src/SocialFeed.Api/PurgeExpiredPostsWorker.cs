using Microsoft.Extensions.Options;
using SocialFeed.Services;

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

        // Runs once at startup, then on every tick.
        do
        {
            try
            {
                // A BackgroundService is a singleton and AppDbContext is scoped, so the
                // context cannot be injected into this class. Each run gets its own scope,
                // and therefore its own context, which is disposed when the run ends.
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
                // A failed run must not take the worker down; the next tick tries again.
                _logger.LogError(ex, "The scheduled purge failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
