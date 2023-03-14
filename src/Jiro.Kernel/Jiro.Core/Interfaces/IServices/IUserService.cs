using Jiro.Core.DTO;
using Microsoft.AspNetCore.Identity;

namespace Jiro.Core.Interfaces.IServices;

public interface IUserService
{
    Task<bool> ChangeEmailAsync(string email, string password);
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<bool> ChangeUsernameAsync(string username, string password);
    Task<IdentityResult> CreateUser(RegisterDTO registerUser);
    Task<VerifiedUserDTO> LoginUser(LoginUsernameDTO userDto, string IpAddress);
}