namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for chat functionality including AI model settings and authentication.
/// </summary>
public class ChatOptions : IOption
{
	/// <summary>
	/// The configuration section name for chat options.
	/// </summary>
	public const string Chat = "Chat";

	/// <summary>
	/// Gets or sets a value indicating whether chat functionality is enabled.
	/// </summary>
	public bool Enabled { get; set; }

	/// <summary>
	/// Gets or sets the system message that defines the AI assistant's personality and behavior.
	/// </summary>
	public string SystemMessage { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the authentication token for accessing the AI service.
	/// </summary>
	public string AuthToken { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the AI model to use for chat responses.
	/// </summary>
	public string Model { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the maximum number of tokens allowed for chat operations.
	/// </summary>
	public int TokenLimit { get; set; } = 2000;
}
