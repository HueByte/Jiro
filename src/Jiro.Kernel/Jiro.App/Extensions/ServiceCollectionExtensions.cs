using Jiro.App.Options;
using Jiro.App.Services;
using Jiro.Core.Services.CommandHandler;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.App.Extensions;

/// <summary>
/// Extension methods for configuring Jiro communication services
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the new WebSocket and gRPC communication services to the service collection.
	/// This configures the architecture where WebSocket (SignalR) receives commands and gRPC sends results.
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="configuration">The configuration</param>
	/// <returns>The service collection for chaining</returns>
	public static IServiceCollection AddJiroCommunication(this IServiceCollection services, IConfiguration configuration)
	{
		// Configure options for both WebSocket and gRPC
		services.Configure<WebSocketOptions>(configuration.GetSection("WebSocket"));
		services.Configure<GrpcOptions>(configuration.GetSection("Grpc"));

		// Register gRPC service for sending command results
		services.AddScoped<IJiroGrpcService, JiroGrpcService>();

		// Register WebSocket connection implementation for receiving commands
		services.AddSingleton<IWebSocketConnection, SignalRWebSocketConnection>();

		// Register as hosted service to start/stop with the application
		services.AddHostedService<JiroWebSocketService>();

		// Register the service for command queue monitoring
		services.AddSingleton<ICommandQueueMonitor, JiroWebSocketService>();

		return services;
	}
}
