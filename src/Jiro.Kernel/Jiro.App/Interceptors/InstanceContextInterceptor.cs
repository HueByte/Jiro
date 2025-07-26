using Grpc.Core;
using Grpc.Core.Interceptors;
using Jiro.Core.Services.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jiro.App.Interceptors;

/// <summary>
/// gRPC interceptor that sets the InstanceContext based on request metadata.
/// </summary>
public class InstanceContextInterceptor : Interceptor
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<InstanceContextInterceptor> _logger;

	public InstanceContextInterceptor(IServiceProvider serviceProvider, ILogger<InstanceContextInterceptor> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
		TRequest request,
		ServerCallContext context,
		UnaryServerMethod<TRequest, TResponse> continuation)
	{
		await SetContextFromMetadata(context);
		return await continuation(request, context);
	}

	public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
		IAsyncStreamReader<TRequest> requestStream,
		ServerCallContext context,
		ClientStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await SetContextFromMetadata(context);
		return await continuation(requestStream, context);
	}

	public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
		TRequest request,
		IServerStreamWriter<TResponse> responseStream,
		ServerCallContext context,
		ServerStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await SetContextFromMetadata(context);
		await continuation(request, responseStream, context);
	}

	public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
		IAsyncStreamReader<TRequest> requestStream,
		IServerStreamWriter<TResponse> responseStream,
		ServerCallContext context,
		DuplexStreamingServerMethod<TRequest, TResponse> continuation)
	{
		await SetContextFromMetadata(context);
		await continuation(requestStream, responseStream, context);
	}

	/// <summary>
	/// Sets the InstanceContext based on gRPC request metadata.
	/// Expected metadata keys: "instance-id" and optionally "session-id"
	/// </summary>
	private async Task SetContextFromMetadata(ServerCallContext context)
	{
		try
		{
			using var scope = _serviceProvider.CreateScope();
			var instanceContext = scope.ServiceProvider.GetRequiredService<IInstanceContext>();

			var instanceIdEntry = context.RequestHeaders.GetValue("instance-id");
			var sessionIdEntry = context.RequestHeaders.GetValue("session-id");

			if (!string.IsNullOrEmpty(instanceIdEntry))
			{
				instanceContext.SetContext(instanceIdEntry, sessionIdEntry);
				_logger.LogDebug("InstanceContext set from gRPC metadata - InstanceId: {InstanceId}, SessionId: {SessionId}",
					instanceIdEntry, sessionIdEntry ?? "null");
			}
			else
			{
				_logger.LogWarning("No instance-id found in gRPC metadata");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error setting InstanceContext from gRPC metadata");
		}

		await Task.CompletedTask;
	}
}