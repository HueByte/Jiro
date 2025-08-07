namespace Jiro.Core.DTO;

/// <summary>
/// Data transfer object for Jiro prompt requests containing user input.
/// </summary>
public class JiroPromptDTO
{
	/// <summary>
	/// Gets or sets the user prompt or command to be processed.
	/// </summary>
	public string Prompt { get; set; } = string.Empty;
}
