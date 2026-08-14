using SocialFeed.Services.Dtos;

namespace SocialFeed.Services;

public enum LoginOutcome
{
    Success,

    /// <summary>No such account, or the wrong password. The two are not distinguished.</summary>
    InvalidCredentials,

    /// <summary>The password was correct, but the account is still awaiting approval.</summary>
    AccountPending
}

public record LoginResult(LoginOutcome Outcome, LoginResponse? Response);
