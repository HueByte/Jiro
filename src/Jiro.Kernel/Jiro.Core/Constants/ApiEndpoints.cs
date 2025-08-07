namespace Jiro.Core.Constants;

/// <summary>
/// Contains API endpoint constants for external service integrations.
/// These endpoints are deprecated and marked for removal.
/// </summary>
public class ApiEndpoints
{
	/// <summary>
	/// The API endpoint for GPT completions. This endpoint is obsolete and should not be used.
	/// </summary>
	[Obsolete]
	public const string GPT_COMPLETIONS = "completions";

	/// <summary>
	/// The API endpoint for ChatGPT completions. This endpoint is obsolete and should not be used.
	/// </summary>
	[Obsolete]
	public const string CHAT_GPT_COMPLETIONS = "chat/completions";
}
