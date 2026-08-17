using System.Text;

namespace SocialFeed.Services;

/// <summary>
/// Marks a position in the feed: the timestamp and id of the last post a client received.
/// <para>
/// It is base64 encoded so it reads as an opaque string to the client. That is not security —
/// anyone can decode it — it is so the client treats it as a token to hand back rather than
/// as two fields it might start constructing itself, which would tie the API to this exact
/// ordering forever.
/// </para>
/// </summary>
public record FeedCursor(DateTime CreatedAt, int Id)
{
    public string Encode()
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CreatedAt.Ticks}:{Id}"));
    }

    /// <summary>
    /// Returns null for anything that is not a cursor this API produced, so a malformed value
    /// starts from the beginning rather than failing the request.
    /// </summary>
    public static FeedCursor? Decode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(value)).Split(':');

            if (parts.Length == 2
                && long.TryParse(parts[0], out var ticks)
                && int.TryParse(parts[1], out var id))
            {
                return new FeedCursor(new DateTime(ticks, DateTimeKind.Utc), id);
            }
        }
        catch (FormatException)
        {
            // Not valid base64.
        }

        return null;
    }
}
