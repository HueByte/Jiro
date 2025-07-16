namespace Jiro.App.Models;

/// <summary>
/// Represents the response for sessions.
/// </summary>
public class SessionsResponse
{
	/// <summary>
	/// Gets or sets the instance ID.
	/// </summary>
	public string InstanceId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the total number of sessions.
	/// </summary>
	public int TotalSessions { get; set; }

	/// <summary>
	/// Gets or sets the current session ID.
	/// </summary>
	public string? CurrentSessionId { get; set; }

	/// <summary>
	/// Gets or sets the list of sessions.
	/// </summary>
	public List<ChatSession> Sessions { get; set; } = new();
}
