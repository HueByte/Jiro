using Jiro.App.Options;
using Jiro.App.Services;
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
				webSocketOptions.ApiKey = configuration.GetValue<string>("ApiKey");
			}

			// Ensure ApiKey is provided
			if (string.IsNullOrEmpty(webSocketOptions.ApiKey))
			{
				throw new InvalidOperationException("WebSocket ApiKey is required. Please provide either 'WebSocket:ApiKey' or 'API_KEY' in configuration.");
			}
		});

		services.Configure<GrpcOptions>(configuration.GetSection("Grpc"));

		// Register exception handlers
		services.AddSingleton<WebSocketExceptionHandler>();
		services.AddSingleton<GrpcExceptionInterceptor>();

		// Register gRPC service for sending command results
		services.AddScoped<IJiroGrpcService, JiroGrpcService>();

		// Register IJiroClientHub implementation for WebSocket communication
		services.AddSingleton<IJiroClient, WebSocketConnection>(services =>
		{
			var logger = services.GetRequiredService<ILogger<WebSocketConnection>>();
			var options = services.GetRequiredService<IOptions<WebSocketOptions>>();
			var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
			var commandHandler = services.GetRequiredService<ICommandHandlerService>();
			var exceptionHandler = services.GetRequiredService<WebSocketExceptionHandler>();

			var webSocketOptions = options.Value;
			if (string.IsNullOrEmpty(webSocketOptions.HubUrl))
			{
				throw new InvalidOperationException("WebSocket HubUrl is required. Please configure 'WebSocket:HubUrl' in your settings.");
			}

			// Build the hub URL with API key query parameter
			string hubUrl = webSocketOptions.HubUrl;
			var separator = hubUrl.Contains('?') ? "&" : "?";
			hubUrl = $"{hubUrl}{separator}api_key={Uri.EscapeDataString(webSocketOptions.ApiKey ?? "")}";


			var connection = new HubConnectionBuilder()
				.AddJsonProtocol(options =>
				{
					options.PayloadSerializerOptions.PropertyNamingPolicy = null; // Use default property names
					options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
				})
				.WithUrl(hubUrl, hubOptions =>
				{
					// Configure additional headers if provided
					if (webSocketOptions.Headers?.Count > 0)
					{
						foreach (var header in webSocketOptions.Headers)
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

			return new WebSocketConnection(connection, logger, options, scopeFactory, commandHandler, exceptionHandler);
		});

		// Register as hosted service to start/stop with the application
		services.AddHostedService<JiroWebSocketService>();

		// Register the service for command queue monitoring
		services.AddSingleton<ICommandQueueMonitor, JiroWebSocketService>();

		return services;
	}
}
