using System.Text.Json;

using Jiro.App.Options;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.System;
using Jiro.Shared.Websocket;
using Jiro.Shared.Websocket.Requests;
using Jiro.Shared.Websocket.Responses;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static Jiro.Shared.Websocket.Endpoints;

namespace Jiro.App.Services;

/// <summary>
/// SignalR implementation of the WebSocket connection interface
/// </summary>
public class WebSocketConnection : IJiroClientHub, IDisposable
{
	private readonly ILogger<WebSocketConnection> _logger;
	private readonly WebSocketOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ICommandHandlerService _commandHandler;
	private HubConnection? _connection;
	private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
	private bool _disposed = false;

	#region Connection Lifecycle Events

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

	#endregion

	#region Command Events

	/// <summary>
	/// Event fired when a command is received from the server
	/// </summary>
	public event Func<CommandMessage, Task>? CommandReceived;

	/// <summary>
	/// Event fired when a keepalive acknowledgment is received
	/// </summary>
	public event Func<Task>? KeepaliveAckReceived;

	#endregion

	#region Request Events

	/// <summary>
	/// Event fired when a logs request is received from the server
	/// </summary>
	public event Func<GetLogsRequest, Task>? LogsRequested;

	/// <summary>
	/// Event fired when a session request is received from the server
	/// </summary>
	public event Func<GetSessionRequest, Task>? SessionRequested;

	/// <summary>
	/// Event fired when a sessions request is received from the server
	/// </summary>
	public event Func<GetSessionsRequest, Task>? SessionsRequested;

	/// <summary>
	/// Event fired when a config request is received from the server
	/// </summary>
	public event Func<GetConfigRequest, Task>? ConfigRequested;

	/// <summary>
	/// Event fired when a config update request is received from the server
	/// </summary>
	public event Func<UpdateConfigRequest, Task>? ConfigUpdateRequested;

	/// <summary>
	/// Event fired when a custom themes request is received from the server
	/// </summary>
	public event Func<GetCustomThemesRequest, Task>? CustomThemesRequested;

	/// <summary>
	/// Event fired when a commands metadata request is received from the server
	/// </summary>
	public event Func<GetCommandsMetadataRequest, Task>? CommandsMetadataRequested;

	/// <summary>
	/// Event fired when a log files request is received from the server
	/// </summary>
	public event Func<Task>? LogFilesRequested;

	/// <summary>
	/// Event fired when a log count request is received from the server
	/// </summary>
	public event Func<Task>? LogCountRequested;

	#endregion

	/// <summary>
	/// Gets the current connection state
	/// </summary>
	public bool IsConnected => _connection?.State == HubConnectionState.Connected;

	/// <summary>
	/// Initializes a new instance of the SignalRWebSocketConnection
	/// </summary>
	/// <param name="logger">The logger</param>
	/// <param name="options">The WebSocket configuration options</param>
	/// <param name="scopeFactory">Service scope factory for creating scoped services</param>
	/// <param name="commandHandler">Command handler service</param>
	public WebSocketConnection(
		ILogger<WebSocketConnection> logger,
		IOptions<WebSocketOptions> options,
		IServiceScopeFactory scopeFactory,
		ICommandHandlerService commandHandler)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

		// Auto-start the connection
		_ = Task.Run(async () =>
		{
			try
			{
				await StartAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to auto-start WebSocket connection");
			}
		});
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
			SetupEvents();

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

	#region Event Handlers
	/// <summary>
	/// Sets up SignalR event handlers
	/// </summary>
	private void SetupEventHandlers()
	{

		if (_connection == null) return;

		// Handle GetLogs command from server
		LogsRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;
				var level = parameters.Level;
				var limit = parameters.Limit ?? 100;

				// Extract new pagination parameters if they exist
				var offset = 0;
				DateTime? fromDate = null;
				DateTime? toDate = null;
				string? searchTerm = null;

				// Check if the request has additional properties (future compatibility)
				if (parameters is IDictionary<string, object> requestDict)
				{
					if (requestDict.TryGetValue("Offset", out var offsetValue) && offsetValue is int offsetInt)
						offset = offsetInt;

					if (requestDict.TryGetValue("FromDate", out var fromDateValue) && fromDateValue is DateTime fromDateTime)
						fromDate = fromDateTime;

					if (requestDict.TryGetValue("ToDate", out var toDateValue) && toDateValue is DateTime toDateTime)
						toDate = toDateTime;

					if (requestDict.TryGetValue("SearchTerm", out var searchTermValue) && searchTermValue is string searchString)
						searchTerm = searchString;
				}

				_logger.LogInformation("Received GetLogs command from server - Level: {Level}, Limit: {Limit}, Offset: {Offset}, SearchTerm: {SearchTerm}, RequestId: {RequestId}",
					level, limit, offset, searchTerm ?? "none", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var logsService = scope.ServiceProvider.GetRequiredService<ILogsProviderService>();

				try
				{
					var logsResponse = await logsService.GetLogsAsync(level, limit, offset, fromDate, toDate, searchTerm);

					var response = new LogsResponse
					{
						RequestId = requestId,
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

					// Add pagination info if the response model supports it
					try
					{
						var responseType = response.GetType();
						var hasMoreProperty = responseType.GetProperty("HasMore");
						var offsetProperty = responseType.GetProperty("Offset");

						if (hasMoreProperty != null && hasMoreProperty.CanWrite)
							hasMoreProperty.SetValue(response, logsResponse.HasMore);

						if (offsetProperty != null && offsetProperty.CanWrite)
							offsetProperty.SetValue(response, logsResponse.Offset);
					}
					catch (Exception)
					{
						// Ignore reflection errors for backward compatibility
						_logger.LogDebug("Response model doesn't support pagination properties, continuing with basic response");
					}

					await SendLogsResponseAsync(response, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving logs");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "GetLogs",
						ErrorMessage = $"Error retrieving logs: {ex.Message}"
					};
					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetLogs command from server");
			}
		};

		// Handle GetSessions command from server
		SessionsRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;
				var instanceId = parameters.InstanceId;

				_logger.LogInformation("Received GetSessions command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();
				var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

				try
				{
					var sessions = await messageManager.GetChatSessionsAsync(instanceId);

					var response = new SessionsResponse
					{
						RequestId = requestId,
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

					await SendSessionsResponseAsync(response, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving sessions");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "GetSessions",
						ErrorMessage = $"Error retrieving sessions: {ex.Message}"
					};

					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetSessions command from server");
				var errorResponse = new ErrorResponse
				{
					RequestId = "", // requestId might not be available in this catch block
					CommandName = "GetSessions",
					ErrorMessage = $"Error handling GetSessions command: {ex.Message}"
				};

				await SendErrorResponseAsync(errorResponse, CancellationToken.None);
			}
		};

		// Handle GetConfig command from server
		ConfigRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;

				_logger.LogInformation("Received GetConfig command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var configService = scope.ServiceProvider.GetRequiredService<IConfigProviderService>();

				try
				{
					var configResponse = await configService.GetConfigAsync();
					configResponse.RequestId = requestId;

					await SendConfigResponseAsync(configResponse, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving configuration");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "GetConfig",
						ErrorMessage = $"Error retrieving configuration: {ex.Message}"
					};
					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetConfig command from server");
			}
		};

		// Handle UpdateConfig command from server
		ConfigUpdateRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;
				var configJson = JsonSerializer.Serialize(parameters.ConfigData); // Convert object to JSON string

				_logger.LogInformation("Received UpdateConfig command from server with config: {Config}, RequestId: {RequestId}", configJson, requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var configService = scope.ServiceProvider.GetRequiredService<IConfigProviderService>();

				try
				{
					var updateResponse = await configService.UpdateConfigAsync(configJson);
					updateResponse.RequestId = requestId;

					await SendConfigUpdateResponseAsync(updateResponse, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating configuration");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "UpdateConfig",
						ErrorMessage = $"Error updating configuration: {ex.Message}"
					};
					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling UpdateConfig command from server");
			}
		};

		// Handle GetCustomThemes command from server
		CustomThemesRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;

				_logger.LogInformation("Received GetCustomThemes command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

				try
				{
					var themesResponse = await themeService.GetCustomThemesAsync();
					themesResponse.RequestId = requestId;

					await SendThemesResponseAsync(themesResponse, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving custom themes");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "GetCustomThemes",
						ErrorMessage = $"Error retrieving custom themes: {ex.Message}"
					};
					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCustomThemes command from server");
			}
		};

		// Handle GetCommandsMetadata command from server
		CommandsMetadataRequested += async (parameters) =>
		{
			try
			{
				var requestId = parameters.RequestId;

				_logger.LogInformation("Received GetCommandsMetadata command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();

				try
				{
					var helpService = scope.ServiceProvider.GetRequiredService<IHelpService>();
					var coreCommandMeta = helpService.CommandMeta;

					// Convert Core CommandMetadata to App CommandMetadata
					List<Jiro.Shared.Websocket.Requests.CommandMetadata> appCommandMeta = coreCommandMeta.Select(c => new Jiro.Shared.Websocket.Requests.CommandMetadata
					{
						CommandName = c.CommandName,
						CommandDescription = c.CommandDescription,
						CommandSyntax = c.CommandSyntax,
						Parameters = c.Parameters,
						ModuleName = c.ModuleName,
						Keywords = c.Keywords
					}).ToList();

					var response = new CommandsMetadataResponse
					{
						RequestId = requestId,
						Commands = appCommandMeta
					};

					// Send response back to server via SignalR
					await SendCommandsMetadataResponseAsync(response, CancellationToken.None);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error retrieving commands metadata");
					var errorResponse = new ErrorResponse
					{
						RequestId = requestId,
						CommandName = "GetCommandsMetadata",
						ErrorMessage = $"Error retrieving commands metadata: {ex.Message}"
					};

					await SendErrorResponseAsync(errorResponse, CancellationToken.None);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCommandsMetadata command from server");
			}
		};

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

		KeepaliveAckReceived += async () =>
		{
			_logger.LogDebug("Keepalive acknowledgment received from server");

			if (KeepaliveAckReceived != null)
			{
				await KeepaliveAckReceived.Invoke();
			}
		};

		SessionRequested += async (req) =>
		{
			try
			{
				_logger.LogInformation("Received GetSession command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

				var session = await messageManager.GetSessionAsync(req.InstanceId, includeMessages: true);
				if (session is null)
				{
					_logger.LogError("Session not found for instance {InstanceId}", req.InstanceId);
					throw new Exception("Session not found.");
				}

				var sessionResponse = new SessionResponse
				{
					InstanceId = req.InstanceId,
					CreatedAt = session.CreatedAt,
					SessionId = session.SessionId,
					LastUpdatedAt = session.LastUpdatedAt,
					SessionName = string.Empty, // TODO: Implement session name
					RequestId = req.RequestId,
					TotalMessages = session.Messages.Count,
					Messages = session.Messages.Select(m => new ChatMessage
					{
						MessageId = m.MessageId,
						Content = m.Message.Content.ToString() ?? string.Empty,
						CreatedAt = m.CreatedAt,
						Type = m.Type.ToString(),
						IsUser = m.IsUser
					}).ToList()
				};

				await SendSessionResponseAsync(sessionResponse, CancellationToken.None);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetSession command from server");
			}
		};
	}

	#endregion
	#region Response Methods

	/// <summary>
	/// Sends a logs response to the server
	/// </summary>
	/// <param name="response">The logs response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendLogsResponseAsync(LogsResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.LogsResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a session response to the server
	/// </summary>
	/// <param name="response">The session response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendSessionResponseAsync(SessionResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.SessionResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a sessions response to the server
	/// </summary>
	/// <param name="response">The sessions response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendSessionsResponseAsync(SessionsResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.SessionsResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a configuration response to the server
	/// </summary>
	/// <param name="response">The configuration response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendConfigResponseAsync(ConfigResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.ConfigResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a config update response to the server
	/// </summary>
	/// <param name="response">The config update response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendConfigUpdateResponseAsync(ConfigUpdateResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.ConfigUpdateResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a themes response to the server
	/// </summary>
	/// <param name="response">The themes response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendThemesResponseAsync(ThemesResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.ThemesResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a commands metadata response to the server
	/// </summary>
	/// <param name="response">The commands metadata response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendCommandsMetadataResponseAsync(CommandsMetadataResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.CommandsMetadataResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a keepalive response to the server
	/// </summary>
	/// <param name="response">The keepalive response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendKeepaliveResponseAsync(KeepaliveResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.KeepaliveResponse, response, cancellationToken);
		}
	}

	/// <summary>
	/// Sends an error response to the server
	/// </summary>
	/// <param name="response">The error response</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the operation</returns>
	public async Task SendErrorResponseAsync(ErrorResponse response, CancellationToken cancellationToken = default)
	{
		if (_connection?.State == HubConnectionState.Connected)
		{
			await _connection.InvokeAsync(ServerHandled.ErrorResponse, response, cancellationToken);
		}
	}

	#endregion

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

	/// <inheritdoc/>
	public void SetupEvents()
	{
		if (_connection == null) return;

		// List of required event handlers and their names for better diagnostics
		var requiredHandlers = new (Delegate? handler, string name)[]
		{
			(CommandReceived, nameof(CommandReceived)),
			(KeepaliveAckReceived, nameof(KeepaliveAckReceived)),
			(LogsRequested, nameof(LogsRequested)), // Now supports pagination to handle large responses
			(SessionRequested, nameof(SessionRequested)), // TODO: fix the request parameters, support streaming
			(SessionsRequested, nameof(SessionsRequested)),
			(ConfigRequested, nameof(ConfigRequested)),
			(ConfigUpdateRequested, nameof(ConfigUpdateRequested)),
			(CustomThemesRequested, nameof(CustomThemesRequested)),
			(CommandsMetadataRequested, nameof(CommandsMetadataRequested)) // TODO: Fix the parameters dictionary
		};

		var missingHandlers = requiredHandlers.Where(h => h.handler is null).Select(h => h.name).ToList();
		if (missingHandlers.Count > 0)
		{
			_logger.LogWarning($"The following event handlers are not set up: {string.Join(", ", missingHandlers)}. Ensure all events are subscribed before calling SetupEvents.");
			return;
		}

		_connection.On(Endpoints.ClientHandled.ReceiveCommand, CommandReceived!);
		_connection.On(Endpoints.ClientHandled.KeepaliveAck, KeepaliveAckReceived!);
		_connection.On(Endpoints.ClientHandled.GetLogs, LogsRequested!);
		_connection.On(Endpoints.ClientHandled.GetSession, SessionRequested!);
		_connection.On(Endpoints.ClientHandled.GetSessions, SessionsRequested!);
		_connection.On(Endpoints.ClientHandled.GetConfig, ConfigRequested!);
		_connection.On(Endpoints.ClientHandled.UpdateConfig, ConfigUpdateRequested!);
		_connection.On(Endpoints.ClientHandled.GetCustomThemes, CustomThemesRequested!);
		_connection.On(Endpoints.ClientHandled.GetCommandsMetadata, CommandsMetadataRequested!);
	}
}
