using Jiro.Shared.Websocket.Responses;

using Microsoft.Extensions.Logging;

namespace Jiro.App.Services;

/// <summary>
/// Centralized exception handler for WebSocket operations
/// </summary>
public class WebSocketExceptionHandler
{
	private readonly ILogger<WebSocketExceptionHandler> _logger;

	public WebSocketExceptionHandler(ILogger<WebSocketExceptionHandler> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Handles exceptions in WebSocket operations and returns appropriate error responses
	/// </summary>
	/// <param name="exception">The exception that occurred</param>
	/// <param name="requestId">The request ID for tracking</param>
	/// <param name="commandName">The command name that failed</param>
	/// <param name="context">Additional context information</param>
	/// <returns>Error response to send to the client</returns>
	public ErrorResponse HandleException(Exception exception, string requestId, string commandName, string? context = null)
	{
		// Log the full exception with stack trace to file logs only
		_logger.LogError(exception, "WebSocket operation failed - Command: {CommandName}, RequestId: {RequestId}, Context: {Context}",
			commandName, requestId, context ?? "None");

		// Log a clean message to console without stack trace
		_logger.LogWarning("WebSocket {CommandName} failed: {Message} [RequestId: {RequestId}]",
			commandName, GetUserFriendlyMessage(exception), requestId);

		return new ErrorResponse
		{
			RequestId = requestId,
			CommandName = commandName,
			ErrorMessage = GetUserFriendlyMessage(exception)
		};
	}

	/// <summary>
	/// Handles exceptions during WebSocket connection lifecycle
	/// </summary>
	/// <param name="exception">The exception that occurred</param>
	/// <param name="operation">The operation that failed (e.g., "Connect", "Disconnect", "Reconnect")</param>
	public void HandleConnectionException(Exception exception, string operation)
	{
		// Log the full exception with stack trace to file logs only
		_logger.LogError(exception, "WebSocket connection operation failed - Operation: {Operation}", operation);

		// Log a clean message to console without stack trace
		_logger.LogWarning("WebSocket {Operation} failed: {Message}", operation, GetUserFriendlyMessage(exception));
	}

	/// <summary>
	/// Handles exceptions during event handler execution
	/// </summary>
	/// <param name="exception">The exception that occurred</param>
	/// <param name="eventName">The event that failed</param>
	/// <param name="additionalContext">Additional context information</param>
	public void HandleEventException(Exception exception, string eventName, string? additionalContext = null)
	{
		// Log the full exception with stack trace to file logs only
		_logger.LogError(exception, "WebSocket event handler failed - Event: {EventName}, Context: {Context}",
			eventName, additionalContext ?? "None");

		// Log a clean message to console without stack trace
		_logger.LogWarning("WebSocket event {EventName} failed: {Message}",
			eventName, GetUserFriendlyMessage(exception));
	}

	/// <summary>
	/// Gets a user-friendly error message from an exception
	/// </summary>
	/// <param name="exception">The exception to process</param>
	/// <returns>A clean, user-friendly error message</returns>
	private static string GetUserFriendlyMessage(Exception exception)
	{
		return exception switch
		{
			ArgumentNullException => "Invalid request parameters",
			ArgumentException => "Invalid request format",
			InvalidOperationException => "Operation cannot be performed at this time",
			TimeoutException => "Request timed out",
			UnauthorizedAccessException => "Access denied",
			System.Net.Http.HttpRequestException => "Network communication error",
			TaskCanceledException => "Request was cancelled",
			NotImplementedException => "Feature not implemented",
			_ => "An error occurred while processing the request"
		};
	}

	/// <summary>
	/// Determines if an exception should be retried
	/// </summary>
	/// <param name="exception">The exception to evaluate</param>
	/// <returns>True if the operation should be retried</returns>
	public static bool ShouldRetry(Exception exception)
	{
		return exception switch
		{
			TaskCanceledException => true,
			TimeoutException => true,
			System.Net.Http.HttpRequestException => true,
			System.Net.Sockets.SocketException => true,
			_ => false
		};
	}
}
