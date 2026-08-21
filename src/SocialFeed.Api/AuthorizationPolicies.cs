using Microsoft.AspNetCore.Authorization;
using SocialFeed.Data.Entities;

namespace SocialFeed.Api;

public static class AuthorizationPolicies
{
    public const string SuperuserOnly = "SuperuserOnly";

    public static AuthorizationBuilder AddSocialFeedPolicies(this AuthorizationBuilder builder)
    {
        return builder.AddPolicy(SuperuserOnly, policy => policy.RequireRole(nameof(UserRole.Superuser)));
    }
}
