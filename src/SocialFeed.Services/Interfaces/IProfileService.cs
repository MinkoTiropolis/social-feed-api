using SocialFeed.Services.Dtos;

namespace SocialFeed.Services.Interfaces;

public interface IProfileService
{
    /// <summary>The user's profile with total posts and total likes received.</summary>
    Task<MeResponse?> GetMeAsync(int userId, CancellationToken cancellationToken);

    /// <summary>Updates name, description and picture. Null fields are left unchanged.</summary>
    Task<MeResponse?> UpdateMeAsync(int userId, UpdateMeRequest request, CancellationToken cancellationToken);
}
