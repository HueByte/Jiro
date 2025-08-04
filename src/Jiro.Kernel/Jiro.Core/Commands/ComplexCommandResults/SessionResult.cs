using Jiro.Core.Services.Conversation.Models;

namespace Jiro.Core.Commands.ComplexCommandResults;

/// <summary>
/// Represents a chat session result containing session metadata and associated messages.
/// </summary>
public class SessionResult
{
	/// <summary>
	/// Gets or sets the unique identifier for the chat session.
	/// </summary>
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the date and time when the session was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the session was last updated.
	/// </summary>
	public DateTime LastUpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets the collection of trimmed messages associated with the session.
	/// </summary>
	public List<TrimmedMessageResult> Messages { get; set; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionResult"/> class.
	/// </summary>
	public SessionResult() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="SessionResult"/> class based on a session model.
	/// </summary>
	/// <param name="session">The session model to convert from.</param>
	public SessionResult(Session session)
	{
		SessionId = session.SessionId;
		CreatedAt = session.CreatedAt;
		LastUpdatedAt = session.LastUpdatedAt;
		Messages = [.. session.Messages
			.Select(static m => new TrimmedMessageResult()
			{
				Id = m.MessageId,
				Content = m.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
				IsUser = m.IsUser,
				CreatedAt = m.CreatedAt,
				Type = m.Type
			})];
	}
}
