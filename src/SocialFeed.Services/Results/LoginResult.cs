using SocialFeed.Services.Dtos;

namespace SocialFeed.Services.Results;

public enum LoginOutcome
{
    Success,
    InvalidCredentials,
    AccountPending
}

public record LoginResult(LoginOutcome Outcome, LoginResponse? Response);
