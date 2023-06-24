using Jiro.Core;
using Jiro.Core.Interfaces.IRepositories;
using Jiro.Core.Models;
using Jiro.Core.Services.Whitelist;
using Jiro.Tests.Utilities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Jiro.Tests.ServiceTests;

public class WhitelistServiceTests
{
    private readonly Mock<IWhitelistRepository> _mockRepo;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly WhitelistService _whitelistService;
    private readonly string existingUserId = "testUserId";
    private readonly string notExistingUserId = "notTestUserId";
    private readonly AppUser existingUser;

    public WhitelistServiceTests()
    {
        existingUser = new AppUser() { Id = existingUserId };
        var entries = new List<WhiteListEntry> { new WhiteListEntry() { UserId = existingUserId } };

        _mockRepo = MockObjects.CreateMockRepository<IWhitelistRepository, string, WhiteListEntry>(entries);
        _userManagerMock = MockObjects.GetUserManagerMock<AppUser>();
        _whitelistService = new WhitelistService(_mockRepo.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task IsWhitelistedAsync_ExistingUser_ReturnsTrue()
    {
        // Act
        var result = await _whitelistService.IsWhitelistedAsync(existingUserId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsWhitelistedAsync_NotExistingUser_ReturnsFalse()
    {
        // Act
        var result = await _whitelistService.IsWhitelistedAsync(notExistingUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddUserToWhitelistAsync_ExistingUser_AddsUserToWhitelist()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(existingUserId))
            .ReturnsAsync(existingUser);

        _mockRepo.Setup(x => x.AddAsync(It.IsAny<WhiteListEntry>()))
            .Returns(Task.FromResult(true));

        // Act
        bool addedToWhitelist = await _whitelistService.AddUserToWhitelistAsync(existingUserId);

        // Assert
        Assert.True(addedToWhitelist);
        _userManagerMock.Verify(x => x.FindByIdAsync(existingUserId), Times.Once);
        _mockRepo.Verify(x => x.AddAsync(It.IsAny<WhiteListEntry>()), Times.Once);
    }

    [Fact]
    public async Task AddUserToWhitelistAsync_NonExistingUser_ThrowsHandledException()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(notExistingUserId))
            .ReturnsAsync((AppUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<HandledException>(() => _whitelistService.AddUserToWhitelistAsync(notExistingUserId));
        _userManagerMock.Verify(x => x.FindByIdAsync(notExistingUserId), Times.Once);
        _mockRepo.Verify(x => x.AddAsync(It.IsAny<WhiteListEntry>()), Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromWhitelistAsync_UserExists_ShouldReturnTrue()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(existingUserId))
            .ReturnsAsync(existingUser);

        _mockRepo.Setup(x => x.RemoveAsync(It.IsAny<WhiteListEntry>()))
            .Returns(Task.FromResult(true));

        _mockRepo.Setup(x => x.SaveChangesAsync())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _whitelistService.RemoveUserFromWhitelistAsync(existingUserId);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(x => x.FindByIdAsync(existingUserId), Times.Once);
        _mockRepo.Verify(x => x.AsQueryable(), Times.Once);
        _mockRepo.Verify(x => x.RemoveAsync(It.IsAny<WhiteListEntry>()), Times.Once);
        _mockRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromWhitelistAsync_UserDoesNotExist_ShouldThrowHandledException()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(notExistingUserId))
            .ReturnsAsync((AppUser?)null);

        // Act & Assert
        await Assert.ThrowsAsync<HandledException>(() => _whitelistService.RemoveUserFromWhitelistAsync(notExistingUserId));

        _userManagerMock.Verify(x => x.FindByIdAsync(notExistingUserId), Times.Once);
        _mockRepo.Verify(x => x.AsQueryable(), Times.Never);
        _mockRepo.Verify(x => x.RemoveAsync(It.IsAny<WhiteListEntry>()), Times.Never);
        _mockRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }
}