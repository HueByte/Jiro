namespace Jiro.Core.Models;

/// <summary>
/// Defines the different types of content that can be contained in a message.
/// </summary>
public enum MessageType
{
	/// <summary>
	/// Plain text message content.
	/// </summary>
	Text,

	/// <summary>
	/// Graph or chart content.
	/// </summary>
	Graph,

	/// <summary>
	/// Image content.
	/// </summary>
	Image,
}
