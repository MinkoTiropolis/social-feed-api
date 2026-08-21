using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public interface IPostService
{
    /// <summary>Creates a post authored by the given user.</summary>
    Task<PostResponse> CreateAsync(int authorId, CreatePostRequest request, CancellationToken cancellationToken);

    /// <summary>A single post, or null when it does not exist or is soft deleted.</summary>
    Task<PostResponse?> GetByIdAsync(int postId, int currentUserId, CancellationToken cancellationToken);

    /// <summary>Likes a post. Idempotent. False when the post does not exist.</summary>
    Task<bool> LikeAsync(int postId, int userId, CancellationToken cancellationToken);

    /// <summary>Removes a like. Idempotent. False when the post does not exist.</summary>
    Task<bool> UnlikeAsync(int postId, int userId, CancellationToken cancellationToken);

    /// <summary>Paginated list of users who liked a post, most recent first.</summary>
    Task<LikersResponse?> GetLikersAsync(int postId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Soft deletes a post. Author or superuser only.</summary>
    Task<PostMutationResult> SoftDeleteAsync(int postId, int currentUserId, bool isSuperuser, CancellationToken cancellationToken);

    /// <summary>Restores a soft deleted post. Author or superuser only.</summary>
    Task<PostMutationResult> RestoreAsync(int postId, int currentUserId, bool isSuperuser, CancellationToken cancellationToken);
}
