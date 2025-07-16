using System.Text.Json;

using Jiro.App.Models;
using Jiro.App.Options;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.System;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.App.Services;

/// <summary>
/// SignalR implementation of the WebSocket connection interface
/// </summary>
public class WebSocketConnection : IWebSocketConnection
{
	private readonly ILogger<WebSocketConnection> _logger;
	private readonly WebSocketOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
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
	/// <param name="scopeFactory">The service scope factory for creating scoped services</param>
	public WebSocketConnection(
		ILogger<WebSocketConnection> logger,
		IOptions<WebSocketOptions> options,
		IServiceScopeFactory scopeFactory)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
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
				_logger.LogDebug("Already connected to hub");
				return;
			}

			if (_connection != null)
			{
				await DisposeConnectionAsync();
			}

			_logger.LogInformation("Connecting to hub at {Url}", _options.HubUrl);

			// Ensure API key is provided for authentication
			if (string.IsNullOrEmpty(_options.ApiKey))
			{
				throw new InvalidOperationException("API key is required for WebSocket authentication. Please configure 'WebSocket:ApiKey' or 'API_KEY' in your settings.");
			}

			// Build the hub URL with API key query parameter
			string hubUrl = _options.HubUrl;
			var separator = hubUrl.Contains('?') ? "&" : "?";
			hubUrl = $"{hubUrl}{separator}api_key={Uri.EscapeDataString(_options.ApiKey)}";

			_connection = new HubConnectionBuilder()
				.WithUrl(hubUrl, options =>
				{
					// Configure additional headers if provided
					if (_options.Headers?.Count > 0)
					{
						foreach (var header in _options.Headers)
						{
							options.Headers.Add(header.Key, header.Value);
						}
					}
				})
				// Custom reconnection intervals: 0s, 2s, 10s, 30s, 60s, then always 60s
				.WithAutomaticReconnect(new SocketRetryPolicy())
				.ConfigureLogging(logging =>
				{
					logging.SetMinimumLevel(LogLevel.Information);
				})
				.Build();

			// Set up event handlers
			SetupEventHandlers();

			// Connect
			await _connection.StartAsync(cancellationToken);

			_logger.LogInformation("Successfully connected to hub");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to connect to hub");
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
				_logger.LogInformation("Disconnecting from hub");
				await DisposeConnectionAsync();
				_logger.LogInformation("Disconnected from hub");
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

		// TODO: Figure out some nicer system for resolving dependency injection in SignalR handlers
		// Handle command reception - expecting JSON string that contains CommandMessage
		_connection.On<string>("ReceiveCommand", async (commandJson) =>
		{
			try
			{
				_logger.LogDebug("Received command from hub: {Command}", commandJson);

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

		// Handle keepalive acknowledgment from server
		_connection.On("KeepaliveAck", async () =>
		{
			try
			{
				_logger.LogDebug("Received keepalive acknowledgment from server");

				// Send acknowledgment back to server via SignalR
				await _connection.InvokeAsync("KeepaliveResponse", new
				{
					timestamp = DateTime.UtcNow,
					status = "acknowledged"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling keepalive acknowledgment");
			}
		});

		// Handle GetLogs command from server
		_connection.On<string, int>("GetLogs", async (level, limit) =>
		{
			try
			{
				_logger.LogInformation("Received GetLogs command from server - Level: {Level}, Limit: {Limit}", level, limit);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var logsService = scope.ServiceProvider.GetRequiredService<ILogsProviderService>();

				try
				{
					var logsResponse = await logsService.GetLogsAsync(level, limit);

					var response = new LogsResponse
					{
						TotalLogs = logsResponse.TotalLogs,
						Level = logsResponse.Level,
						Limit = logsResponse.Limit,
						Logs = logsResponse.Logs.Select(log => new LogEntry
						{
							File = log.File,
							Timestamp = log.Timestamp,
							Level = log.Level,
							Message = log.Message
						}).ToList()
					};

					await _connection.InvokeAsync("LogsResponse", response);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving logs");
					var errorResponse = new ErrorResponse
					{
						CommandName = "GetLogs",
						ErrorMessage = $"Error retrieving logs: {ex.Message}"
					};
					await _connection.InvokeAsync("ErrorResponse", errorResponse);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetLogs command from server");
			}
		});

		// Handle GetSessions command from server
		_connection.On("GetSessions", async () =>
		{
			try
			{
				_logger.LogInformation("Received GetSessions command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				try
				{
					var instanceId = configuration.GetValue<string>("INSTANCE_ID") ?? Environment.MachineName;
					var sessions = await messageManager.GetChatSessionsAsync(instanceId);

					var response = new SessionsResponse
					{
						InstanceId = instanceId,
						TotalSessions = sessions.Count,
						CurrentSessionId = null, // TODO: Get current session ID from context or configuration
						Sessions = sessions.Select(session => new ChatSession
						{
							SessionId = session.Id, // Mapping DbModel<string> Id to SessionId
							SessionName = session.Name, // Mapping Name to SessionName
							CreatedAt = session.CreatedAt
						}).ToList()
					};

					await _connection.InvokeAsync("SessionsResponse", response);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving sessions");
					var errorResponse = new ErrorResponse
					{
						CommandName = "GetSessions",
						ErrorMessage = $"Error retrieving sessions: {ex.Message}"
					};
					await _connection.InvokeAsync("ErrorResponse", errorResponse);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetSessions command from server");
				var errorResponse = new ErrorResponse
				{
					CommandName = "GetSessions",
					ErrorMessage = $"Error handling GetSessions command: {ex.Message}"
				};
				await _connection.InvokeAsync("ErrorResponse", errorResponse);
			}
		});

		// Handle GetConfig command from server
		_connection.On("GetConfig", async () =>
		{
			try
			{
				_logger.LogInformation("Received GetConfig command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var configService = scope.ServiceProvider.GetRequiredService<IConfigProviderService>();

				try
				{
					var configResponse = await configService.GetConfigAsync();

					await _connection.InvokeAsync("ConfigResponse", configResponse);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving configuration");
					var errorResponse = new ErrorResponse
					{
						CommandName = "GetConfig",
						ErrorMessage = $"Error retrieving configuration: {ex.Message}"
					};
					await _connection.InvokeAsync("ErrorResponse", errorResponse);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetConfig command from server");
			}
		});

		// Handle UpdateConfig command from server
		_connection.On<string>("UpdateConfig", async (configJson) =>
		{
			try
			{
				_logger.LogInformation("Received UpdateConfig command from server with config: {Config}", configJson);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var configService = scope.ServiceProvider.GetRequiredService<IConfigProviderService>();

				try
				{
					var updateResponse = await configService.UpdateConfigAsync(configJson);

					await _connection.InvokeAsync("ConfigUpdateResponse", updateResponse);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating configuration");
					var errorResponse = new ErrorResponse
					{
						CommandName = "UpdateConfig",
						ErrorMessage = $"Error updating configuration: {ex.Message}"
					};
					await _connection.InvokeAsync("ErrorResponse", errorResponse);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling UpdateConfig command from server");
			}
		});

		// Handle GetCustomThemes command from server
		_connection.On("GetCustomThemes", async () =>
		{
			try
			{
				_logger.LogInformation("Received GetCustomThemes command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

				try
				{
					var themeResponse = await themeService.GetCustomThemesAsync();

					await _connection.InvokeAsync("ThemesResponse", themeResponse);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving custom themes");
					var errorResponse = new ErrorResponse
					{
						CommandName = "GetCustomThemes",
						ErrorMessage = $"Error retrieving custom themes: {ex.Message}"
					};
					await _connection.InvokeAsync("ErrorResponse", errorResponse);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCustomThemes command from server");
			}
		});

		// Handle GetCommandsMetadata command from server
		_connection.On("GetCommandsMetadata", async () =>
		{
			try
			{
				_logger.LogInformation("Received GetCommandsMetadata command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();

				try
				{
					var helpService = scope.ServiceProvider.GetRequiredService<IHelpService>();
					var response = helpService.CommandMeta;

					// Send response back to server via SignalR
					await _connection.InvokeAsync("CommandsMetadataResponse", response);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving commands metadata");
					await _connection.InvokeAsync("ErrorResponse", "GetCommandsMetadata", $"Error retrieving commands metadata: {ex.Message}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCommandsMetadata command from server");
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
