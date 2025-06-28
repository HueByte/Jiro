using Jiro.Core.Models;

using OpenAI.Chat;

namespace Jiro.Core.Services.Conversation.Models;

public class ChatMessageWithMetadata
{
	public string MessageId { get; set; } = string.Empty;
	public bool IsUser { get; set; }
	public MessageType Type { get; set; }
	public ChatMessage Message { get; set; } = default!;
	public DateTime CreatedAt { get; set; }
}
