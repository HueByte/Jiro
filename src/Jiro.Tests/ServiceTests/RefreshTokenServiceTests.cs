using Jiro.Core;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Jiro.Core.Services.Auth;
using Jiro.Tests.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Jiro.Tests.ServiceTests;

public class RefreshTokenServiceTests
{
    private readonly JWTOptions _jwtOptions;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly Mock<IJWTService> _jwtMock;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;

    public RefreshTokenServiceTests()
    {
        var jwtOptionsMock = new Mock<IOptions<JWTOptions>>();
        _jwtOptions = new JWTOptions { RefreshTokenExpireTime = 30 };
        jwtOptionsMock.Setup(j => j.Value).Returns(_jwtOptions);

        _userManagerMock = MockObjects.GetUserManagerMock<AppUser>();
        _jwtMock = new Mock<IJWTService>();

        _refreshTokenService = new RefreshTokenService(jwtOptionsMock.Object, _userManagerMock.Object, _jwtMock.Object);
    }

    [Fact]
    public void CreateRefreshToken_ReturnsRefreshTokenWithValidProperties()
    {
        // Arrange
        var ipAddress = "127.0.0.1";

        // Act
        var refreshToken = _refreshTokenService.CreateRefreshToken(ipAddress);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.NotNull(refreshToken.Token);
        Assert.NotEmpty(refreshToken.Token);
        Assert.Equal(ipAddress, refreshToken.CreatedByIp);
        Assert.Equal(DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpireTime), refreshToken.Expires, TimeSpan.FromSeconds(1));
        Assert.Equal(DateTime.UtcNow, refreshToken.Created, TimeSpan.FromSeconds(1));
    }
}
