using Jiro.Core.Attributes;

namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for bot-related settings including authentication tokens and message limits.
/// </summary>
public class BotOptions : IOption
{
	/// <summary>
	/// The configuration section name for bot options.
	/// </summary>
	public const string Bot = "Bot";

	/// <summary>
	/// Gets or sets the bot authentication token. This value is anonymized in logs.
	/// </summary>
	[Anomify]
	public string Token { get; set; } = default!;

	/// <summary>
	/// Gets or sets the OpenAI API key for chat functionality. This value is anonymized in logs.
	/// </summary>
	[Anomify]
	public string OpenAIKey { get; set; } = default!;

	/// <summary>
	/// Gets or sets the number of messages to fetch in chat operations.
	/// </summary>
	public int MessageFetchCount { get; set; } = default!;

	/// <summary>
	/// Gets or sets the maximum number of tokens allowed for AI model requests.
	/// </summary>
	public int MaxTokens { get; set; }
}
