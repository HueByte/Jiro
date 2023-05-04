using Jiro.Core.DTO;
using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Interfaces.IServices;

public interface IUserService
{
    Task<bool> ChangeEmailAsync(string userId, string email, string password);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> ChangeUsernameAsync(string userId, string username, string password);
    Task<IdentityResult> CreateUserAsync(RegisterDTO registerUser);
    Task<IdentityResult> DeleteUserAsync(string userId);
    Task<IdentityResult> AssignRoleAsync(string userId, string role);
    Task<VerifiedUserDTO> LoginUserAsync(LoginUsernameDTO userDto, string IpAddress);
    Task<List<UserInfoDTO>> GetUsersAsync();
}