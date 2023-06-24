using System.Text.Json.Serialization;
using Jiro.Core.Models;

namespace Jiro.Core;

public class VerifiedUser
{
    public string? Username { get; set; }
    public string[]? Roles { get; set; }
    public string? Email { get; set; }

    [JsonIgnore]
    public string? RefreshToken { get; set; }

    [JsonIgnore]
    public DateTime RefreshTokenExpiration { get; set; }

    [JsonIgnore]
    public string? Token { get; set; }

    [JsonIgnore]
    public DateTime AccessTokenExpiration { get; set; }

    public VerifiedUser() { }

    public VerifiedUser(AppUser user, string[] roles, string jwtToken, string email, RefreshToken refreshToken, DateTime accessTokenExireDate)
    {
        Username = user.UserName;
        Roles = roles;
        Token = jwtToken;
        Email = email;
        RefreshToken = refreshToken.Token;
        RefreshTokenExpiration = refreshToken.Expires;
        AccessTokenExpiration = accessTokenExireDate;
    }
}
