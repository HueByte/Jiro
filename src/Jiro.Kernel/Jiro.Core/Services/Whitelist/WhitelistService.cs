using Jiro.Core.DTO;
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

        public async Task<bool> IsWhitelistedAsync(string userId)
        {
            return await _whitelistRepository.AsQueryable()
                .AnyAsync(x => x.UserId == userId);
        }

        public async Task<bool> AddUserToWhitelistAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                throw new HandledException("User doesn't exist");

            return await AddUserToWhitelistAsync(user);
        }

        public async Task<bool> AddUserToWhitelistAsync(AppUser user)
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

        public async Task<bool> RemoveUserFromWhitelistAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                throw new HandledException("User doesn't exist");

            return await RemoveUserFromWhitelistAsync(user);
        }

        public async Task<bool> RemoveUserFromWhitelistAsync(AppUser user)
        {
            var entry = await _whitelistRepository.AsQueryable()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            if (entry is null)
                return true;

            await _whitelistRepository.RemoveAsync(entry);
            await _whitelistRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateWhitelistRangeAsync(IEnumerable<WhitelistedUserDTO> users)
        {
            if (users is null)
                throw new HandledException("Users cannot be null");

            if (!users.Any())
                return true;

            var entitiestoAdd = users.Where(x => x.IsWhitelisted)
                .Select(x => new WhiteListEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    AddedDate = DateTime.UtcNow,
                    UserId = x.Id
                })
                .ToList();

            var toRemove = users.Where(x => !x.IsWhitelisted && x.Username != "server")
                .Select(x => x.Id)
                .ToList();

            var entriesToDelete = _whitelistRepository
                .AsQueryable()
                .Where(entry => toRemove.Contains(entry.UserId))
                .ToList();

            await _whitelistRepository.AddRangeAsync(entitiestoAdd);
            await _whitelistRepository.RemoveRangeAsync(entriesToDelete);

            await _whitelistRepository.SaveChangesAsync();

            return true;
        }

        public Task<List<WhitelistedUserDTO>> GetWhiteListUsersAsync()
        {
            return _whitelistRepository
                .AsQueryable()
                .Include(e => e.User)
                .Select(e => new WhitelistedUserDTO
                {
                    Id = e.Id,
                    IsWhitelisted = true,
                    Email = e.User.Email!,
                    Username = e.User.UserName!
                })
                .ToListAsync();
        }

        public async Task<List<WhitelistedUserDTO>> GetUsersWithWhitelistFlagAsync()
        {
            var whitelistedUsers = await _whitelistRepository.AsQueryable()
                .Select(e => e.UserId)
                .ToListAsync();

            var result = await _userManager.Users
                .Select(usr => new WhitelistedUserDTO
                {
                    Id = usr.Id,
                    IsWhitelisted = whitelistedUsers.Any(id => id == usr.Id),
                    Email = usr.Email!,
                    Username = usr.UserName!
                })
                .ToListAsync();

            return result;
        }
    }
}