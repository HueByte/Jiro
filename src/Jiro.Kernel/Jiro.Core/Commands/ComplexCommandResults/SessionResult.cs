using Jiro.Core.Models;
using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Commands.ComplexCommandResults;

public class SessionResult
{
	public string SessionId { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime LastUpdatedAt { get; set; }
	public List<TrimmedMessageResult> Messages { get; set; } = [];

	public SessionResult() { }

	public SessionResult(Session session)
	{
		SessionId = session.SessionId;
		CreatedAt = session.CreatedAt;
		LastUpdatedAt = session.LastUpdatedAt;
		Messages = [.. session.Messages
			.Select(m => new TrimmedMessageResult()
			{
				Id = m.MessageId,
				Content = m.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = m.IsUser,
				CreatedAt = m.CreatedAt,
				Type = m.Type
			})];
	}
}
