namespace Jiro.App.Models;

/// <summary>
/// Represents a chat session.
/// </summary>
public class ChatSession
{
	/// <summary>
	/// Gets or sets the session ID.
	/// </summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the session name.
	/// </summary>
	public string SessionName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the timestamp of the session creation.
	/// </summary>
	public DateTime CreatedAt { get; set; }
}
