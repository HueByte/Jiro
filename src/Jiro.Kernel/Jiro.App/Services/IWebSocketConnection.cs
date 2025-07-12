namespace Jiro.App.Services;

/// <summary>
/// Interface for WebSocket connection management
/// </summary>
public interface IWebSocketConnection : IDisposable
{
	/// <summary>
	/// Event fired when connection is closed
	/// </summary>
	event Func<Exception?, Task>? Closed;

	/// <summary>
	/// Event fired when connection is reconnecting
	/// </summary>
	event Func<Exception?, Task>? Reconnecting;

	/// <summary>
	/// Event fired when connection is reconnected
	/// </summary>
	event Func<string?, Task>? Reconnected;

	/// <summary>
	/// Gets the current connection state
	/// </summary>
	bool IsConnected { get; }

	/// <summary>
	/// Starts the WebSocket connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	Task StartAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops the WebSocket connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	Task StopAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Registers a handler for incoming commands
	/// </summary>
	/// <param name="handler">Command handler function</param>
	void OnCommand(Func<CommandMessage, Task> handler);
}
