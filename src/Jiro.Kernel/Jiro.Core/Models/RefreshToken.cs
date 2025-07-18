using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

public class RefreshToken : DbModel<int>
{
	[Key]
	[JsonIgnore]
	public override int Id { get; set; }
	public string? Token { get; set; }
	public DateTime Expires { get; set; }
	public DateTime Created { get; set; }
	public string? CreatedByIp { get; set; }
	public DateTime? Revoked { get; set; }
	public string? RevokedByIp { get; set; }
	public string? ReasonRevoked { get; set; }
	public bool IsExpired => DateTime.UtcNow >= Expires;
	public bool IsRevoked => Revoked != null;
	public bool IsActive => !IsRevoked && !IsExpired;
}
