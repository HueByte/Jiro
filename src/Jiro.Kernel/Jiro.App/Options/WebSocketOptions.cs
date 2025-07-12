namespace Jiro.App.Options;

/// <summary>
/// Configuration options for WebSocket communication
/// </summary>
public class WebSocketOptions
{
	/// <summary>
	/// The URL of the SignalR hub
	/// </summary>
	public string HubUrl { get; set; } = "https://localhost:5001/commandHub";

	/// <summary>
	/// Delay between reconnection attempts in milliseconds
	/// </summary>
	public int ReconnectionDelayMs { get; set; } = 5000;

	/// <summary>
	/// Maximum number of reconnection attempts
	/// </summary>
	public int MaxReconnectionAttempts { get; set; } = 5;

	/// <summary>
	/// Handshake timeout in milliseconds
	/// </summary>
	public int HandshakeTimeoutMs { get; set; } = 15000;

	/// <summary>
	/// Keep alive interval in milliseconds
	/// </summary>
	public int KeepAliveIntervalMs { get; set; } = 15000;

	/// <summary>
	/// Server timeout in milliseconds
	/// </summary>
	public int ServerTimeoutMs { get; set; } = 30000;

	/// <summary>
	/// API key for authentication
	/// </summary>
	public string? ApiKey { get; set; }

	/// <summary>
	/// Additional headers to send with the connection
	/// </summary>
	public Dictionary<string, string> Headers { get; set; } = new();
}
