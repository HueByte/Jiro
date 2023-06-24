namespace Jiro.Core.Interfaces.IServices;

public interface ICurrentUserService
{
    string? UserId { get; }
    void SetCurrentUser(string? userId);
}