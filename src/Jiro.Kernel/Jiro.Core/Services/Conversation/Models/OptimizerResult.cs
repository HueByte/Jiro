namespace Jiro.Core.Services.Conversation.Models;

/// <summary>
/// Represents the result of a message history optimization operation.
/// </summary>
public class OptimizerResult
{
	/// <summary>
	/// Gets or sets the number of messages that were removed during optimization.
	/// </summary>
	public int RemovedMessages { get; set; }

	/// <summary>
	/// Gets or sets a summary of the messages that were processed during optimization.
	/// </summary>
	public string MessagesSummary { get; set; } = string.Empty;
}
