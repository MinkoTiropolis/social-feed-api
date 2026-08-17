namespace SocialFeed.Services;

public enum PostMutationResult
{
    Success,

    /// <summary>The post does not exist, or is not in the state the operation needs.</summary>
    NotFound,

    /// <summary>The post exists, but the caller is neither its author nor a superuser.</summary>
    Forbidden
}
