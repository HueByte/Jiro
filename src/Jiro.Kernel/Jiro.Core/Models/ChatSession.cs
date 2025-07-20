using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

/// <summary>
/// Represents a chat session containing conversation messages and metadata.
/// </summary>
public class ChatSession : DbModel<string>
{
	/// <summary>
	/// Gets or sets the name of the chat session.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the description of the chat session.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the collection of messages in this chat session.
	/// </summary>
	public List<Message> Messages { get; set; } = [];

	/// <summary>
	/// Gets or sets the date and time when the chat session was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the chat session was last updated.
	/// </summary>
	public DateTime LastUpdatedAt { get; set; }
}
