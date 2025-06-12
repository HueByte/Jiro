namespace Jiro.Core.DTO;

public class WhitelistedUserDTO
{
	public string Id { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public bool IsWhitelisted
	{
		get; set;
	}
}
