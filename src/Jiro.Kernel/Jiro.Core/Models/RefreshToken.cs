using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

/// <summary>
/// Represents a refresh token used for authentication token renewal.
/// </summary>
public class RefreshToken : DbModel<int>
{
	/// <summary>
	/// Gets or sets the unique identifier for this refresh token.
	/// </summary>
	[Key]
	[JsonIgnore]
	public override int Id { get; set; }

	/// <summary>
	/// Gets or sets the token string value.
	/// </summary>
	public string? Token { get; set; }

	/// <summary>
	/// Gets or sets the date and time when this token expires.
	/// </summary>
	public DateTime Expires { get; set; }

	/// <summary>
	/// Gets or sets the date and time when this token was created.
	/// </summary>
	public DateTime Created { get; set; }

	/// <summary>
	/// Gets or sets the IP address from which this token was created.
	/// </summary>
	public string? CreatedByIp { get; set; }

	/// <summary>
	/// Gets or sets the date and time when this token was revoked, if applicable.
	/// </summary>
	public DateTime? Revoked { get; set; }

	/// <summary>
	/// Gets or sets the IP address from which this token was revoked.
	/// </summary>
	public string? RevokedByIp { get; set; }

	/// <summary>
	/// Gets or sets the reason why this token was revoked.
	/// </summary>
	public string? ReasonRevoked { get; set; }

	/// <summary>
	/// Gets a value indicating whether this token has expired.
	/// </summary>
	public bool IsExpired => DateTime.UtcNow >= Expires;

	/// <summary>
	/// Gets a value indicating whether this token has been revoked.
	/// </summary>
	public bool IsRevoked => Revoked != null;

	/// <summary>
	/// Gets a value indicating whether this token is currently active (not expired and not revoked).
	/// </summary>
	public bool IsActive => !IsRevoked && !IsExpired;
}
