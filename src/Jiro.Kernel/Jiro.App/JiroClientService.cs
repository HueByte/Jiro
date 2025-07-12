using Grpc.Net.ClientFactory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jiro.App;

/// <summary>
/// A hosted service that manages gRPC client factory configuration for communication with the Jiro server.
/// This service is now simplified as WebSocket handles command reception and gRPC only sends results.
/// </summary>
internal class JiroClientService : IHostedService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<JiroClientService> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroClientService"/> class.
	/// </summary>
	/// <param name="scopeFactory">Factory for creating service scopes for dependency injection.</param>
	/// <param name="logger">Logger instance for recording service events and errors.</param>
	public JiroClientService(IServiceScopeFactory scopeFactory, ILogger<JiroClientService> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	/// <summary>
	/// Starts the gRPC client service, ensuring the gRPC client factory is properly configured.
	/// </summary>
	/// <param name="cancellationToken">Token to signal cancellation of the service startup.</param>
	/// <returns>A task representing the asynchronous start operation.</returns>
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Jiro gRPC Client Service started");

		// Validate gRPC client configuration
		try
		{
			await using var scope = _scopeFactory.CreateAsyncScope();
			var clientFactory = scope.ServiceProvider.GetRequiredService<GrpcClientFactory>();
			_logger.LogInformation("gRPC client factory configured successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to configure gRPC client factory");
			throw;
		}

		await Task.CompletedTask;
	}

	/// <summary>
	/// Stops the hosted service gracefully when the application is shutting down.
	/// </summary>
	/// <param name="cancellationToken">Token to signal cancellation of the service shutdown.</param>
	/// <returns>A completed task representing the shutdown operation.</returns>
	public Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Jiro gRPC Client Service stopped");
		return Task.CompletedTask;
	}
}
