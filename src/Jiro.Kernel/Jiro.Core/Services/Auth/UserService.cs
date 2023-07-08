using Jiro.Core.Constants;
using Jiro.Core.DTO;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.Auth;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IJWTService _jwtAuthentication;
    private readonly JWTOptions _jwtOptions;
    private readonly IRefreshTokenService _refreshTokenService;
    public UserService(
        ILogger<UserService> logger,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IJWTService jwtAuthentication,
        IOptions<JWTOptions> jwtOptions,
        IRefreshTokenService refreshTokenService)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtAuthentication = jwtAuthentication;
        _jwtOptions = jwtOptions.Value;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<bool> ChangeUsernameAsync(string? userId, string newUsername, string password)
    {
        if (string.IsNullOrEmpty(newUsername))
            throw new JiroException(new ArgumentException("username was null or empty", nameof(newUsername)), "Please provide username");

        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException("userId was null or empty", nameof(userId)), "Something went wrong, please try again");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new JiroException(new Exception("Couldn't find the desired user"), "Something went wrong");

        if (user.UserName == newUsername)
            return true;

        var passwordVerification = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordVerification)
            throw new JiroException("Wrong password");

        var duplicateUser = await _userManager.FindByNameAsync(newUsername);
        if (duplicateUser is not null)
            throw new JiroException("This username is already taken");

        await _userManager.SetUserNameAsync(user, newUsername);

        return true;
    }

    public async Task<bool> ChangePasswordAsync(string? userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
            throw new JiroException("New and old password can't be empty");

        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException("userId was null or empty", nameof(userId)), "Something went wrong, please try again");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new JiroException(new Exception("Couldn't find the desired user"), "Something went wrong");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
            throw new JiroException("Something went wrong, please try again");

        return true;
    }

    public async Task<IdentityResult> CreateUserAsync(RegisterDTO registerUser)
    {
        if (registerUser is null)
            throw new JiroException(new ArgumentException("RegisterDTO cannot be null", nameof(registerUser)), "The provided data was invalid");

        var user = new AppUser()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = registerUser.Username,
            Email = registerUser?.Email,
            AccountCreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, registerUser?.Password!);

        if (!result.Succeeded)
            throw new JiroException("Coudln't create the user", result.Errors.Select(errors => errors.Description).ToArray());

        // seed data
        await _userManager.AddToRoleAsync(user, Roles.USER);

        return result;
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException("userId was null or empty", nameof(userId)), "Something went wrong, please try again");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new JiroException("Couldn't find this user");

        var result = await _userManager.DeleteAsync(user);

        return result;
    }

    public async Task<VerifiedUser> LoginUserAsync(LoginUsernameDTO userDto, string ipAddress)
    {
        if (userDto is null)
            throw new JiroException(new ArgumentException("LoginUsernameDTO cannot be null", nameof(userDto)), "The provided data was invalid");

        if (string.IsNullOrEmpty(ipAddress))
            throw new JiroException(new ArgumentException("ipAddress was null or empty", nameof(ipAddress)), "Something went wrong, please try again");

        var user = await _userManager.Users
            .Where(u => u.UserName == userDto!.Username)
            .Include(e => e.UserRoles)
            .ThenInclude(e => e.Role)
            .Include(e => e.RefreshTokens)
            .FirstOrDefaultAsync();

        return await HandleLogin(user, userDto?.Password, ipAddress);
    }

    public async Task<bool> ChangeEmailAsync(string? userId, string email, string password)
    {
        if (string.IsNullOrEmpty(userId))
            throw new JiroException(new ArgumentException("userId was null or empty", nameof(userId)), "Something went wrong, please try again");

        var user = await _userManager.FindByIdAsync(userId) ?? throw new JiroException("Couldn't find this user");

        if (string.IsNullOrEmpty(email))
            throw new JiroException("Email cannot be empty");

        if (user.Email == email)
            return true;

        var passwordVerification = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordVerification)
            throw new JiroException("Wrong password");

        var duplicateUser = await _userManager.FindByEmailAsync(email);
        if (duplicateUser is not null)
            throw new JiroException("Cannot change to that email");

        var result = await _userManager.ChangeEmailAsync(user, email, "");

        if (!result.Succeeded)
            throw new JiroException("Couldn't change the email", result.Errors.Select(errors => errors.Description).ToArray());

        return result.Succeeded;
    }

    public async Task<IdentityResult> AssignRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId) ?? throw new JiroException("Couldn't find this user");

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var reasons = result.Errors.Select(e => e.Description).ToArray();
            throw new JiroException(new Exception($"Failed to add to the role {role}\n{string.Join("\n", reasons)}"), "Role assignment failed");
        }

        return result;
    }

    public Task<List<UserInfoDTO>> GetUsersAsync()
    {
        return _userManager.Users
            .Include(usr => usr.UserRoles)
                .ThenInclude(usr => usr.Role)
            .Select(usr => new UserInfoDTO()
            {
                Id = usr.Id,
                Username = usr.UserName!,
                Email = usr.Email!,
                Roles = usr.UserRoles.Select(e => e.Role.Name).ToArray()!,
            })
            .AsSplitQuery()
            .ToListAsync();
    }

    private async Task<VerifiedUser> HandleLogin(AppUser? user, string? password, string ipAddress)
    {
        if (user is null || string.IsNullOrEmpty(password))
            throw new JiroException(new Exception("Couldn't find the user"), "Couldn't log in, check your login or password"); // Couldn't find user

        // Validate credentials 
        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (!result.Succeeded)
            throw new JiroException("Couldn't log in, check your login or password");

        var refreshToken = _refreshTokenService.CreateRefreshToken(ipAddress);
        user.RefreshTokens ??= new();
        user.RefreshTokens?.Add(refreshToken);

        var roles = user.UserRoles.Select(e => e.Role.Name).ToList();
        var token = _jwtAuthentication.GenerateJsonWebToken(user!, roles!);

        _refreshTokenService.RemoveOldRefreshTokens(user);

        // save removal of old refresh tokens
        await _userManager.UpdateAsync(user);

        return new VerifiedUser()
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