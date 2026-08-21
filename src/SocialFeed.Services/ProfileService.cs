using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Services.Dtos;
using SocialFeed.Services.Interfaces;

namespace SocialFeed.Services;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the signed-in user's profile with both totals.
    /// </summary>
    public async Task<MeResponse?> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new MeResponse
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Description = u.Description,
                ProfilePictureUrl = u.ProfilePicturePath,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt,
                TotalPosts = u.Posts.Count(),
                TotalLikes = u.Posts.SelectMany(p => p.Likes).Count()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the signed-in user's name, description and profile picture. Fields left null keep their current value.
    /// </summary>
    public async Task<MeResponse?> UpdateMeAsync(int userId, UpdateMeRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (request.Name is not null)
        {
            user.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            user.Description = request.Description.Trim();
        }

        if (request.ProfilePictureUrl is not null)
        {
            user.ProfilePicturePath = request.ProfilePictureUrl.Trim();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetMeAsync(userId, cancellationToken);
    }
}
