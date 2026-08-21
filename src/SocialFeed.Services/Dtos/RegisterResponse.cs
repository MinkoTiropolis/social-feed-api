namespace SocialFeed.Services.Dtos;

public class RegisterResponse
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
