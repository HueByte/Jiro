using System.Collections.Concurrent;

using Jiro.Core.Options;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandHandler;
using Jiro.Shared.Websocket;
using Jiro.Shared.Websocket.Requests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedCommandMessage = Jiro.Shared.Websocket.Requests.CommandMessage;

namespace Jiro.App.Services;

/// <summary>
/// Hosted service that manages WebSocket communication using IJiroClientHub
/// </summary>
public class JiroWebSocketService : BackgroundService, ICommandQueueMonitor
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JiroWebSocketService> _logger;
	private readonly ICommandHandlerService _commandHandler;
	private readonly JiroCloudOptions _jiroCloudOptions;
	private readonly IJiroInstance _jiroClient;
	private readonly IJiroGrpcService _grpcService;

	// Command queue monitoring
	private readonly ConcurrentDictionary<string, DateTime> _activeCommands = new();
	private long _totalCommandsProcessed = 0;
	private long _successfulCommands = 0;
	private long _failedCommands = 0;

	/// <summary>
	/// Gets the current number of executing commands
	/// </summary>
	public int ActiveCommandCount => _activeCommands.Count;

	/// <summary>
	/// Gets the list of currently executing command IDs
	/// </summary>
	public IEnumerable<string> ActiveCommandIds => _activeCommands.Keys;

	/// <summary>
	/// Gets the total number of commands processed since startup
	/// </summary>
	public long TotalCommandsProcessed => _totalCommandsProcessed;

	/// <summary>
	/// Gets the number of commands that completed successfully
	/// </summary>
	public long SuccessfulCommands => _successfulCommands;

	/// <summary>
	/// Gets the number of commands that failed
	/// </summary>
	public long FailedCommands => _failedCommands;

	/// <summary>
	/// Initializes a new instance of the JiroWebSocketService
	/// </summary>
	/// <param name="scopeFactory">Service scope factory for creating scoped services</param>
	/// <param name="logger">Logger instance</param>
	/// <param name="commandHandler">Command handler service</param>
	/// <param name="options">WebSocket configuration options</param>
	/// <param name="jiroClientHub">WebSocket client hub interface</param>
	/// <param name="grpcService">gRPC service for sending command results</param>
	public JiroWebSocketService(
		IServiceScopeFactory scopeFactory,
		ILogger<JiroWebSocketService> logger,
		ICommandHandlerService commandHandler,
		IOptions<JiroCloudOptions> jiroCloudOptions,
		IJiroInstance jiroClientHub,
		IJiroGrpcService grpcService)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_jiroCloudOptions = jiroCloudOptions?.Value ?? throw new ArgumentNullException(nameof(jiroCloudOptions));
		_jiroClient = jiroClientHub ?? throw new ArgumentNullException(nameof(jiroClientHub));
		_grpcService = grpcService ?? throw new ArgumentNullException(nameof(grpcService));
	}

	/// <summary>
	/// Starts the WebSocket service and establishes connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Starting Jiro WebSocket Service");

		// Register event handlers for IJiroClientHub
		_jiroClient.CommandReceived += HandleCommandAsync;
		_jiroClient.Closed += OnConnectionClosed;
		_jiroClient.Reconnecting += OnConnectionReconnecting;
		_jiroClient.Reconnected += OnConnectionReconnected;

		// Start the WebSocket connection
		if (_jiroClient is WebSocketConnection wsConnection)
		{
			await wsConnection.StartAsync(cancellationToken);
		}

		_logger.LogInformation("Jiro WebSocket Service started successfully (IJiroClientHub)");
		await base.StartAsync(cancellationToken);
	}

	/// <summary>
	/// Stops the WebSocket service and closes connections
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Stopping Jiro WebSocket Service");

		// Unregister event handlers
		_jiroClient.CommandReceived -= HandleCommandAsync;
		_jiroClient.Closed -= OnConnectionClosed;
		_jiroClient.Reconnecting -= OnConnectionReconnecting;
		_jiroClient.Reconnected -= OnConnectionReconnected;

		// Stop the WebSocket connection
		if (_jiroClient is WebSocketConnection wsConnection)
		{
			await wsConnection.StopAsync(cancellationToken);
		}

		_logger.LogInformation("Jiro WebSocket Service stopped successfully (IJiroClientHub)");
		await base.StopAsync(cancellationToken);
	}

	/// <summary>
	/// Main execution loop for the background service
	/// </summary>
	/// <param name="stoppingToken">Cancellation token for stopping the service</param>
	/// <returns>Task representing the async operation</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Keep the service running
		try
		{
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (OperationCanceledException)
		{
			// Expected when cancellation is requested
		}
	}

	private async Task<ActionResult> HandleCommandAsync(SharedCommandMessage commandMessage)
	{
		var commandSyncId = commandMessage.CommandSyncId;
		_activeCommands.TryAdd(commandSyncId, DateTime.UtcNow);
		Interlocked.Increment(ref _totalCommandsProcessed);

		try
		{
			_logger.LogInformation("Processing command: {Command} [{SyncId}] with SessionId: '{SessionId}' (IsEmpty: {IsEmpty})", 
				commandMessage.Command, commandSyncId, commandMessage.SessionId ?? "null", string.IsNullOrEmpty(commandMessage.SessionId));

			await using var scope = _scopeFactory.CreateAsyncScope();
			var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();

			// Set command context
			commandContext.SetCurrentInstance(commandMessage.InstanceId);
			commandContext.SetSessionId(commandMessage.SessionId);
			commandContext.SetData(commandMessage.Parameters.Select(static kvp =>
				new KeyValuePair<string, object>(kvp.Key, kvp.Value)));

			// Execute command
			var result = await _commandHandler.ExecuteCommandAsync(scope.ServiceProvider, commandMessage.Command);

			// Get the possibly updated session ID from command context
			var finalSessionId = commandContext.SessionId ?? commandMessage.SessionId;
			_logger.LogInformation("Final SessionId for response: '{FinalSessionId}' (Original: '{OriginalSessionId}', Context: '{ContextSessionId}')", 
				finalSessionId, commandMessage.SessionId ?? "null", commandContext.SessionId ?? "null");

			// Send result via gRPC to server
			if (result.IsSuccess)
			{
				await _grpcService.SendCommandResultAsync(commandSyncId, result, finalSessionId);
				_logger.LogInformation("Command result sent via gRPC: {Command} [{SyncId}] with SessionId: '{SessionId}'", 
					commandMessage.Command, commandSyncId, finalSessionId);
			}
			else
			{
				var errorMessage = result.Result?.Message ?? "Command execution failed";
				await _grpcService.SendCommandErrorAsync(commandSyncId, errorMessage, finalSessionId);
				_logger.LogWarning("Command error sent via gRPC: {Command} [{SyncId}] Error: {Error}",
					commandMessage.Command, commandSyncId, errorMessage);
			}

			Interlocked.Increment(ref _successfulCommands);
			_logger.LogInformation("Command completed successfully: {Command} [{SyncId}] Result: {Result}",
				commandMessage.Command, commandSyncId, result);

			return new ActionResult
			{
				IsSuccess = true,
				Message = $"Command '{commandMessage.Command}' executed successfully"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing command: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

			try
			{
				// Send error via gRPC to server
				// Get the session ID from context or use the original one
				var sessionId = "";
				try
				{
					await using var errorScope = _scopeFactory.CreateAsyncScope();
					var errorCommandContext = errorScope.ServiceProvider.GetRequiredService<ICommandContext>();
					sessionId = errorCommandContext.SessionId ?? commandMessage.SessionId;
				}
				catch
				{
					sessionId = commandMessage.SessionId;
				}

				await _grpcService.SendCommandErrorAsync(commandSyncId, ex.Message, sessionId);
				_logger.LogInformation("Command error sent via gRPC: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

				// Error handling is now managed by the base client class automatically
			}
			catch (Exception sendEx)
			{
				_logger.LogError(sendEx, "Failed to send error response via gRPC/WebSocket for command [{SyncId}]", commandSyncId);
			}

			Interlocked.Increment(ref _failedCommands);

			return new ActionResult
			{
				IsSuccess = false,
				Message = $"Command '{commandMessage.Command}' failed: {ex.Message}",
				Errors = new[] { ex.Message }
			};
		}
		finally
		{
			_activeCommands.TryRemove(commandSyncId, out _);
		}
	}

	private Task OnConnectionClosed(Exception? exception)
	{
		if (exception != null)
		{
			_logger.LogError(exception, "WebSocket connection closed with error");
		}
		else
		{
			_logger.LogInformation("WebSocket connection closed");
		}

		// Clear active commands since we're disconnected
		_activeCommands.Clear();
		return Task.CompletedTask;
	}

	private Task OnConnectionReconnecting(Exception? exception)
	{
		_logger.LogWarning(exception, "WebSocket connection reconnecting");
		return Task.CompletedTask;
	}

	private Task OnConnectionReconnected(string? connectionId)
	{
		_logger.LogInformation("WebSocket connection reconnected with ID: {ConnectionId}", connectionId);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Disposes the service and cleans up resources
	/// </summary>
	public override void Dispose()
	{
		// If IJiroClientHub implements IDisposable, dispose it here
		if (_jiroClient is IDisposable disposable)
		{
			disposable.Dispose();
		}
		base.Dispose();
	}
}
