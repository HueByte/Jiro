using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jiro.Core.Services.Whitelist
{
    public class WhitelistService : IWhitelistService
    {
        private readonly IWhitelistRepository _whitelistRepository;
        private readonly UserManager<AppUser> _userManager;
        public WhitelistService(IWhitelistRepository whitelistRepository, UserManager<AppUser> userManager)
        {
            _whitelistRepository = whitelistRepository;
            _userManager = userManager;
        }

        public async Task<bool> IsWhitelisted(string userId)
        {
            return await _whitelistRepository.AsQueryable()
                .AnyAsync(x => x.UserId == userId);
        }

        public async Task<bool> AddUserToWhitelist(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                throw new HandledException("User doesn't exist");

            return await AddUserToWhitelist(user);
        }

        public async Task<bool> AddUserToWhitelist(AppUser user)
        {
            var entry = new WhiteListEntry
            {
                Id = Guid.NewGuid().ToString(),
                AddedDate = DateTime.UtcNow,
                UserId = user.Id
            };

            await _whitelistRepository.AddAsync(entry);
            await _whitelistRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserToWhitelist(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                throw new HandledException("User doesn't exist");

            return await RemoveUserToWhitelist(user);
        }

        public async Task<bool> RemoveUserToWhitelist(AppUser user)
        {
            var entry = await _whitelistRepository.AsQueryable()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (entry is null)
                return true;

            await _whitelistRepository.RemoveAsync(entry);
            await _whitelistRepository.SaveChangesAsync();
            return true;
        }
    }
}