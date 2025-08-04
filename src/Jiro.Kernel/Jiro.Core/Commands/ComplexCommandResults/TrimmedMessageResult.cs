
using Jiro.Core.Models;

namespace Jiro.Core.Commands.ComplexCommandResults;

/// <summary>
/// Represents a simplified message result containing essential message information for command responses.
/// </summary>
public class TrimmedMessageResult
{
	/// <summary>
	/// Gets or sets the unique identifier of the message.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the text content of the message.
	/// </summary>
	public string Content { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets a value indicating whether the message was sent by a user (true) or assistant (false).
	/// </summary>
	public bool IsUser { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the message was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the type of the message (text, image, etc.).
	/// </summary>
	public MessageType Type { get; set; } = MessageType.Text;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrimmedMessageResult"/> class.
	/// </summary>
	public TrimmedMessageResult() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="TrimmedMessageResult"/> class with specified values.
	/// </summary>
	/// <param name="id">The unique identifier of the message.</param>
	/// <param name="content">The text content of the message.</param>
	/// <param name="isUser">A value indicating whether the message was sent by a user.</param>
	/// <param name="createdAt">The date and time when the message was created.</param>
	/// <param name="type">The type of the message.</param>
	public TrimmedMessageResult(string id, string content, bool isUser, DateTime createdAt, MessageType type)
	{
		Id = id;
		Content = content;
		IsUser = isUser;
		CreatedAt = createdAt;
		Type = type;
	}
}
