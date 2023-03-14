using Jiro.Core.DTO;
using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices;

public interface IRefreshTokenService
{
    RefreshToken CreateRefreshToken(string ipAddress);
    Task<VerifiedUserDTO> RefreshToken(string token, string ipAddress);
    void RemoveOldRefreshTokens(AppUser user);
    Task RevokeToken(string token, string ipAddress);
}