using System.Collections.Concurrent;

using Jiro.App.Options;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandHandler;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.App.Services;

/// <summary>
/// Hosted service that manages WebSocket communication for receiving commands and sending results via gRPC
/// </summary>
public class JiroWebSocketService : BackgroundService, ICommandQueueMonitor
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JiroWebSocketService> _logger;
	private readonly ICommandHandlerService _commandHandler;
	private readonly WebSocketOptions _options;
	private readonly IWebSocketConnection _webSocketConnection;

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
	/// <param name="webSocketConnection">WebSocket connection interface</param>
	public JiroWebSocketService(
		IServiceScopeFactory scopeFactory,
		ILogger<JiroWebSocketService> logger,
		ICommandHandlerService commandHandler,
		IOptions<WebSocketOptions> options,
		IWebSocketConnection webSocketConnection)
	{
		_scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
		_options = options?.Value ?? throw new ArgumentNullException(nameof(options));
		_webSocketConnection = webSocketConnection ?? throw new ArgumentNullException(nameof(webSocketConnection));
	}

	/// <summary>
	/// Starts the WebSocket service and establishes connection
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Task representing the async operation</returns>
	public override async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Starting Jiro WebSocket Service");

		// Register command handler
		_webSocketConnection.OnCommand(HandleCommandAsync);

		// Set up connection event handlers
		_webSocketConnection.Closed += OnConnectionClosed;
		_webSocketConnection.Reconnecting += OnConnectionReconnecting;
		_webSocketConnection.Reconnected += OnConnectionReconnected;

		try
		{
			await _webSocketConnection.StartAsync(cancellationToken);
			_logger.LogInformation("Jiro WebSocket Service started successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to start Jiro WebSocket Service");
			throw;
		}

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

		try
		{
			await _webSocketConnection.StopAsync(cancellationToken);
			_logger.LogInformation("Jiro WebSocket Service stopped successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error stopping Jiro WebSocket Service");
		}

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

	private async Task HandleCommandAsync(CommandMessage commandMessage)
	{
		var commandSyncId = commandMessage.CommandSyncId;
		_activeCommands.TryAdd(commandSyncId, DateTime.UtcNow);
		Interlocked.Increment(ref _totalCommandsProcessed);

		try
		{
			_logger.LogInformation("Processing command: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

			await using var scope = _scopeFactory.CreateAsyncScope();
			var commandContext = scope.ServiceProvider.GetRequiredService<ICommandContext>();
			var grpcService = scope.ServiceProvider.GetRequiredService<IJiroGrpcService>();

			// Set command context
			commandContext.SetCurrentInstance(commandMessage.InstanceId);
			commandContext.SetSessionId(commandMessage.SessionId);
			commandContext.SetData(commandMessage.Parameters.Select(kvp =>
				new KeyValuePair<string, object>(kvp.Key, kvp.Value)));

			// Execute command
			var result = await _commandHandler.ExecuteCommandAsync(scope.ServiceProvider, commandMessage.Command);

			// Send result via gRPC
			await grpcService.SendCommandResultAsync(commandSyncId, result);

			Interlocked.Increment(ref _successfulCommands);
			_logger.LogInformation("Command completed successfully: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing command: {Command} [{SyncId}]", commandMessage.Command, commandSyncId);

			try
			{
				await using var scope = _scopeFactory.CreateAsyncScope();
				var grpcService = scope.ServiceProvider.GetRequiredService<IJiroGrpcService>();
				await grpcService.SendCommandErrorAsync(commandSyncId, ex.Message);
			}
			catch (Exception grpcEx)
			{
				_logger.LogError(grpcEx, "Failed to send error result via gRPC for command [{SyncId}]", commandSyncId);
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
		_webSocketConnection?.Dispose();
		base.Dispose();
	}
}
