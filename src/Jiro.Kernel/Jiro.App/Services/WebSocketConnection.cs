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
public partial class WebSocketConnection : JiroClientBase, IDisposable
{
	private readonly ILogger<WebSocketConnection> _webSocketLogger;
	private readonly WebSocketOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ICommandHandlerService _commandHandler;
	private readonly WebSocketExceptionHandler _exceptionHandler;
	private bool _disposed = false;

	/// <summary>
	/// Gets the current connection state
	/// </summary>
	public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

	/// <summary>
	/// Initializes a new instance of the WebSocketConnection
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
		WebSocketExceptionHandler exceptionHandler) : base(connection, logger as ILogger<JiroClientBase>)
	{
		_webSocketLogger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

		_ = InitializeAsync(_options.HubUrl, _options.ApiKey, 
			(ex, context) => _exceptionHandler.HandleConnectionException(ex, context));
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
	/// Sets up custom event handlers for business logic
	/// </summary>
	protected override void SetupHandlers()
	{
		_webSocketLogger.LogDebug("Setting up custom event handlers");

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

				_webSocketLogger.LogInformation("Received GetLogs command from server - Level: {Level}, Limit: {Limit}, Offset: {Offset}, SearchTerm: {SearchTerm}, RequestId: {RequestId}",
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
					_webSocketLogger.LogDebug("Response model doesn't support pagination properties, continuing with basic response");
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

				_webSocketLogger.LogInformation("Received GetSessions command from server with RequestId: {RequestId}", requestId);

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

				_webSocketLogger.LogInformation("Received GetConfig command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var configService = scope.ServiceProvider.GetRequiredService<IConfigProviderService>();

				var configResponse = await configService.GetConfigAsync();
				configResponse.RequestId = requestId;

				return configResponse;
			}
			catch (Exception ex)
			{
				_webSocketLogger.LogError(ex, "Error handling GetConfig command from server");
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

				_webSocketLogger.LogInformation("Received UpdateConfig command from server with config: {Config}, RequestId: {RequestId}", configJson, requestId);

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
				_webSocketLogger.LogError(ex, "Error handling UpdateConfig command from server");
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

				_webSocketLogger.LogInformation("Received GetCustomThemes command from server with RequestId: {RequestId}", requestId);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var themeService = scope.ServiceProvider.GetRequiredService<IThemeService>();

				var themesResponse = await themeService.GetCustomThemesAsync();
				themesResponse.RequestId = requestId;

				return themesResponse;
			}
			catch (Exception ex)
			{
				_webSocketLogger.LogError(ex, "Error handling GetCustomThemes command from server");
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

				_webSocketLogger.LogInformation("Received GetCommandsMetadata command from server with RequestId: {RequestId}", requestId);

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
				_webSocketLogger.LogError(ex, "Error handling GetCommandsMetadata command from server");
				return new CommandsMetadataResponse
				{
					RequestId = parameters.RequestId,
					Commands = new List<Jiro.Shared.Websocket.Requests.CommandMetadata>()
				};
			}
		};

		KeepaliveAckReceived += async () =>
		{
			_webSocketLogger.LogDebug("Keepalive acknowledgment received from server");
		};

		SessionRequested += async (req) =>
		{
			try
			{
				_webSocketLogger.LogInformation("Received GetSession command from server");

				await using var scope = _scopeFactory.CreateAsyncScope();
				var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

				var session = await messageManager.GetSessionAsync(req.SessionId, includeMessages: true);
				if (session is null)
				{
					_webSocketLogger.LogError("Session not found for instance {InstanceId}", req.InstanceId);
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
				_webSocketLogger.LogError(ex, "Error handling GetSession command from server");
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
				_webSocketLogger.LogError(ex, "Error disposing SignalR connection");
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
				_webSocketLogger.LogError(ex, "Error during disposal");
			}
			finally
			{
				_connectionSemaphore.Dispose();
				_disposed = true;
			}
		}
	}
}
