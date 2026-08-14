namespace SocialFeed.Services.Dtos;

public class RegisterResponse
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Always "Pending". Returned so the client can explain why logging in will not work yet.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
