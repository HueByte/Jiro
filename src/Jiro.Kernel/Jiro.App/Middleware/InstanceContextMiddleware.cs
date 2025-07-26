using Jiro.Core.Services.Context;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jiro.App.Middleware;

/// <summary>
/// Middleware that sets the InstanceContext based on HTTP request headers or query parameters.
/// Used for WebSocket connections and HTTP API requests.
/// </summary>
public class InstanceContextMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<InstanceContextMiddleware> _logger;

	public InstanceContextMiddleware(RequestDelegate next, ILogger<InstanceContextMiddleware> logger)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			// Get InstanceContext from DI container
			var instanceContext = context.RequestServices.GetRequiredService<IInstanceContext>();

			// Try to get instance-id from headers first, then query parameters
			string? instanceId = context.Request.Headers["instance-id"].FirstOrDefault()
				?? context.Request.Query["instance_id"].FirstOrDefault()
				?? context.Request.Query["instanceId"].FirstOrDefault();

			// Try to get session-id from headers first, then query parameters
			string? sessionId = context.Request.Headers["session-id"].FirstOrDefault()
				?? context.Request.Query["session_id"].FirstOrDefault()
				?? context.Request.Query["sessionId"].FirstOrDefault();

			if (!string.IsNullOrEmpty(instanceId))
			{
				instanceContext.SetContext(instanceId, sessionId);
				_logger.LogDebug("InstanceContext set from HTTP request - InstanceId: {InstanceId}, SessionId: {SessionId}",
					instanceId, sessionId ?? "null");
			}
			else
			{
				_logger.LogDebug("No instance-id found in HTTP request headers or query parameters");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error setting InstanceContext from HTTP request");
		}

		await _next(context);
	}
}
