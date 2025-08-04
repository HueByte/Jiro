namespace Jiro.Core.Services.Supervisor;

/// <summary>
/// Service for supervising and monitoring application communication events and connection state.
/// </summary>
public class SupervisorService
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
	/// <summary>
	/// Event triggered when a connection is established.
	/// </summary>
	public event Func<Task>? OnConnected = null;

	/// <summary>
	/// Event triggered when a connection is disconnected.
	/// </summary>
	public event Func<Task>? OnDisconnected = null;

	/// <summary>
	/// Event triggered when a message is received.
	/// </summary>
	public event Func<string, Task>? OnMessageReceived = null;

	/// <summary>
	/// Event triggered when a message is sent.
	/// </summary>
	public event Func<string, Task>? OnMessageSent = null;
#pragma warning restore CS0414 // Field is assigned but its value is never used
}
