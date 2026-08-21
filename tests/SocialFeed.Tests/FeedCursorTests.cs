using System.Text;
using SocialFeed.Services;

namespace SocialFeed.Tests;

public class FeedCursorTests
{
    [Fact]
    public void Round_trips_a_cursor()
    {
        var original = new FeedCursor(new DateTime(2026, 8, 17, 12, 30, 0, DateTimeKind.Utc), 42);

        var decoded = FeedCursor.Decode(original.Encode());

        Assert.Equal(original, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-base64!!")]
    public void Returns_null_for_a_value_that_is_not_a_cursor(string? value)
    {
        Assert.Null(FeedCursor.Decode(value));
    }

    [Theory]
    [InlineData("no-separator")]
    [InlineData("abc:42")]
    [InlineData("123:not-an-int")]
    [InlineData("1:2:3")]
    public void Returns_null_for_base64_that_does_not_hold_a_cursor(string decoded)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(decoded));

        Assert.Null(FeedCursor.Decode(encoded));
    }

    [Theory]
    [InlineData("5000000000000000000:1")]
    [InlineData("9223372036854775807:1")]
    [InlineData("-1:1")]
    public void Returns_null_for_a_tick_count_outside_the_DateTime_range(string decoded)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(decoded));

        Assert.Null(FeedCursor.Decode(encoded));
    }
}
