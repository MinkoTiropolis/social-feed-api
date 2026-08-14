using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;
using SocialFeed.Data.Entities;
using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Creates a new account. Returns null when the email is already taken.
    /// </summary>
    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Email = email,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Role = UserRole.User,
            Status = AccountStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two registrations for the same address can pass the check above at the same
            // time. The unique index on Email rejects the second one, and it means exactly
            // what the check meant: the address is taken.
            return null;
        }

        return new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            Status = user.Status.ToString()
        };
    }
}
