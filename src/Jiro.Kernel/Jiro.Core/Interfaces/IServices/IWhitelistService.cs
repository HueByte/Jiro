using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IWhitelistService
    {
        Task<bool> AddUserToWhitelist(string userId);
        Task<bool> AddUserToWhitelist(AppUser user);
        Task<bool> IsWhitelisted(string userId);
        Task<bool> RemoveUserToWhitelist(string userId);
        Task<bool> RemoveUserToWhitelist(AppUser user);
    }
}