using Microsoft.AspNetCore.Authorization;
using SocialFeed.Data.Entities;

namespace SocialFeed.Api;

public static class AuthorizationPolicies
{
    public const string SuperuserOnly = "SuperuserOnly";

    public static AuthorizationBuilder AddSocialFeedPolicies(this AuthorizationBuilder builder)
    {
        return builder
            // Named once here rather than comparing role strings inside each action, so the
            // rule cannot drift between endpoints.
            .AddPolicy(SuperuserOnly, policy => policy.RequireRole(nameof(UserRole.Superuser)));
    }
}
