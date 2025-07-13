using Jiro.Core.Abstraction;

namespace Jiro.Core.Models;

/// <summary>
/// Represents a message in a chat session, containing user or assistant content.
/// </summary>
public class Message : DbModel<string>
{
	/// <summary>
	/// Gets or sets the unique identifier for the message. Must be set explicitly.
	/// </summary>
	public override string Id { get; set; } = default!;

	/// <summary>
	/// Gets or sets the content of the message.
	/// </summary>
	public string Content { get; set; } = default!;

	/// <summary>
	/// Gets or sets the instance identifier associated with this message.
	/// </summary>
	public string InstanceId { get; set; } = default!;

	/// <summary>
	/// Gets or sets the session identifier that this message belongs to.
	/// </summary>
	public string SessionId { get; set; } = default!;

	/// <summary>
	/// Gets or sets a value indicating whether this message was sent by a user.
	/// </summary>
	public bool IsUser { get; set; }

	/// <summary>
	/// Gets or sets the timestamp when this message was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the type of the message (text, graph, image, etc.).
	/// </summary>
	public MessageType Type { get; set; }
}
