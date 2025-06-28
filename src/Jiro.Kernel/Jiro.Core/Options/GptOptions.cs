using System.ComponentModel.DataAnnotations;

namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for GPT integration. This class is obsolete and should not be used in new implementations.
/// </summary>
[Obsolete]
public class GptOptions
{
	/// <summary>
	/// The configuration section name for GPT options.
	/// </summary>
	public const string Gpt = "Gpt";

	/// <summary>
	/// Gets or sets the base URL for the GPT API endpoint.
	/// </summary>
	[Required]
	public string BaseUrl { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the authentication token for API access.
	/// </summary>
	public string AuthToken { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the organization identifier for the API.
	/// </summary>
	public string? Organization { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether fine-tuning is enabled.
	/// </summary>
	public bool FineTune { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to use ChatGPT instead of single GPT.
	/// </summary>
	[Required]
	public bool UseChatGpt { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether GPT functionality is enabled.
	/// </summary>
	[Required]
	public bool Enable { get; set; }

	/// <summary>
	/// Gets or sets the configuration options for ChatGPT.
	/// </summary>
	public ChatGptOptions? ChatGpt { get; set; }

	/// <summary>
	/// Gets or sets the configuration options for single GPT.
	/// </summary>
	public SingleGptOptions? SingleGpt { get; set; }
}

/// <summary>
/// Configuration options for ChatGPT functionality. This class is obsolete and should not be used in new implementations.
/// </summary>
[Obsolete]
public class ChatGptOptions
{
	/// <summary>
	/// The configuration section name for ChatGPT options.
	/// </summary>
	public const string ChatGpt = "ChatGpt";

	/// <summary>
	/// Gets or sets the system message that defines the AI's behavior.
	/// </summary>
	public string SystemMessage { get; set; } = string.Empty;
}

/// <summary>
/// Configuration options for single GPT functionality. This class is obsolete and should not be used in new implementations.
/// </summary>
[Obsolete]
public class SingleGptOptions
{
	/// <summary>
	/// The configuration section name for single GPT options.
	/// </summary>
	public const string SingleGpt = "SingleGpt";

	/// <summary>
	/// Gets or sets the maximum number of tokens to generate in responses.
	/// </summary>
	public int TokenLimit { get; set; } = 500;

	/// <summary>
	/// Gets or sets the context message that provides background information for the AI.
	/// </summary>
	public string ContextMessage { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the stop sequence that indicates when generation should end.
	/// </summary>
	public string? Stop { get; set; }

	/// <summary>
	/// Gets or sets the AI model to use for generation.
	/// </summary>
	public string? Model { get; set; }
}
