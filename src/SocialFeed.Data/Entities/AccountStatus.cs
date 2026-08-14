namespace SocialFeed.Data.Entities;

public enum AccountStatus
{
    /// <summary>
    /// The account has been registered but cannot log in until a superuser approves it.
    /// </summary>
    Pending = 0,

    Approved = 1
}
