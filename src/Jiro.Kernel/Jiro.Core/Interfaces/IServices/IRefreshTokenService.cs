using Jiro.Core.Models;
using Jiro.Core.Services.Auth.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IRefreshTokenService
{
    RefreshToken CreateRefreshToken(string ipAddress);
    Task<VerifiedUser> RefreshToken(string token, string ipAddress);
    void RemoveOldRefreshTokens(AppUser user);
    Task RevokeToken(string token, string ipAddress);
}