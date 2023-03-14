using Jiro.Core.Models;

namespace Jiro.Core.Interfaces.IServices
{
    public interface IJWTService
    {
        string GenerateJsonWebToken(AppUser user, IList<string> roles);
    }
}