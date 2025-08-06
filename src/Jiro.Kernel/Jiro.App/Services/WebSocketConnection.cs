using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

using Jiro.Core.Options;
using Jiro.Core.Services.CommandHandler;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.System;
using Jiro.Shared.Utilities;
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
public class WebSocketConnection : JiroInstanceBase, IDisposable
{
	private readonly ILogger<WebSocketConnection> _webSocketLogger;
	private readonly JiroCloudOptions _jiroCloudOptions;
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
	/// <param name="jiroCloudOptions">The JiroCloud configuration options</param>
	/// <param name="scopeFactory">Service scope factory for creating scoped services</param>
	/// <param name="commandHandler">Command handler service</param>
	/// <param name="exceptionHandler">Exception handler for WebSocket errors</param>
	public WebSocketConnection(
		HubConnection connection,
		ILogger<WebSocketConnection> logger,
		IOptions<JiroCloudOptions> jiroCloudOptions,
		IServiceScopeFactory scopeFactory,
		ICommandHandlerService commandHandler,
		WebSocketExceptionHandler exceptionHandler) : base(connection, logger as ILogger<JiroInstanceBase>)
	{
		_webSocketLogger = logger ?? throw new ArgumentNullException(nameof(logger));
		_jiroCloudOptions = jiroCloudOptions?.Value ?? throw new ArgumentNullException(nameof(jiroCloudOptions));
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

		// Don't start connection in constructor - wait for explicit initialization
		_webSocketLogger.LogWarning("🔥 CONSTRUCTOR: WebSocketConnection created, NOT starting connection yet");
	}


	/// <summary>
	/// Starts the WebSocket connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		_webSocketLogger.LogWarning("🔥 StartAsync: Initializing connection (handlers will be setup by base class)");

		await InitializeAsync(_jiroCloudOptions.WebSocket.HubUrl, _jiroCloudOptions.ApiKey,
			(ex, context) => _exceptionHandler.HandleConnectionException(ex, context), cancellationToken);

		_webSocketLogger.LogWarning("🔥 StartAsync: Connection initialized successfully");
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

		// Setup stream handlers - now handled by base class with proper streaming to server
		_webSocketLogger.LogWarning("🔥 CLIENT: Setting up stream handlers");
		SessionMessagesStreamRequested += HandleSessionMessagesStreamAsync;
		LogsStreamRequested += HandleLogsStreamAsync;
		_webSocketLogger.LogWarning("🔥 CLIENT: Stream handlers setup complete");

		// Setup new v1.3.0 event handlers
		RemoveSessionRequested += HandleRemoveSessionAsync;
		UpdateSessionRequested += HandleUpdateSessionAsync;
		MachineInfoRequested += HandleMachineInfoAsync;

		Reconnected += (input) =>
		{
			_logger?.LogInformation("WebSocket connection re-established");
			return Task.CompletedTask;
		};

		Reconnecting += (input) =>
		{
			_logger?.LogWarning("WebSocket connection is reconnecting");
			return Task.CompletedTask;
		};

		Closed += async (input) =>
		{
			_logger?.LogWarning("WebSocket connection closed: {Message}", input);
			await DisposeConnectionAsync();
		};

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
					Configuration = new Shared.Websocket.Requests.ConfigurationSection { Values = new Dictionary<string, string>() },
					SystemInfo = new Shared.Websocket.Requests.SystemInfo
					{
						OperatingSystem = "Error",
						RuntimeVersion = "Error",
						MachineName = "Error",
						ProcessorCount = 0,
						TotalMemory = 0
					},
					UptimeSeconds = 0
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
					Configuration = new Shared.Websocket.Requests.ConfigurationSection { Values = new Dictionary<string, string>() },
					SystemInfo = new Shared.Websocket.Requests.SystemInfo
					{
						OperatingSystem = "Error",
						RuntimeVersion = "Error",
						MachineName = "Error",
						ProcessorCount = 0,
						TotalMemory = 0
					},
					UptimeSeconds = 0
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
					Parameters = c.Parameters.Select(static p => new KeyValuePair<string, string>(p.Key, p.Value.ToString())).ToDictionary(),
					ModuleName = c.ModuleName,
					Keywords = c.Keywords
				}).ToList();

				var response = new CommandsMetadataResponse
				{
					RequestId = requestId,
					Commands = appCommandMeta
				};

				_logger?.LogInformation("Returning {CommandCount} command metadata items", appCommandMeta.Count);

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

	#region New v1.3.0 Event Handlers

	/// <summary>
	/// Handles remove session request from server
	/// </summary>
	/// <param name="request">The remove session request</param>
	/// <returns>Action result indicating success or failure</returns>
	private async Task<ActionResult> HandleRemoveSessionAsync(RemoveSessionRequest request)
	{
		try
		{
			_webSocketLogger.LogInformation("Received RemoveSession command from server for SessionId: {SessionId}", request.SessionId);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

			var success = await messageManager.RemoveSessionAsync(request.SessionId);
			if (!success)
			{
				return new ActionResult
				{
					IsSuccess = false,
					Message = "Session not found",
					Errors = new[] { $"Session with ID {request.SessionId} does not exist" }
				};
			}

			return new ActionResult
			{
				IsSuccess = true,
				Message = $"Session {request.SessionId} removed successfully"
			};
		}
		catch (Exception ex)
		{
			_webSocketLogger.LogError(ex, "Error handling RemoveSession command for SessionId: {SessionId}", request.SessionId);
			return new ActionResult
			{
				IsSuccess = false,
				Message = "Failed to remove session",
				Errors = new[] { ex.Message }
			};
		}
	}

	/// <summary>
	/// Handles update session request from server
	/// </summary>
	/// <param name="request">The update session request</param>
	/// <returns>Action result indicating success or failure</returns>
	private async Task<ActionResult> HandleUpdateSessionAsync(UpdateSessionRequest request)
	{
		try
		{
			_webSocketLogger.LogInformation("Received UpdateSession command from server for SessionId: {SessionId}, Name: {Name}",
				request.SessionId, request.Name ?? "unchanged");

			await using var scope = _scopeFactory.CreateAsyncScope();
			var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

			var success = await messageManager.UpdateSessionAsync(request.SessionId, request.Name, request.Description);
			if (!success)
			{
				return new ActionResult
				{
					IsSuccess = false,
					Message = "Session not found",
					Errors = new[] { $"Session with ID {request.SessionId} does not exist" }
				};
			}

			return new ActionResult
			{
				IsSuccess = true,
				Message = $"Session {request.SessionId} updated successfully"
			};
		}
		catch (Exception ex)
		{
			_webSocketLogger.LogError(ex, "Error handling UpdateSession command for SessionId: {SessionId}", request.SessionId);
			return new ActionResult
			{
				IsSuccess = false,
				Message = "Failed to update session",
				Errors = new[] { ex.Message }
			};
		}
	}

	/// <summary>
	/// Handles machine info request from server
	/// </summary>
	/// <param name="request">The machine info request</param>
	/// <returns>Machine information response</returns>
	private async Task<MachineInfoResponse> HandleMachineInfoAsync(MachineInfoRequest request)
	{
		try
		{
			_webSocketLogger.LogInformation("Received MachineInfo command from server with RequestId: {RequestId}", request.RequestId);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

			// Get real-time performance data using PerformanceAnalyzer
			var performanceAnalyzer = PerformanceAnalyzerFactory.Create();

			var metadata = new Dictionary<string, string>
			{
				["OS"] = Environment.OSVersion.ToString(),
				["Platform"] = Environment.OSVersion.Platform.ToString(),
				["ProcessorCount"] = Environment.ProcessorCount.ToString(),
				["RuntimeVersion"] = Environment.Version.ToString(),
				["WorkingSet"] = Environment.WorkingSet.ToString(),
				["UserName"] = Environment.UserName,
				["UserDomainName"] = Environment.UserDomainName,
				["Is64BitOS"] = Environment.Is64BitOperatingSystem.ToString(),
				["Is64BitProcess"] = Environment.Is64BitProcess.ToString()
			};

			// Add real-time performance metrics
			try
			{
				var cpuUsage = await performanceAnalyzer.GetCpuUsageAsync();
				var availableMemoryMB = await performanceAnalyzer.GetAvailableMemoryMBAsync();
				var usedMemoryMB = await performanceAnalyzer.GetApplicationMemoryUsedMBAsync();
				var memoryUsagePercentage = await performanceAnalyzer.GetApplicationMemoryUsagePercentageAsync();

				metadata.Add("CpuUsagePercent", cpuUsage.ToString("F2"));
				metadata.Add("AvailableMemoryMB", availableMemoryMB.ToString("F2"));
				metadata.Add("UsedMemoryMB", usedMemoryMB.ToString("F2"));
				metadata.Add("MemoryUsagePercent", memoryUsagePercentage.ToString("F2"));
				metadata.Add("TotalMemoryMB", (availableMemoryMB + usedMemoryMB).ToString("F2"));
			}
			catch (Exception perfEx)
			{
				_webSocketLogger.LogWarning(perfEx, "Failed to get performance metrics for machine info");
				metadata.Add("PerformanceMetricsError", perfEx.Message);
			}

			var machineInfo = new MachineInfoResponse
			{
				RequestId = request.RequestId,
				MachineId = Environment.MachineName,
				Name = Environment.MachineName,
				Description = $"Jiro instance running on {Environment.OSVersion}",
				Status = "Active",
				Metadata = metadata
			};

			return machineInfo;
		}
		catch (Exception ex)
		{
			_webSocketLogger.LogError(ex, "Error handling MachineInfo command from server");
			return new MachineInfoResponse
			{
				RequestId = request.RequestId,
				MachineId = "Error",
				Name = "Error",
				Description = "Failed to retrieve machine information",
				Status = "Error",
				Metadata = new Dictionary<string, string>
				{
					["Error"] = ex.Message
				}
			};
		}
	}

	#endregion

	#endregion

	#region Stream Methods

	/// <summary>
	/// Handles session messages stream request by sending data to server
	/// </summary>
	/// <param name="request">The session request</param>
	private Task<ActionResult> HandleSessionMessagesStreamAsync(GetSingleSessionRequest request)
	{
		try
		{
			_webSocketLogger.LogInformation("Starting HandleSessionMessagesStreamAsync for SessionId: {SessionId}, RequestId: {RequestId}",
				request.SessionId, request.RequestId);

			// Create channel first (SignalR best practice)
			var channel = Channel.CreateUnbounded<ChatMessage>();
			var writer = channel.Writer;
			var reader = channel.Reader;

			// Start background task to populate channel
			_ = Task.Run(async () =>
			{
				try
				{
					await foreach (var message in GetSessionMessagesStreamAsync(request))
					{
						await writer.WriteAsync(message);
					}
				}
				catch (Exception ex)
				{
					_webSocketLogger.LogError(ex, "Error populating session messages channel for RequestId: {RequestId}", request.RequestId);
				}
				finally
				{
					writer.Complete();
				}
			});

			// Send stream to SignalR hub (fire and forget)
			_ = Task.Run(async () =>
			{
				try
				{
					await ReceiveSessionMessagesStreamAsync(request.RequestId, reader);
					_webSocketLogger.LogInformation("Successfully sent session messages stream for RequestId: {RequestId}", request.RequestId);
				}
				catch (Exception ex)
				{
					_webSocketLogger.LogError(ex, "Failed to send session messages stream for RequestId: {RequestId}", request.RequestId);
				}
			});

			_webSocketLogger.LogInformation("Completed HandleSessionMessagesStreamAsync for RequestId: {RequestId}", request.RequestId);
			return Task.FromResult(new ActionResult { IsSuccess = true, Message = "Session messages stream initiated successfully" });
		}
		catch (Exception ex)
		{
			_webSocketLogger.LogError(ex, "Error in HandleSessionMessagesStreamAsync for RequestId: {RequestId}", request.RequestId);
			return Task.FromResult(new ActionResult { IsSuccess = false, Message = "Failed to initiate session messages stream", Errors = new[] { ex.Message } });
		}
	}

	/// <summary>
	/// Gets session messages as a stream
	/// </summary>
	/// <param name="request">The session request</param>
	/// <returns>An async enumerable of chat messages</returns>
	private async IAsyncEnumerable<ChatMessage> GetSessionMessagesStreamAsync(GetSingleSessionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		_webSocketLogger.LogInformation("Starting SessionMessagesStream for SessionId: {SessionId}, InstanceId: {InstanceId}, RequestId: {RequestId}",
			request.SessionId, request.InstanceId, request.RequestId);

		IServiceScope? scope = null;
		var messageCount = 0;
		try
		{
			scope = _scopeFactory.CreateScope();
			var messageManager = scope.ServiceProvider.GetRequiredService<IMessageManager>();

			var session = await messageManager.GetSessionAsync(request.SessionId, includeMessages: true);
			if (session?.Messages != null)
			{
				foreach (var message in session.Messages)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						_webSocketLogger.LogInformation("SessionMessagesStream cancelled after {MessageCount} messages for RequestId: {RequestId}",
							messageCount, request.RequestId);
						yield break;
					}

					yield return new ChatMessage
					{
						MessageId = message.MessageId,
						Content = message.Message.Content.FirstOrDefault()?.Text ?? string.Empty,
						CreatedAt = message.CreatedAt,
						Type = message.Type.ToString(),
						IsUser = message.IsUser
					};

					messageCount++;

					// Add a small delay every 5 messages to allow for smooth streaming
					if (messageCount % 5 == 0)
					{
						await Task.Delay(1, cancellationToken);
					}
				}
			}

			_webSocketLogger.LogInformation("Completed SessionMessagesStream with {MessageCount} messages for RequestId: {RequestId}",
				messageCount, request.RequestId);
		}
		finally
		{
			scope?.Dispose();
		}
	}

	/// <summary>
	/// Handles logs stream request by sending data to server
	/// </summary>
	/// <param name="request">The logs request</param>
	private Task<ActionResult> HandleLogsStreamAsync(GetLogsRequest request)
	{
		try
		{
			_webSocketLogger.LogWarning("🔥 CLIENT: HandleLogsStreamAsync CALLED! Level: {Level}, RequestId: {RequestId}",
				request.Level ?? "all", request.RequestId);

			// Create channel first (SignalR best practice)
			var channel = Channel.CreateUnbounded<LogEntry>();
			var writer = channel.Writer;
			var reader = channel.Reader;

			// Start background task to populate channel
			_ = Task.Run(async () =>
			{
				try
				{
					await foreach (var logEntry in GetLogsStreamForSignalRAsync(request))
					{
						await writer.WriteAsync(logEntry);
					}
				}
				catch (Exception ex)
				{
					_webSocketLogger.LogError(ex, "Error populating logs channel for RequestId: {RequestId}", request.RequestId);
				}
				finally
				{
					writer.Complete();
				}
			});

			// Send stream to SignalR hub (fire and forget)
			_ = Task.Run(async () =>
			{
				try
				{
					await ReceiveLogsStreamAsync(request.RequestId, reader);
					_webSocketLogger.LogInformation("Successfully sent logs stream for RequestId: {RequestId}", request.RequestId);
				}
				catch (Exception ex)
				{
					_webSocketLogger.LogError(ex, "Failed to send logs stream for RequestId: {RequestId}", request.RequestId);
				}
			});

			_webSocketLogger.LogInformation("Completed HandleLogsStreamAsync for RequestId: {RequestId}", request.RequestId);
			return Task.FromResult(new ActionResult { IsSuccess = true, Message = "Logs stream initiated successfully" });
		}
		catch (Exception ex)
		{
			_webSocketLogger.LogError(ex, "Error in HandleLogsStreamAsync for RequestId: {RequestId}", request.RequestId);
			return Task.FromResult(new ActionResult { IsSuccess = false, Message = "Failed to initiate logs stream", Errors = new[] { ex.Message } });
		}
	}

	/// <summary>
	/// Gets logs stream optimized for SignalR using StreamLimitedLogsAsync
	/// </summary>
	/// <param name="request">The logs request</param>
	/// <returns>An async enumerable of log entries</returns>
	private async IAsyncEnumerable<LogEntry> GetLogsStreamForSignalRAsync(GetLogsRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		_webSocketLogger.LogInformation("Starting GetLogsStreamForSignalRAsync for Level: {Level}, Limit: {Limit}, Offset: {Offset}, RequestId: {RequestId}",
			request.Level ?? "all", request.Limit ?? 100, request.Offset ?? 0, request.RequestId);

		IServiceScope? scope = null;
		var entryCount = 0;
		try
		{
			scope = _scopeFactory.CreateScope();
			var logsService = scope.ServiceProvider.GetRequiredService<ILogsProviderService>();

			// Use StreamLimitedLogsAsync for better performance and automatic stopping
			await foreach (var log in logsService.StreamLimitedLogsAsync(
				level: request.Level,
				limit: request.Limit ?? 100,
				offset: request.Offset ?? 0,
				fromDate: null,
				toDate: null,
				searchTerm: null,
				cancellationToken: cancellationToken))
			{
				if (cancellationToken.IsCancellationRequested)
				{
					_webSocketLogger.LogInformation("GetLogsStreamForSignalRAsync cancelled after {EntryCount} entries for RequestId: {RequestId}",
						entryCount, request.RequestId);
					yield break;
				}

				yield return new LogEntry
				{
					File = log.File,
					Timestamp = log.Timestamp,
					Level = log.Level,
					Message = log.Message
				};

				entryCount++;

				// Log progress every 50 entries
				if (entryCount % 50 == 0)
				{
					_webSocketLogger.LogDebug("Streamed {EntryCount} log entries for RequestId: {RequestId}",
						entryCount, request.RequestId);
				}
			}

			_webSocketLogger.LogInformation("Completed GetLogsStreamForSignalRAsync with {EntryCount} entries for RequestId: {RequestId}",
				entryCount, request.RequestId);
		}
		finally
		{
			scope?.Dispose();
		}
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
