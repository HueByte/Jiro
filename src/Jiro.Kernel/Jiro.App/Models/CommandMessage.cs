using System.Text.Json.Serialization;

namespace Jiro.App.Models;

/// <summary>
/// Represents a command message received via WebSocket
/// </summary>
public class CommandMessage
{
	/// <summary>
	/// The unique identifier of the instance
	/// </summary>
	[JsonPropertyName("instanceId")]
	public string InstanceId { get; set; } = string.Empty;

	/// <summary>
	/// The command to execute
	/// </summary>
	[JsonPropertyName("command")]
	public string Command { get; set; } = string.Empty;

	/// <summary>
	/// The unique synchronization ID for the command
	/// </summary>
	[JsonPropertyName("commandSyncId")]
	public string CommandSyncId { get; set; } = string.Empty;

	/// <summary>
	/// The session ID associated with the command
	/// </summary>
	[JsonPropertyName("sessionId")]
	public string SessionId { get; set; } = string.Empty;

	/// <summary>
	/// Parameters for the command
	/// </summary>
	[JsonPropertyName("parameters")]
	public Dictionary<string, string> Parameters { get; set; } = new();
}
