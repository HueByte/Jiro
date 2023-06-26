using Jiro.Core;
using Jiro.Core.Constants;
using Jiro.Core.DTO;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Auth;
using Jiro.Tests.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Jiro.Tests.ServiceTests;

public class UserServiceTests
{
    private readonly IUserService _userService;
    private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
    private readonly Mock<IJWTService> _jwtServiceMock;
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly Mock<IWhitelistRepository> _whitelistRepositoryMock;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IOptions<JWTOptions>> _jwtOptionsMock;

    public UserServiceTests()
    {
        Mock<UserManager<AppUser>> userManager = MockObjects.GetUserManagerMock<AppUser>();
        Mock<SignInManager<AppUser>> signInManager = MockObjects.GetSignInManagerMock<AppUser>();
        Mock<IJWTService> jwtMock = new();
        Mock<IOptions<JWTOptions>> jwtOptionsMock = new();
        Mock<IRefreshTokenService> refreshTokenServiceMock = new();
        Mock<IWhitelistRepository> whitelistRepositoryMock = new();
        Mock<ILogger<UserService>> loggerMock = new();

        _userManagerMock = userManager;
        _signInManagerMock = signInManager;
        _jwtServiceMock = jwtMock;
        _refreshTokenServiceMock = refreshTokenServiceMock;
        _whitelistRepositoryMock = whitelistRepositoryMock;
        _jwtOptionsMock = jwtOptionsMock;
        _jwtOptionsMock.Setup(x => x.Value).Returns(new JWTOptions() { Secret = "test" });
        _userService = new UserService(
            loggerMock.Object,
            userManager.Object,
            signInManager.Object,
            jwtMock.Object,
            jwtOptionsMock.Object,
            refreshTokenServiceMock.Object,
            whitelistRepositoryMock.Object);
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenValidParams_ReturnsTrue()
    {
        // Arrange
        string userId = "userId";
        string newUsername = "newUsername";
        string password = "password";
        var user = new AppUser { Id = userId, UserName = "oldUsername" };

        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        _userManagerMock.Setup(u => u.FindByNameAsync(newUsername)).ReturnsAsync((AppUser)null!);

        // Act
        bool result = await _userService.ChangeUsernameAsync(userId, newUsername, password);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(u => u.SetUserNameAsync(user, newUsername), Times.Once);
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenEmptyNewUsername_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        string newUsername = "";
        string password = "password";

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangeUsernameAsync(userId, newUsername, password);
        });
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenInvalidUserId_ThrowsJiroException()
    {
        // Arrange
        string userId = "";
        string newUsername = "newUsername";
        string password = "password";

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangeUsernameAsync(userId, newUsername, password);
        });
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenSameUsername_ReturnsTrue()
    {
        // Arrange
        string userId = "userId";
        string newUsername = "oldUsername";
        string password = "password";
        var user = new AppUser { Id = userId, UserName = "oldUsername" };
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        // Act
        bool result = await _userService.ChangeUsernameAsync(userId, newUsername, password);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(u => u.SetUserNameAsync(user, newUsername), Times.Never);
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenWrongPassword_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        string newUsername = "newUsername";
        string password = "wrongPassword";
        var user = new AppUser { Id = userId, UserName = "oldUsername" };
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangeUsernameAsync(userId, newUsername, password);
        });
    }

    [Fact]
    public async Task ChangeUsernameAsync_WhenDuplicateUsername_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        string newUsername = "newUsername";
        string password = "password";
        var user = new AppUser { Id = userId, UserName = "oldUsername" };
        var duplicateUser = new AppUser { Id = "duplicateUserId", UserName = newUsername };
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        _userManagerMock.Setup(u => u.FindByNameAsync(newUsername)).ReturnsAsync(duplicateUser);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangeUsernameAsync(userId, newUsername, password);
        });
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenValidParams_ReturnsTrue()
    {
        // Arrange
        string userId = "userId";
        string currentPassword = "currentPassword";
        string newPassword = "newPassword";
        var user = new AppUser { Id = userId };
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        bool result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(u => u.ChangePasswordAsync(user, currentPassword, newPassword), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenEmptyPasswords_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        string currentPassword = "";
        string newPassword = "";

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
        });
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenInvalidUserId_ThrowsJiroException()
    {
        // Arrange
        string userId = "";
        string currentPassword = "currentPassword";
        string newPassword = "newPassword";

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
        });
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenChangePasswordFails_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        string currentPassword = "currentPassword";
        string newPassword = "newPassword";
        var user = new AppUser { Id = userId };
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.ChangePasswordAsync(user, currentPassword, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error message" }));

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
        });
    }

    [Fact]
    public async Task CreateUserAsync_WhenValidRegisterUser_ReturnsIdentityResult()
    {
        // Arrange
        var registerUser = new RegisterDTO
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "password"
        };

        var createdUser = new AppUser
        {
            Id = "userId",
            UserName = registerUser.Username,
            Email = registerUser.Email,
            AccountCreatedDate = It.IsAny<DateTime>()
        };

        var identityResult = IdentityResult.Success;
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), registerUser.Password)).ReturnsAsync(identityResult);
        _userManagerMock.Setup(u => u.AddToRoleAsync(createdUser, Roles.USER)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUserAsync(registerUser);

        // Assert
        Assert.Equal(identityResult, result);
        _userManagerMock.Verify(u => u.CreateAsync(It.IsAny<AppUser>(), registerUser.Password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WhenNullRegisterUser_ThrowsJiroException()
    {
        // Arrange
        RegisterDTO? registerUser = null;

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.CreateUserAsync(registerUser!);
        });
    }

    [Fact]
    public async Task CreateUserAsync_WhenCreateUserFails_ThrowsJiroExceptionList()
    {
        // Arrange
        var registerUser = new RegisterDTO
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "password"
        };

        var identityResult = IdentityResult.Failed(new IdentityError { Description = "Error message" });
        _userManagerMock.Setup(u => u.CreateAsync(It.IsAny<AppUser>(), registerUser.Password)).ReturnsAsync(identityResult);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.CreateUserAsync(registerUser);
        });
    }

    [Fact]
    public async Task DeleteUserAsync_WhenValidUserId_ReturnsIdentityResult()
    {
        // Arrange
        string userId = "userId";
        var user = new AppUser { Id = userId };
        var identityResult = IdentityResult.Success;
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(u => u.DeleteAsync(user)).ReturnsAsync(identityResult);

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        Assert.Equal(identityResult, result);
        _userManagerMock.Verify(u => u.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(u => u.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNullOrEmptyUserId_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        AppUser? user = null!;
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.DeleteUserAsync(userId);
        });
    }

    [Fact]
    public async Task DeleteUserAsync_WhenUserNotFound_ThrowsJiroException()
    {
        // Arrange
        string userId = "userId";
        AppUser? user = null!;
        _userManagerMock.Setup(u => u.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.DeleteUserAsync(userId);
        });
    }

    [Fact]
    public async Task LoginUserAsync_WhenValidUser_ReturnsVerifiedUserDTO()
    {
        // Arrange
        string username = "testuser";
        string password = "password";
        string ipAddress = "127.0.0.1";
        var userDto = new LoginUsernameDTO { Username = username, Password = password };
        ICollection<AppUserRole> userRoles = new List<AppUserRole>()
        {
            new AppUserRole { Role = new AppRole() { Name = Roles.USER } }
        };
        var appUser = new AppUser { UserName = username, UserRoles = userRoles, Email = "test@example.com" };
        var verifiedUserDto = new VerifiedUserDTO
        {
            Username = username,
            Roles = new[] { Roles.USER },
            RefreshToken = "refreshToken",
            Token = "token",
            Email = "test@example.com"
        };

        _userManagerMock.Setup(u => u.Users)
            .Returns(MockObjects.GetMockDbSet(new List<AppUser> { appUser }).Object);

        _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(appUser, password, false))
            .ReturnsAsync(SignInResult.Success);

        _jwtServiceMock.Setup(j => j.GenerateJsonWebToken(appUser, It.IsAny<List<string>>()))
            .Returns(verifiedUserDto.Token);

        _refreshTokenServiceMock.Setup(r => r.CreateRefreshToken(ipAddress))
            .Returns(new RefreshToken { Token = verifiedUserDto.RefreshToken, Expires = verifiedUserDto.RefreshTokenExpiration });

        //_jwtOptionsMock.Setup(o => o.)

        // Act
        var result = await _userService.LoginUserAsync(userDto, ipAddress);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(verifiedUserDto.Username, result.Username);
        Assert.Equal(verifiedUserDto.Roles, result.Roles);
        Assert.Equal(verifiedUserDto.RefreshToken, result.RefreshToken);
        Assert.Equal(verifiedUserDto.Token, result.Token);
        Assert.Equal(verifiedUserDto.Email, result.Email);

        _userManagerMock.Verify(u => u.Users, Times.Once);
        _signInManagerMock.Verify(s => s.CheckPasswordSignInAsync(appUser, password, false), Times.Once);
        _jwtServiceMock.Verify(j => j.GenerateJsonWebToken(appUser, It.IsAny<List<string>>()), Times.Once);
        _refreshTokenServiceMock.Verify(r => r.CreateRefreshToken(ipAddress), Times.Once);
    }

    [Fact]
    public async Task LoginUserAsync_WhenInvalidUser_ThrowsJiroException()
    {
        // Arrange
        string username = "testuser";
        string ipAddress = "127.0.0.1";
        var userDto = new LoginUsernameDTO { Username = username };

        _userManagerMock.Setup(u => u.Users)
            .Returns(MockObjects.GetMockDbSet(new List<AppUser>()).Object);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.LoginUserAsync(userDto, ipAddress);
        });

        _userManagerMock.Verify(u => u.Users, Times.Once);
        _signInManagerMock.Verify(s => s.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), false), Times.Never);
        _jwtServiceMock.Verify(j => j.GenerateJsonWebToken(It.IsAny<AppUser>(), It.IsAny<List<string>>()), Times.Never);
        _refreshTokenServiceMock.Verify(r => r.CreateRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginUserAsync_WhenInvalidCredentials_ThrowsJiroException()
    {
        // Arrange
        string username = "testuser";
        string password = "password";
        string ipAddress = "127.0.0.1";
        var userDto = new LoginUsernameDTO { Username = username };
        var appUser = new AppUser { UserName = username };

        _userManagerMock.Setup(u => u.Users)
            .Returns(MockObjects.GetMockDbSet(new List<AppUser> { appUser }).Object);

        _signInManagerMock.Setup(s => s.CheckPasswordSignInAsync(appUser, password, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.LoginUserAsync(userDto, ipAddress);
        });

        _userManagerMock.Verify(u => u.Users, Times.Once);
        _jwtServiceMock.Verify(j => j.GenerateJsonWebToken(It.IsAny<AppUser>(), It.IsAny<List<string>>()), Times.Never);
        _refreshTokenServiceMock.Verify(r => r.CreateRefreshToken(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_WhenValidUser_ReturnsIdentityResult()
    {
        // Arrange
        string userId = "user1";
        string role = "Role1";
        var appUser = new AppUser { Id = userId };
        var identityResult = IdentityResult.Success;

        _userManagerMock.Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync(appUser);

        _userManagerMock.Setup(u => u.AddToRoleAsync(appUser, role))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _userService.AssignRoleAsync(userId, role);

        // Assert
        Assert.Equal(identityResult, result);

        _userManagerMock.Verify(u => u.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(appUser, role), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_WhenInvalidUser_ThrowsJiroException()
    {
        // Arrange
        string userId = "user1";
        string role = "Role1";

        _userManagerMock.Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync((AppUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.AssignRoleAsync(userId, role);
        });

        _userManagerMock.Verify(u => u.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_WhenAddToRoleAsyncFails_ThrowsJiroExceptionList()
    {
        // Arrange
        string userId = "user1";
        string role = "Role1";
        var appUser = new AppUser { Id = userId };
        var identityResult = IdentityResult.Failed(new IdentityError { Description = "Role assignment failed" });

        _userManagerMock.Setup(u => u.FindByIdAsync(userId))
            .ReturnsAsync(appUser);

        _userManagerMock.Setup(u => u.AddToRoleAsync(appUser, role))
            .ReturnsAsync(identityResult);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<JiroException>(async () =>
        {
            await _userService.AssignRoleAsync(userId, role);
        });

        Assert.Contains("Role assignment failed", exception.Message);

        _userManagerMock.Verify(u => u.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(u => u.AddToRoleAsync(appUser, role), Times.Once);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsListOfUserInfoDTO()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new AppUser
            {
                Id = "user1",
                UserName = "User1",
                Email = "user1@example.com",
                UserRoles = new List<AppUserRole>
                {
                    new AppUserRole { Role = new AppRole { Name = "Role1" } }
                }
            },
            new AppUser
            {
                Id = "user2",
                UserName = "User2",
                Email = "user2@example.com",
                UserRoles = new List<AppUserRole>
                {
                    new AppUserRole { Role = new AppRole { Name = "Role2" } }
                }
            }
        };

        var whitelistEntries = new List<WhiteListEntry>
        {
            new WhiteListEntry { UserId = "user1" },
            new WhiteListEntry { UserId = "user2" }
        };

        var usersMock = MockObjects.GetMockDbSet(users);

        _userManagerMock.Setup(u => u.Users)
            .Returns(usersMock.Object);

        _whitelistRepositoryMock.Setup(w => w.AsQueryable())
            .Returns(whitelistEntries.AsQueryable());

        // Act
        var result = await _userService.GetUsersAsync();

        // Assert
        Assert.Equal(users.Count, result.Count);

        for (var i = 0; i < users.Count; i++)
        {
            Assert.Equal(users[i].Id, result[i].Id);
            Assert.Equal(users[i].UserName, result[i].Username);
            Assert.Equal(users[i].Email, result[i].Email);
            Assert.Equal(users[i].UserRoles.Select(ur => ur.Role.Name).ToArray(), result[i].Roles);
            Assert.True(result[i].IsWhitelisted);
        }
    }
}
