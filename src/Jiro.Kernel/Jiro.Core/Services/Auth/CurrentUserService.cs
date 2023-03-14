namespace Jiro.Core.Services.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        public string UserId { get; init; } = default!;
    }
}