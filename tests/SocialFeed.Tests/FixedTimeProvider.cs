namespace SocialFeed.Tests;

/// <summary>
/// A clock stuck at one instant, so tests can place data an exact number of days in the
/// past and assert what happens at a boundary.
/// </summary>
public class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
