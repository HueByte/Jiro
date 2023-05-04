using System.Formats.Asn1;
using Jiro.Core.Constants;
using Jiro.Core.DTO;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.Auth;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJWTService _jwtAuthentication;
    private readonly JWTOptions _jwtOptions;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IWhitelistRepository _whitelistRepository;
    public UserService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJWTService jwtAuthentication,
        IOptions<JWTOptions> jwtOptions,
        IRefreshTokenService refreshTokenService,
        IWhitelistRepository whitelistRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtAuthentication = jwtAuthentication;
        _jwtOptions = jwtOptions.Value;
        _refreshTokenService = refreshTokenService;
        _whitelistRepository = whitelistRepository;
    }

    public async Task<bool> ChangeUsernameAsync(string? userId, string newUsername, string password)
    {
        if (string.IsNullOrEmpty(newUsername))
            throw new HandledException("Username cannot be empty");

        if (string.IsNullOrEmpty(userId))
            throw new HandledException("Couldn't find this user");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new HandledException("Couldn't find this user");

        if (user.UserName == newUsername)
            return true;

        var passwordVerification = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordVerification)
            throw new HandledException("Wrong password");

        var duplicateUser = await _userManager.FindByNameAsync(newUsername);
        if (duplicateUser is not null)
            throw new HandledException("This username is already taken");

        await _userManager.SetUserNameAsync(user, newUsername);

        return true;
    }

    public async Task<bool> ChangePasswordAsync(string? userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            throw new HandledException("New and old password can't be empty");

        if (string.IsNullOrEmpty(userId))
            throw new HandledException("Couldn't find this user");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new HandledException("Couldn't find this user");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
            throw new HandledException("Couldn't change password, the current password is incorrect");

        return true;
    }

    public async Task<IdentityResult> CreateUserAsync(RegisterDTO registerUser)
    {
        if (registerUser is null)
            throw new HandledException("Register User model cannot be empty");

        var user = new AppUser()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = registerUser.Username,
            Email = registerUser?.Email,
            AccountCreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerUser?.Password!);

        if (!result.Succeeded)
            throw new HandledExceptionList(result.Errors.Select(errors => errors.Description).ToList());

        // seed data
        await _userManager.AddToRoleAsync(user, Roles.USER);

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new HandledException("Couldn't find this user");

        var result = await _userManager.DeleteAsync(user);

        return result;
    }

    public async Task<VerifiedUserDTO> LoginUserAsync(LoginUsernameDTO userDto, string IpAddress)
    {
        if (userDto is null && string.IsNullOrEmpty(IpAddress))
            throw new HandledException("User model and Ip address cannot be empty");

        var user = await _userManager.Users
            .Where(u => u.UserName == userDto!.Username)
            .Include(e => e.UserRoles)
            .ThenInclude(e => e.Role)
            .Include(e => e.RefreshTokens)
            .FirstOrDefaultAsync();

        return await HandleLogin(user!, userDto!.Password!, IpAddress);
    }

    public async Task<bool> ChangeEmailAsync(string? userId, string email, string password)
    {
        if (string.IsNullOrEmpty(userId))
            throw new HandledException("Couldn't find this user");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new HandledException("Couldn't find this user");
        if (string.IsNullOrEmpty(email))
            throw new HandledException("Email cannot be empty");

        if (user.Email == email)
            return true;

        var passwordVerification = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordVerification)
            throw new HandledException("Wrong password");

        var duplicateUser = await _userManager.FindByEmailAsync(email);
        if (duplicateUser is not null)
            throw new HandledException("This email is already taken");

        var result = await _userManager.ChangeEmailAsync(user, email, "");

        if (!result.Succeeded)
            throw new HandledExceptionList(result.Errors.Select(errors => errors.Description).ToList());

        return result.Succeeded;
    }

    public async Task<IdentityResult> AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            throw new HandledException("Couldn't find this user");

        var result = await _userManager.AddToRoleAsync(user, role);

        return result;
    }

    public async Task<List<UserInfoDTO>> GetUsersAsync()
    {
        var users = await _userManager.Users
            .Include(usr => usr.UserRoles)
                .ThenInclude(usr => usr.Role)
            .Join(_whitelistRepository.AsQueryable(),
                user => user.Id,
                wle => wle.UserId,
                (user, wle) => new { User = user, IsWhitelisted = true })
            .Select(usr => new UserInfoDTO()
            {
                Id = usr.User.Id,
                Username = usr.User.UserName!,
                Email = usr.User.Email!,
                Roles = usr.User.UserRoles.Select(e => e.Role.Name).ToArray()!,
                IsWhitelisted = usr.IsWhitelisted
            })
            .ToListAsync();

        return users;
    }

    private async Task<VerifiedUserDTO> HandleLogin(AppUser user, string password, string ipAddress)
    {
        if (user is null)
            throw new HandledException("Couldn't log in, check your login or password"); // Couldn't find user

        // Validate credentials 
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
            throw new HandledException("Couldn't log in, check your login or password");

        var refreshToken = _refreshTokenService.CreateRefreshToken(ipAddress);
        user.RefreshTokens ??= new();
        user.RefreshTokens?.Add(refreshToken);

        var roles = user.UserRoles.Select(e => e.Role.Name).ToList();
        var token = _jwtAuthentication.GenerateJsonWebToken(user!, roles!);

        _refreshTokenService.RemoveOldRefreshTokens(user);

        // save removal of old refresh tokens
        await _userManager.UpdateAsync(user);

        return new VerifiedUserDTO()
        {
            Username = user!.UserName,
            Roles = roles?.ToArray()!,
            RefreshToken = refreshToken!.Token,
            RefreshTokenExpiration = refreshToken!.Expires,
            Token = token,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpireTime),
            Email = user.Email
        };
    }
}