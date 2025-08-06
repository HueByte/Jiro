using Jiro.Core.Models;
using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation.Models;

/// <summary>
/// Represents a chat message with additional metadata for conversation management.
/// </summary>
public class ChatMessageWithMetadata
{
	/// <summary>
	/// Gets or sets the unique identifier for the message.
	/// </summary>
	public string MessageId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether this message was sent by a user.
	/// </summary>
	public bool IsUser { get; set; }

	/// <summary>
	/// Gets or sets the type of the message content.
	/// </summary>
	public MessageType Type { get; set; }

	/// <summary>
	/// Gets or sets the actual chat message content from the OpenAI library.
	/// </summary>
	public ChatMessage Message { get; set; } = default!;

	/// <summary>
	/// Gets or sets the timestamp when this message was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }
}
