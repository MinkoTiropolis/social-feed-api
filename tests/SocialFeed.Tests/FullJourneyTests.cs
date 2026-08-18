using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Tests;

/// <summary>
/// One pass through everything the assignment asks for, against the real HTTP pipeline:
/// register, be blocked as pending, be approved, log in, post, like, see it in the feed,
/// soft delete it, see it disappear, restore it, see it return.
/// </summary>
public class FullJourneyTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public FullJourneyTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_approve_post_like_delete_restore()
    {
        var client = _factory.CreateClient();
        var email = $"journey-{Guid.NewGuid():N}@sharebook.local";
        const string password = "Str0ngPass!";

        // Registering creates a sandboxed account.
        var register = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password,
            name = "Journey Tester",
            description = "QA, Waracle"
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = await register.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.Equal("Pending", registered!.Status);

        // It cannot log in yet, and the refusal is distinct from a wrong password.
        var blocked = await client.PostAsJsonAsync("/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        // The seeded superuser approves it.
        var adminToken = await LogIn(client, "admin@sharebook.local", "Admin123!");
        var pending = await GetJson<List<PendingUserResponse>>(client, "/admin/users/pending", adminToken);
        Assert.Contains(pending, u => u.Email == email);

        var approve = await Send(client, HttpMethod.Post, $"/admin/users/{registered.Id}/approve", adminToken);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        // Now the same credentials work.
        var token = await LogIn(client, email, password);

        // Create a post.
        var create = await SendJson(client, HttpMethod.Post, "/posts", token, new { content = "My first post on sharebook." });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var post = await create.Content.ReadFromJsonAsync<PostResponse>();

        // It appears in the feed, unliked.
        var feed = await GetJson<FeedResponse>(client, "/feed", token);
        var inFeed = Assert.Single(feed.Items, p => p.Id == post!.Id);
        Assert.Equal(0, inFeed.LikeCount);
        Assert.False(inFeed.LikedByMe);

        // Liking twice leaves one like.
        await Send(client, HttpMethod.Post, $"/posts/{post!.Id}/like", token);
        await Send(client, HttpMethod.Post, $"/posts/{post.Id}/like", token);

        feed = await GetJson<FeedResponse>(client, "/feed", token);
        inFeed = Assert.Single(feed.Items, p => p.Id == post.Id);
        Assert.Equal(1, inFeed.LikeCount);
        Assert.True(inFeed.LikedByMe);

        // Soft deleting hides it from the feed.
        var delete = await Send(client, HttpMethod.Delete, $"/posts/{post.Id}", token);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        feed = await GetJson<FeedResponse>(client, "/feed", token);
        Assert.DoesNotContain(feed.Items, p => p.Id == post.Id);

        var gone = await Send(client, HttpMethod.Get, $"/posts/{post.Id}", token);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);

        // Restoring brings it back, likes intact.
        var restore = await Send(client, HttpMethod.Post, $"/posts/{post.Id}/restore", token);
        Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);

        feed = await GetJson<FeedResponse>(client, "/feed", token);
        inFeed = Assert.Single(feed.Items, p => p.Id == post.Id);
        Assert.Equal(1, inFeed.LikeCount);
    }

    [Fact]
    public async Task A_user_cannot_delete_someone_elses_post()
    {
        var client = _factory.CreateClient();

        var danielToken = await LogIn(client, "daniel@sharebook.local", "User123!");
        var mariaToken = await LogIn(client, "maria@sharebook.local", "User123!");

        var create = await SendJson(client, HttpMethod.Post, "/posts", danielToken, new { content = "Daniel's post." });
        var post = await create.Content.ReadFromJsonAsync<PostResponse>();

        var forbidden = await Send(client, HttpMethod.Delete, $"/posts/{post!.Id}", mariaToken);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task The_feed_requires_authentication()
    {
        var response = await _factory.CreateClient().GetAsync("/feed");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logging_out_stops_the_refresh_token_working()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "maria@sharebook.local", password = "User123!" });
        var session = await login.Content.ReadFromJsonAsync<LoginResponse>();

        // The refresh token works before logging out.
        var beforeLogout = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = session!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);

        var logout = await SendJson(client, HttpMethod.Post, "/auth/logout", session.AccessToken, new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // And stops working afterwards. This is what makes logout mean something with a
        // stateless access token.
        var afterLogout = await client.PostAsJsonAsync("/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    private static async Task<string> LogIn(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });

        response.EnsureSuccessStatusCode();

        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();

        return session!.AccessToken;
    }

    private static async Task<T> GetJson<T>(HttpClient client, string url, string token)
    {
        var response = await Send(client, HttpMethod.Get, url, token);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, string? token = null)
    {
        return SendJson(client, method, url, token, null);
    }

    private static Task<HttpResponseMessage> SendJson(HttpClient client, HttpMethod method, string url, string? token, object? body)
    {
        var request = new HttpRequestMessage(method, url);

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }
}
