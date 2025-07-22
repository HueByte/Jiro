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

namespace Jiro.App.Services;

/// <summary>
/// SignalR implementation of the WebSocket connection interface
/// </summary>
/// TODO: Use JiroClientBase when fixed
partial class WebSocketConnection : IJiroClient
{
	// Events for IJiroClient interface
	public event Func<Exception?, Task>? Closed;
	public event Func<Exception?, Task>? Reconnecting;
	public event Func<string?, Task>? Reconnected;
	public event Func<CommandMessage, Task>? CommandReceived;
	public event Func<Task>? KeepaliveAckReceived;
	public event Func<GetLogsRequest, Task<LogsResponse>>? LogsRequested;
	public event Func<GetSessionRequest, Task<SessionResponse>>? SessionRequested;
	public event Func<GetSessionsRequest, Task<SessionsResponse>>? SessionsRequested;
	public event Func<GetConfigRequest, Task<ConfigResponse>>? ConfigRequested;
	public event Func<GetCustomThemesRequest, Task<ThemesResponse>>? CustomThemesRequested;
	public event Func<GetCommandsMetadataRequest, Task<CommandsMetadataResponse>>? CommandsMetadataRequested;
	public event Func<UpdateConfigRequest, Task<ConfigResponse>>? ConfigUpdateRequested;

	// Other properties and methods...
}
public partial class WebSocketConnection : IDisposable
{
	private readonly ILogger<WebSocketConnection> _logger;
	private readonly WebSocketOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ICommandHandlerService _commandHandler;
	private readonly WebSocketExceptionHandler _exceptionHandler;
	private HubConnection? _hubConnection;
	private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
	private bool _disposed = false;

	/// <summary>
	/// Gets the current connection state
	/// </summary>
	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	/// <summary>
	/// Initializes a new instance of the SignalRWebSocketConnection
	/// </summary>
	/// <param name="connection">The SignalR HubConnection</param>
	/// <param name="logger">The logger</param>
	/// <param name="options">The WebSocket configuration options</param>
	/// <param name="scopeFactory">Service scope factory for creating scoped services</param>
	/// <param name="commandHandler">Command handler service</param>
	/// <param name="exceptionHandler">Exception handler for WebSocket errors</param>
	public WebSocketConnection(
		HubConnection connection,
		ILogger<WebSocketConnection> logger,
		IOptions<WebSocketOptions> options,
		IServiceScopeFactory scopeFactory,
		ICommandHandlerService commandHandler,
		WebSocketExceptionHandler exceptionHandler) /*: base(connection, logger)*/
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
		_hubConnection = connection ?? throw new ArgumentNullException(nameof(connection));

		_ = InitializeAsync();
	}

	/// <summary>
	/// Starts the WebSocket connection
	/// </summary>
	/// <returns>Task representing the async operation</returns>
	private async Task InitializeAsync()
	{
		CancellationToken cancellationToken = new CancellationTokenSource().Token;

		_logger.LogInformation("Initializing WebSocket connection to {Url}", _options.HubUrl);
		if (_hubConnection == null)
		{
			throw new InvalidOperationException("HubConnection is not initialized. Ensure the connection is properly configured.");
		}

		await _connectionSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (_hubConnection?.State == HubConnectionState.Connected)
			{
				_logger.LogDebug("Already connected to hub");
				return;
			}

			if (_hubConnection is null)
			{
				throw new InvalidOperationException("HubConnection is not initialized. Ensure the connection is properly configured.");
			}

			if (_hubConnection.State == HubConnectionState.Connected)
			{
				await _hubConnection.StopAsync(cancellationToken);
			}

			_logger.LogInformation("Connecting to hub at {Url}", _options.HubUrl);

			// Ensure API key is provided for authentication
			if (string.IsNullOrEmpty(_options.ApiKey))
			{
				throw new InvalidOperationException("API key is required for WebSocket authentication. Please configure 'WebSocket:ApiKey' or 'API_KEY' in your settings.");
			}

			// Connect
			await _hubConnection.StartAsync(cancellationToken);

			SetupEvents();
			SetupEventHandlers();

			_logger.LogInformation("Successfully connected to hub");
		}
		catch (Exception ex)
		{
			_exceptionHandler.HandleConnectionException(ex, "Connect");
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
			if (_hubConnection != null)
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
		_logger.LogDebug("Setting up event handlers");
		if (_hubConnection == null) return;

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

				return response;
			}
			catch (Exception ex)
			{
				_exceptionHandler.HandleEventException(ex, "GetLogs");
				var errorResponse = _exceptionHandler.HandleException(ex, parameters.RequestId, "GetLogs", $"Level: {parameters.Level}, Limit: {parameters.Limit}");
				return new LogsResponse
				{
					RequestId = parameters.RequestId,
					TotalLogs = 0,
					Level = parameters.Level,
					Limit = parameters.Limit ?? 100,
					Logs = new List<LogEntry>()
				};
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

				return response;
			}
			catch (Exception ex)
			{
				_exceptionHandler.HandleEventException(ex, "GetSessions");
				return new SessionsResponse
				{
					RequestId = parameters.RequestId,
					InstanceId = parameters.InstanceId,
					TotalSessions = 0,
					CurrentSessionId = null,
					Sessions = new List<ChatSession>()
				};
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

				var configResponse = await configService.GetConfigAsync();
				configResponse.RequestId = requestId;

				return configResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetConfig command from server");
				return new ConfigResponse
				{
					RequestId = parameters.RequestId,
					ApplicationName = "Jiro",
					Version = "Error",
					Environment = "Error",
					InstanceId = "Error",
					Configuration = new Shared.Websocket.Requests.ConfigurationSection { Values = new Dictionary<string, object>() },
					SystemInfo = new Shared.Websocket.Requests.SystemInfo
					{
						OperatingSystem = "Error",
						RuntimeVersion = "Error",
						MachineName = "Error",
						ProcessorCount = 0,
						TotalMemory = 0
					},
					Uptime = TimeSpan.Zero
				};
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

				var updateResponse = await configService.UpdateConfigAsync(configJson);

				// Convert ConfigUpdateResponse to ConfigResponse and get current config
				var configResponse = await configService.GetConfigAsync();
				configResponse.RequestId = requestId;

				return configResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling UpdateConfig command from server");
				return new ConfigResponse
				{
					RequestId = parameters.RequestId,
					ApplicationName = "Jiro",
					Version = "Error",
					Environment = "Error",
					InstanceId = "Error",
					Configuration = new Shared.Websocket.Requests.ConfigurationSection { Values = new Dictionary<string, object>() },
					SystemInfo = new Shared.Websocket.Requests.SystemInfo
					{
						OperatingSystem = "Error",
						RuntimeVersion = "Error",
						MachineName = "Error",
						ProcessorCount = 0,
						TotalMemory = 0
					},
					Uptime = TimeSpan.Zero
				};
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

				var themesResponse = await themeService.GetCustomThemesAsync();
				themesResponse.RequestId = requestId;

				return themesResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCustomThemes command from server");
				return new ThemesResponse
				{
					RequestId = parameters.RequestId,
					Themes = new List<Shared.Websocket.Requests.Theme>()
				};
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

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetCommandsMetadata command from server");
				return new CommandsMetadataResponse
				{
					RequestId = parameters.RequestId,
					Commands = new List<Jiro.Shared.Websocket.Requests.CommandMetadata>()
				};
			}
		};

		KeepaliveAckReceived += async () =>
		{
			_logger.LogDebug("Keepalive acknowledgment received from server");
		};

		SessionRequested += async (req) =>
		{
			try
			{
				_logger.LogInformation("Received GetSession command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

				var session = await messageManager.GetSessionAsync(req.SessionId, includeMessages: true);
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
					SessionName = session.Name,
					RequestId = req.RequestId,
					TotalMessages = session.Messages.Count,
					Messages = session.Messages.Select(m => new ChatMessage
					{
						MessageId = m.MessageId,
						Content = m.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
						CreatedAt = m.CreatedAt,
						Type = m.Type.ToString(),
						IsUser = m.IsUser
					}).ToList()
				};

				return sessionResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error handling GetSession command from server");
				return new SessionResponse
				{
					InstanceId = req.InstanceId,
					SessionId = req.SessionId,
					RequestId = req.RequestId,
					SessionName = "Error",
					TotalMessages = 0,
					Messages = new List<ChatMessage>(),
					CreatedAt = DateTime.UtcNow,
					LastUpdatedAt = DateTime.UtcNow
				};
			}
		};
	}

	#endregion
	#region SetupEvents
	/// <summary>
	/// Sets up the events for the WebSocket connection
	/// This method is called after the connection is established to ensure all events are ready to be invoked.
	/// It should be called after the connection is started to ensure the events are properly registered
	/// and can be invoked when the corresponding events occur.
	/// </summary>
	/// <exception cref="NotImplementedException"></exception>
	public void SetupEvents()
	{
		if (_hubConnection == null) return;

		// Fire-and-forget notifications
		_hubConnection.On<CommandMessage>(Events.CommandReceived, async command =>
		{
			_logger.LogInformation("{EventName} received", Events.CommandReceived);
			if (CommandReceived != null)
				await CommandReceived(command);
			_logger.LogInformation("{EventName} executed", Events.CommandReceived);
		});

		_hubConnection.On(Events.KeepaliveAckReceived, async () =>
		{
			_logger.LogInformation("{EventName} received", Events.KeepaliveAckReceived);
			if (KeepaliveAckReceived != null)
				await KeepaliveAckReceived();
			_logger.LogInformation("{EventName} executed", Events.KeepaliveAckReceived);
		});

		// RPC-style calls (server expects a return value)

		_hubConnection.On<GetLogsRequest, LogsResponse>(
			Events.LogsRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.LogsRequested, request.RequestId);
				var response = await LogsRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.LogsRequested, response.RequestId);
				return response;
			});

		_hubConnection.On<GetSessionsRequest, SessionsResponse>(
			Events.SessionsRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.SessionsRequested, request.RequestId);
				var response = await SessionsRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.SessionsRequested, response.RequestId);
				return response;
			});

		_hubConnection.On<GetSessionRequest, SessionResponse>(
			Events.SessionRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.SessionRequested, request.RequestId);
				var response = await SessionRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.SessionRequested, response.RequestId);
				return response;
			});

		_hubConnection.On<GetConfigRequest, ConfigResponse>(
			Events.ConfigRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.ConfigRequested, request.RequestId);
				var response = await ConfigRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.ConfigRequested, response.RequestId);
				return response;
			});

		_hubConnection.On<UpdateConfigRequest, ConfigResponse>(
			Events.ConfigUpdated,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.ConfigUpdated, request.RequestId);
				var response = await ConfigUpdateRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.ConfigUpdated, response.RequestId);
				return response;
			});

		_hubConnection.On<GetCustomThemesRequest, ThemesResponse>(
			Events.CustomThemesRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.CustomThemesRequested, request.RequestId);
				var response = await CustomThemesRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.CustomThemesRequested, response.RequestId);
				return response;
			});

		_hubConnection.On<GetCommandsMetadataRequest, CommandsMetadataResponse>(
			Events.CommandsMetadataRequested,
			async request =>
			{
				_logger.LogInformation("{EventName} received: {RequestId}", Events.CommandsMetadataRequested, request.RequestId);
				var response = await CommandsMetadataRequested!(request);
				_logger.LogInformation("{EventName} handled: {RequestId}", Events.CommandsMetadataRequested, response.RequestId);
				return response;
			});
	}
	#endregion

	/// <summary>
	/// Disposes the current connection
	/// </summary>
	/// <returns>A task representing the disposal operation</returns>
	private async Task DisposeConnectionAsync()
	{
		if (_hubConnection != null)
		{
			try
			{
				if (_hubConnection.State == HubConnectionState.Connected)
				{
					await _hubConnection.StopAsync();
				}
				await _hubConnection.DisposeAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error disposing SignalR connection");
			}
			finally
			{
				_hubConnection = null;
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
