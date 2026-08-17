using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public class PostService
{
    private readonly AppDbContext _db;

    public PostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PostResponse> CreateAsync(int authorId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        var post = new Post
        {
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.Posts.Add(post);
        await _db.SaveChangesAsync(cancellationToken);

        // Read it back through the same projection every other endpoint uses, so a newly
        // created post looks identical to the same post seen later in the feed.
        return (await GetByIdAsync(post.Id, authorId, cancellationToken))!;
    }

    /// <summary>
    /// Returns a single post, or null when it does not exist or has been soft deleted. The
    /// global query filter handles the deleted case, so it needs no mention here.
    /// </summary>
    public async Task<PostResponse?> GetByIdAsync(int postId, int currentUserId, CancellationToken cancellationToken)
    {
        return await _db.Posts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => new PostResponse
            {
                Id = p.Id,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                Author = new AuthorSummary
                {
                    Id = p.Author.Id,
                    Name = p.Author.Name,
                    Description = p.Author.Description,
                    ProfilePictureUrl = p.Author.ProfilePicturePath
                },
                LikeCount = p.Likes.Count(),
                LikedByMe = p.Likes.Any(l => l.UserId == currentUserId)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
