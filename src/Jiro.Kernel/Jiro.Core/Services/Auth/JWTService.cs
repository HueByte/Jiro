using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jiro.Core.Models;
using Jiro.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Jiro.Core.Services.Auth;

public class JWTService : IJWTService
{
    private readonly JWTOptions _jwtOptions;
    public JWTService(IOptions<JWTOptions> options)
    {
        _jwtOptions = options.Value;
    }

    // TODO: consider email/username choice system configurable
    public string GenerateJsonWebToken(AppUser user, IList<string> roles)
    {
        if (user is null)
            throw new HandledException($"User is empty");

        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var token = new JwtSecurityToken(
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpireTime),
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}