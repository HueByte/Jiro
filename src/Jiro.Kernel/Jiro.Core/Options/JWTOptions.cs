namespace Jiro.Core.Options;

public class JWTOptions : IOption
{
	public const string JWT = "JWT";

	public string? Issuer { get; set; }
	public string? Audience { get; set; }
	public string Secret { get; set; } = default!;
	public double AccessTokenExpireTime { get; set; }
	public double RefreshTokenExpireTime { get; set; }
}
