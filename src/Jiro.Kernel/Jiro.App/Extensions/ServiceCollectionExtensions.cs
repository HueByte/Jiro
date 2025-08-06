using Jiro.App.Services;
using Jiro.Core.Options;
using Jiro.Core.Services.CommandHandler;
using Jiro.Shared.Websocket;

using Microsoft.AspNetCore.SignalR.Client;
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
	/// Adds the WebSocket and gRPC communication services to the service collection.
	/// This configures the architecture where WebSocket (SignalR) receives commands and gRPC sends results.
	/// </summary>
	/// <param name="services">The service collection</param>
	/// <param name="configuration">The configuration</param>
	/// <returns>The service collection for chaining</returns>
	public static IServiceCollection AddJiroCommunication(this IServiceCollection services, IConfiguration configuration)
	{
		// Configure JiroCloud options which contains both WebSocket and gRPC configuration
		services.Configure<JiroCloudOptions>(configuration.GetSection(JiroCloudOptions.JiroCloud));

		// Register exception handlers and interceptors
		services.AddSingleton<WebSocketExceptionHandler>();
		services.AddSingleton<GrpcExceptionInterceptor>();
		services.AddSingleton<Jiro.App.Interceptors.InstanceContextInterceptor>();

		// Register gRPC service for sending command results
		services.AddScoped<IJiroGrpcService, JiroGrpcService>();

		// Register IJiroClient implementation for WebSocket communication
		services.AddSingleton<IJiroClient, WebSocketConnection>(services =>
		{
			var logger = services.GetRequiredService<ILogger<WebSocketConnection>>();
			var jiroCloudOptions = services.GetRequiredService<IOptions<JiroCloudOptions>>();
			var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
			var commandHandler = services.GetRequiredService<ICommandHandlerService>();
			var exceptionHandler = services.GetRequiredService<WebSocketExceptionHandler>();

			var cloudConfig = jiroCloudOptions.Value;
			if (string.IsNullOrEmpty(cloudConfig.WebSocket.HubUrl))
			{
				throw new InvalidOperationException("JiroCloud WebSocket HubUrl is required. Please configure 'JiroCloud:WebSocket:HubUrl' in your settings.");
			}

			if (string.IsNullOrEmpty(cloudConfig.ApiKey))
			{
				throw new InvalidOperationException("JiroCloud ApiKey is required. Please provide 'JiroCloud:ApiKey' or 'JIRO_JiroCloud__ApiKey' environment variable in configuration.");
			}

			// Build the hub URL with API key query parameter
			string hubUrl = cloudConfig.WebSocket.HubUrl;
			var separator = hubUrl.Contains('?') ? "&" : "?";
			hubUrl = $"{hubUrl}{separator}api_key={Uri.EscapeDataString(cloudConfig.ApiKey)}";

			var connection = new HubConnectionBuilder()
				.AddJsonProtocol(options =>
				{
					options.PayloadSerializerOptions.PropertyNamingPolicy = null; // Use default property names
					options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
				})
				.WithUrl(hubUrl, hubOptions =>
				{
					// Configure additional headers if provided
					if (cloudConfig.WebSocket.Headers?.Count > 0)
					{
						foreach (var header in cloudConfig.WebSocket.Headers)
						{
							hubOptions.Headers.Add(header.Key, header.Value);
						}
					}
				})
				// Custom reconnection intervals: 0s, 2s, 10s, 30s, 60s, then always 60s
				.WithAutomaticReconnect(new SocketRetryPolicy())
				.ConfigureLogging(logging =>
				{
					logging.SetMinimumLevel(LogLevel.Information);
				})
				.Build();

			return new WebSocketConnection(connection, logger, jiroCloudOptions, scopeFactory, commandHandler, exceptionHandler);
		});

		// Register as hosted service to start/stop with the application
		services.AddHostedService<JiroWebSocketService>();

		// Register the service for command queue monitoring
		services.AddSingleton<ICommandQueueMonitor, JiroWebSocketService>();

		return services;
	}
}
