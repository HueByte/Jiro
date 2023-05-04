using Jiro.Core.DTO;
using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IWhitelistService
    {
        Task<bool> AddUserToWhitelistAsync(string userId);
        Task<bool> AddUserToWhitelistAsync(AppUser user);
        Task<bool> IsWhitelistedAsync(string userId);
        Task<bool> RemoveUserFromWhitelistAsync(string userId);
        Task<bool> RemoveUserFromWhitelistAsync(AppUser user);
        Task<bool> UpdateWhitelistRangeAsync(IEnumerable<WhitelistedUserDTO> users);
        Task<List<WhitelistedUserDTO>> GetWhiteListUsersAsync();
        Task<List<WhitelistedUserDTO>> GetUsersWithWhitelistFlagAsync();
    }
}