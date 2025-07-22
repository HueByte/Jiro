using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.Logging;

namespace Jiro.App.Services;

/// <summary>
/// gRPC interceptor for centralized exception handling and logging
/// </summary>
public class GrpcExceptionInterceptor : Interceptor
{
	private readonly ILogger<GrpcExceptionInterceptor> _logger;

	public GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Intercepts unary gRPC calls to handle exceptions
	/// </summary>
	public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
		TRequest request,
		ClientInterceptorContext<TRequest, TResponse> context,
		AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
	{
		var call = continuation(request, context);

		return new AsyncUnaryCall<TResponse>(
			HandleUnaryResponse<TRequest, TResponse>(call.ResponseAsync, context.Method),
			call.ResponseHeadersAsync,
			call.GetStatus,
			call.GetTrailers,
			call.Dispose);
	}

	/// <summary>
	/// Handles the response from a unary gRPC call with proper exception handling
	/// </summary>
	private async Task<TResponse> HandleUnaryResponse<TRequest, TResponse>(Task<TResponse> responseTask, Method<TRequest, TResponse> method)
	{
		try
		{
			var response = await responseTask;

			// Log successful gRPC calls at debug level
			_logger.LogDebug("gRPC call successful - Method: {Method}", method.Name);

			return response;
		}
		catch (RpcException rpcEx)
		{
			HandleGrpcException(rpcEx, method.Name);
			throw;
		}
		catch (Exception ex)
		{
			HandleGeneralException(ex, method.Name);
			throw;
		}
	}

	/// <summary>
	/// Handles gRPC-specific exceptions
	/// </summary>
	/// <param name="rpcException">The RPC exception that occurred</param>
	/// <param name="methodName">The gRPC method that failed</param>
	private void HandleGrpcException(RpcException rpcException, string methodName)
	{
		var statusCode = rpcException.StatusCode;
		var message = GetUserFriendlyGrpcMessage(rpcException);

		// Log the full exception with stack trace to file logs only
		_logger.LogError(rpcException, "gRPC call failed - Method: {Method}, StatusCode: {StatusCode}, Details: {Details}",
			methodName, statusCode, rpcException.Status.Detail);

		// Log a clean message to console without stack trace
		_logger.LogWarning("gRPC {Method} failed [{StatusCode}]: {Message}",
			methodName, statusCode, message);
	}

	/// <summary>
	/// Handles general exceptions that occur during gRPC calls
	/// </summary>
	/// <param name="exception">The exception that occurred</param>
	/// <param name="methodName">The gRPC method that failed</param>
	private void HandleGeneralException(Exception exception, string methodName)
	{
		// Log the full exception with stack trace to file logs only
		_logger.LogError(exception, "gRPC call failed with unexpected exception - Method: {Method}", methodName);

		// Log a clean message to console without stack trace
		_logger.LogWarning("gRPC {Method} failed: {Message}", methodName, GetUserFriendlyMessage(exception));
	}

	/// <summary>
	/// Gets a user-friendly message from a gRPC exception
	/// </summary>
	/// <param name="rpcException">The RPC exception to process</param>
	/// <returns>A clean, user-friendly error message</returns>
	private static string GetUserFriendlyGrpcMessage(RpcException rpcException)
	{
		return rpcException.StatusCode switch
		{
			StatusCode.Unavailable => "Service temporarily unavailable",
			StatusCode.DeadlineExceeded => "Request timeout",
			StatusCode.Unauthenticated => "Authentication failed",
			StatusCode.PermissionDenied => "Access denied",
			StatusCode.NotFound => "Service endpoint not found",
			StatusCode.InvalidArgument => "Invalid request parameters",
			StatusCode.FailedPrecondition => "Service not ready",
			StatusCode.Aborted => "Request was aborted",
			StatusCode.OutOfRange => "Request parameters out of range",
			StatusCode.Unimplemented => "Feature not implemented",
			StatusCode.Internal => "Internal server error",
			StatusCode.DataLoss => "Data corruption detected",
			StatusCode.Cancelled => "Request was cancelled",
			_ => rpcException.Status.Detail ?? "Unknown gRPC error"
		};
	}

	/// <summary>
	/// Gets a user-friendly error message from a general exception
	/// </summary>
	/// <param name="exception">The exception to process</param>
	/// <returns>A clean, user-friendly error message</returns>
	private static string GetUserFriendlyMessage(Exception exception)
	{
		return exception switch
		{
			TaskCanceledException => "Request was cancelled",
			TimeoutException => "Request timed out",
			System.Net.Http.HttpRequestException => "Network communication error",
			InvalidOperationException => "Operation cannot be performed at this time",
			ArgumentException => "Invalid request parameters",
			UnauthorizedAccessException => "Access denied",
			_ => "An error occurred while communicating with the server"
		};
	}

	/// <summary>
	/// Determines if a gRPC exception indicates a retriable error
	/// </summary>
	/// <param name="rpcException">The RPC exception to evaluate</param>
	/// <returns>True if the operation should be retried</returns>
	public static bool ShouldRetryGrpcError(RpcException rpcException)
	{
		return rpcException.StatusCode switch
		{
			StatusCode.Unavailable => true,
			StatusCode.DeadlineExceeded => true,
			StatusCode.Internal => true,
			StatusCode.Aborted => true,
			StatusCode.ResourceExhausted => true,
			_ => false
		};
	}

	/// <summary>
	/// Determines if a general exception should be retried
	/// </summary>
	/// <param name="exception">The exception to evaluate</param>
	/// <returns>True if the operation should be retried</returns>
	public static bool ShouldRetryGeneralError(Exception exception)
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
