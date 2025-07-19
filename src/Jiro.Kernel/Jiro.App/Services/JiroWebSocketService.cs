using System.Collections.Concurrent;

using Jiro.App.Options;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandHandler;
using Jiro.Shared.Websocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedCommandMessage = Jiro.Shared.Websocket.Requests.CommandMessage;
using SharedErrorResponse = Jiro.Shared.Websocket.Responses.ErrorResponse;

namespace Jiro.App.Services;

/// <summary>
/// Hosted service that manages WebSocket communication using IJiroClientHub
/// </summary>
public class JiroWebSocketService : BackgroundService, ICommandQueueMonitor
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JiroWebSocketService> _logger;
	private readonly ICommandHandlerService _commandHandler;
	private readonly WebSocketOptions _options;
	private readonly IJiroClientHub _jiroClientHub;

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
	public JiroWebSocketService(
		IServiceScopeFactory scopeFactory,
		ILogger<JiroWebSocketService> logger,
		ICommandHandlerService commandHandler,
		IOptions<WebSocketOptions> options,
		IJiroClientHub jiroClientHub)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_jiroClientHub = jiroClientHub ?? throw new ArgumentNullException(nameof(jiroClientHub));
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
		_jiroClientHub.CommandReceived += HandleCommandAsync;
		_jiroClientHub.Closed += OnConnectionClosed;
		_jiroClientHub.Reconnecting += OnConnectionReconnecting;
		_jiroClientHub.Reconnected += OnConnectionReconnected;

		// Optionally, register other event handlers for requests if needed

		// No explicit StartAsync for IJiroClientHub assumed; if needed, add here

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
		_jiroClientHub.CommandReceived -= HandleCommandAsync;
		_jiroClientHub.Closed -= OnConnectionClosed;
		_jiroClientHub.Reconnecting -= OnConnectionReconnecting;
		_jiroClientHub.Reconnected -= OnConnectionReconnected;

		// No explicit StopAsync for IJiroClientHub assumed; if needed, add here

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

	private async Task HandleCommandAsync(SharedCommandMessage commandMessage)
	{
		var commandSyncId = commandMessage.CommandSyncId;
		_activeCommands.TryAdd(commandSyncId, DateTime.UtcNow);
		Interlocked.Increment(ref _totalCommandsProcessed);

		try
		{
			_logger.LogInformation("Processing command: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();

			// Set command context
			commandContext.SetCurrentInstance(commandMessage.InstanceId);
			commandContext.SetSessionId(commandMessage.SessionId);
			commandContext.SetData(commandMessage.Parameters.Select(static kvp =>
				new KeyValuePair<string, object>(kvp.Key, kvp.Value)));

			// Execute command
			var result = await _commandHandler.ExecuteCommandAsync(scope.ServiceProvider, commandMessage.Command);

			// Note: IJiroClientHub interface doesn't have a method for sending command results
			// You may need to extend the interface or handle results through a different mechanism

			Interlocked.Increment(ref _successfulCommands);
			_logger.LogInformation("Command completed successfully: {Command} [{SyncId}] Result: {Result}",
				commandMessage.Command, commandSyncId, result);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing command: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

			try
			{
				var errorResponse = new SharedErrorResponse
				{
					ErrorMessage = $"[{commandSyncId}] {ex.Message}"
				};
				await _jiroClientHub.SendErrorResponseAsync(errorResponse, CancellationToken.None);
			}
			catch (Exception sendEx)
			{
				_logger.LogError(sendEx, "Failed to send error response via IJiroClientHub for command [{SyncId}]", commandSyncId);
			}

			Interlocked.Increment(ref _failedCommands);
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
		if (_jiroClientHub is IDisposable disposable)
		{
			disposable.Dispose();
		}
		base.Dispose();
	}
}
