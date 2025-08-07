namespace Jiro.Core.Services.Conversation.Models;

/// <summary>
/// Represents a chat session with its metadata and associated messages.
/// </summary>
public class Session
{
	/// <summary>
	/// Gets or sets the unique identifier of the instance that owns this session.
	/// </summary>
	public string InstanceId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier of the session.
	/// </summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the display name of the session.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the date and time when the session was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the session was last updated.
	/// </summary>
	public DateTime LastUpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets the collection of chat messages associated with this session.
	/// </summary>
	public List<ChatMessageWithMetadata> Messages { get; set; } = [];
}
