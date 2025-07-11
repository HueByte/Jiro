using System.Text.Json;

using Jiro.App.Options;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.App.Services;

/// <summary>
/// SignalR implementation of the WebSocket connection interface
/// </summary>
public class SignalRWebSocketConnection : IWebSocketConnection
{
	private readonly ILogger<SignalRWebSocketConnection> _logger;
	private readonly WebSocketOptions _options;
	private HubConnection? _connection;
	private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
	private bool _disposed = false;
	private Func<CommandMessage, Task>? _commandHandler;

	/// <summary>
	/// Event fired when connection is closed
	/// </summary>
	public event Func<Exception?, Task>? Closed;

	/// <summary>
	/// Event fired when connection is reconnecting
	/// </summary>
	public event Func<Exception?, Task>? Reconnecting;

	/// <summary>
	/// Event fired when connection is reconnected
	/// </summary>
	public event Func<string?, Task>? Reconnected;

	/// <summary>
	/// Gets the current connection state
	/// </summary>
	public bool IsConnected => _connection?.State == HubConnectionState.Connected;

	/// <summary>
	/// Initializes a new instance of the SignalRWebSocketConnection
	/// </summary>
	/// <param name="logger">The logger</param>
	/// <param name="options">The WebSocket configuration options</param>
	public SignalRWebSocketConnection(
		ILogger<SignalRWebSocketConnection> logger,
		IOptions<WebSocketOptions> options)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
	}

	/// <summary>
	/// Starts the WebSocket connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		await _connectionSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (_connection?.State == HubConnectionState.Connected)
			{
				_logger.LogDebug("Already connected to SignalR hub");
				return;
			}

			if (_connection != null)
			{
				await DisposeConnectionAsync();
			}

			_logger.LogInformation("Connecting to SignalR hub at {Url}", _options.HubUrl);

			_connection = new HubConnectionBuilder()
				.WithUrl(_options.HubUrl, options =>
				{
					// Configure authentication if token is provided
					if (!string.IsNullOrEmpty(_options.AccessToken))
					{
						options.AccessTokenProvider = () => Task.FromResult<string?>(_options.AccessToken);
					}

					// Configure additional headers if provided
					if (_options.Headers?.Count > 0)
					{
						foreach (var header in _options.Headers)
						{
							options.Headers.Add(header.Key, header.Value);
						}
					}
				})
				.WithAutomaticReconnect(new[]
				{
					TimeSpan.Zero,
					TimeSpan.FromSeconds(2),
					TimeSpan.FromSeconds(10),
					TimeSpan.FromSeconds(30)
				})
				.ConfigureLogging(logging =>
				{
					logging.SetMinimumLevel(LogLevel.Information);
				})
				.Build();

			// Set up event handlers
			SetupEventHandlers();

			// Connect
			await _connection.StartAsync(cancellationToken);

			_logger.LogInformation("Successfully connected to SignalR hub");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to connect to SignalR hub");
			throw;
		}
		finally
		{
			_connectionSemaphore.Release();
		}
	}

	/// <summary>
	/// Stops the WebSocket connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public async Task StopAsync(CancellationToken cancellationToken = default)
	{
		await _connectionSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (_connection != null)
			{
				_logger.LogInformation("Disconnecting from SignalR hub");
				await DisposeConnectionAsync();
				_logger.LogInformation("Disconnected from SignalR hub");
			}
		}
		finally
		{
			_connectionSemaphore.Release();
		}
	}

	/// <summary>
	/// Registers a handler for incoming commands
	/// </summary>
	/// <param name="handler">Command handler function</param>
	public void OnCommand(Func<CommandMessage, Task> handler)
	{
		_commandHandler = handler;
	}

	/// <summary>
	/// Sets up SignalR event handlers
	/// </summary>
	private void SetupEventHandlers()
	{
		if (_connection == null) return;

		// Handle command reception - expecting JSON string that contains CommandMessage
		_connection.On<string>("ReceiveCommand", async (commandJson) =>
		{
			try
			{
				_logger.LogDebug("Received command from SignalR hub: {Command}", commandJson);

				// Deserialize the command message
				var commandMessage = JsonSerializer.Deserialize<CommandMessage>(commandJson, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				if (commandMessage != null && _commandHandler != null)
				{
					await _commandHandler.Invoke(commandMessage);
				}
				else
				{
					_logger.LogWarning("Failed to deserialize command message or no handler registered");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing received command: {Command}", commandJson);
			}
		});

		// Handle connection closed
		_connection.Closed += async (exception) =>
		{
			if (exception != null)
			{
				_logger.LogError(exception, "SignalR connection closed with error");
			}
			else
			{
				_logger.LogInformation("SignalR connection closed");
			}

			if (Closed != null)
			{
				await Closed.Invoke(exception);
			}
		};

		// Handle reconnecting
		_connection.Reconnecting += async (exception) =>
		{
			_logger.LogWarning(exception, "SignalR connection reconnecting");

			if (Reconnecting != null)
			{
				await Reconnecting.Invoke(exception);
			}
		};

		// Handle reconnected
		_connection.Reconnected += async (connectionId) =>
		{
			_logger.LogInformation("SignalR connection reconnected with ID: {ConnectionId}", connectionId);

			if (Reconnected != null)
			{
				await Reconnected.Invoke(connectionId);
			}
		};
	}

	/// <summary>
	/// Disposes the current connection
	/// </summary>
	/// <returns>A task representing the disposal operation</returns>
	private async Task DisposeConnectionAsync()
	{
		if (_connection != null)
		{
			try
			{
				if (_connection.State == HubConnectionState.Connected)
				{
					await _connection.StopAsync();
				}
				await _connection.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing SignalR connection");
			}
			finally
			{
				_connection = null;
			}
		}
	}

	/// <summary>
	/// Disposes the SignalRWebSocketConnection
	/// </summary>
	public void Dispose()
	{
		if (!_disposed)
		{
			try
			{
				StopAsync().GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during disposal");
			}
			finally
			{
				_connectionSemaphore.Dispose();
				_disposed = true;
			}
		}
	}
}
