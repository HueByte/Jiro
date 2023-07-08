namespace Jiro.Core.Services.Auth;

public class CurrentUserService : ICurrentUserService
{
    public string? UserId { get; private set; }

    public void SetCurrentUser(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException(null, nameof(userId)), "Something went wrong with parsing current user", "Try to relogin");

        UserId = userId;
    }
}