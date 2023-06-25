using System.Security.Cryptography;
using Jiro.Core.DTO;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.Auth;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly JWTOptions _jwtOptions;
    private readonly UserManager<AppUser> _userManager;
    private readonly IJWTService _jwtAuth;
    public RefreshTokenService(IOptions<JWTOptions> jwtOptions, UserManager<AppUser> userManager, IJWTService jwtAuthentication)
    {
        _jwtOptions = jwtOptions.Value;
        _userManager = userManager;
        _jwtAuth = jwtAuthentication;
    }

    public RefreshToken CreateRefreshToken(string ipAddress)
    {
        var randomSeed = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomSeed);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomSeed),
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.RefreshTokenExpireTime),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };
    }

    public async Task<VerifiedUser> RefreshToken(string token, string ipAddress)
    {
        var user = await GetUserByRefreshToken(token)
            ?? throw new TokenException("Couldn't find user with provided refresh token");

        user.RefreshTokens ??= new();

        // Get matching token
        var oldRefreshToken = user.RefreshTokens.FirstOrDefault(e => e.Token == token);

        if (oldRefreshToken is null || !oldRefreshToken.IsActive)
            throw new TokenException("Token was not found or is not active");

        // Get new refresh token and revoke old one
        var newRefreshToken = RotateToken(oldRefreshToken, ipAddress);
        user.RefreshTokens.Add(newRefreshToken);

        // Remove old tokens
        RemoveOldRefreshTokens(user);
        await _userManager.UpdateAsync(user);

        var roles = user.UserRoles.Select(e => e.Role.Name).ToList();
        var jwtToken = _jwtAuth.GenerateJsonWebToken(user, roles!);

        return new VerifiedUser
        {
            Username = user.UserName,
            Token = jwtToken,
            AccessTokenExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpireTime),
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiration = newRefreshToken.Expires,
            Roles = roles?.ToArray()!
        };
    }

    public async Task RevokeToken(string token, string ipAddress)
    {
        if (string.IsNullOrEmpty(token))
            throw new TokenException("Revoking token was empty");

        var user = await GetUserByRefreshToken(token);
        var refreshToken = user.RefreshTokens?.FirstOrDefault(e => e.Token == token);

        if (refreshToken is null || !refreshToken.IsActive)
            throw new TokenException("Token is invalid");

        RevokeRefreshToken(refreshToken, ipAddress);

        await _userManager.UpdateAsync(user);
    }

    /// <summary>
    /// removes old refresh tokens for user based on RefreshTokenExpireTime setting 
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public void RemoveOldRefreshTokens(AppUser user)
    {
        user.RefreshTokens!
            .RemoveAll(token =>
                !token.IsActive
                && token.Created.AddDays(_jwtOptions.RefreshTokenExpireTime) <= DateTime.UtcNow);
    }

    /// <summary>
    /// revokes refresh token with given reason
    /// </summary>
    /// <param name="token"></param>
    /// <param name="ipAddress"></param>
    /// <param name="reason"></param>
    private static void RevokeRefreshToken(RefreshToken token, string ipAddress, string? reason = null)
    {
        token.Revoked = DateTime.UtcNow;
        token.RevokedByIp = ipAddress;
        token.ReasonRevoked = reason;
    }

    /// <summary>
    /// Expires old refresh token and returns a new one
    /// </summary>
    /// <param name="oldToken"></param>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private RefreshToken RotateToken(RefreshToken oldToken, string ipAddress)
    {
        var newRefreshToken = CreateRefreshToken(ipAddress);
        RevokeRefreshToken(oldToken, ipAddress, "Rotated Token");
        return newRefreshToken;
    }

    /// <summary>
    /// fetches user by given refresh token
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="TokenException"></exception>
    private Task<AppUser?> GetUserByRefreshToken(string token)
    {
        return _userManager.Users
            .Include(e => e.RefreshTokens)
            .Include(e => e.UserRoles)
            .ThenInclude(e => e.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(user => user.RefreshTokens != null && user.RefreshTokens.Any(t => t.Token == token));
    }
}
