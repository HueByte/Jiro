
using Jiro.Core.Models;

namespace Jiro.Core.Commands.ComplexCommandResults;

public class TrimmedMessageResult
{
	public string Id { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public bool IsUser { get; set; }
	public DateTime CreatedAt { get; set; }
	public MessageType Type { get; set; } = MessageType.Text;

	public TrimmedMessageResult () { }

	public TrimmedMessageResult (string id, string content, bool isUser, DateTime createdAt, MessageType type)
	{
		Id = id;
		Content = content;
		IsUser = isUser;
		CreatedAt = createdAt;
		Type = type;
	}
}
