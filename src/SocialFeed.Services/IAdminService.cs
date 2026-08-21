using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public interface IAdminService
{
    /// <summary>Accounts waiting for approval, oldest first.</summary>
    Task<List<PendingUserResponse>> GetPendingUsersAsync(CancellationToken cancellationToken);

    /// <summary>Approves an account. Returns false when no such user exists.</summary>
    Task<bool> ApproveUserAsync(int userId, CancellationToken cancellationToken);
}
