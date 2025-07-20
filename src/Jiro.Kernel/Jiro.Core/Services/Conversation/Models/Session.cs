namespace Jiro.Core.Services.Conversation.Models;

public class Session
{
	public string InstanceId { get; set; } = string.Empty;
	public string SessionId { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime LastUpdatedAt { get; set; }
	public List<ChatMessageWithMetadata> Messages { get; set; } = [];
}
