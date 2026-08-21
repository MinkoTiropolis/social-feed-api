using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PendingUserResponse>> GetPendingUsersAsync(CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Status == AccountStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new PendingUserResponse
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ApproveUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.Status == AccountStatus.Approved)
        {
            return true;
        }

        user.Status = AccountStatus.Approved;
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
