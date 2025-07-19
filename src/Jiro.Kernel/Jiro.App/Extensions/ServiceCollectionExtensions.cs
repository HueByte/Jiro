using Jiro.App.Options;
using Jiro.App.Services;
using Jiro.Shared.Websocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
		services.Configure<WebSocketOptions>(webSocketOptions =>
		{
			configuration.GetSection("WebSocket").Bind(webSocketOptions);

			// If ApiKey is not set in WebSocket section, use the global API_KEY
			if (string.IsNullOrEmpty(webSocketOptions.ApiKey))
			{
				webSocketOptions.ApiKey = configuration.GetValue<string>("API_KEY");
			}

			// Ensure ApiKey is provided
			if (string.IsNullOrEmpty(webSocketOptions.ApiKey))
			{
				throw new InvalidOperationException("WebSocket ApiKey is required. Please provide either 'WebSocket:ApiKey' or 'API_KEY' in configuration.");
			}
		});

		services.Configure<GrpcOptions>(configuration.GetSection("Grpc"));

		// Register gRPC service for sending command results
		services.AddScoped<IJiroGrpcService, JiroGrpcService>();

		// Register IJiroClientHub implementation for WebSocket communication
		services.AddSingleton<IJiroClientHub, WebSocketConnection>();

		// Register as hosted service to start/stop with the application
		services.AddHostedService<JiroWebSocketService>();

		// Register the service for command queue monitoring
		services.AddSingleton<ICommandQueueMonitor, JiroWebSocketService>();

		return services;
	}
}
