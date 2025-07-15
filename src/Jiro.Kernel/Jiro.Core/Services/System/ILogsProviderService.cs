using Jiro.Core.Services.System.Models;

namespace Jiro.Core.Services.System;

/// <summary>
/// Service for retrieving system logs
/// </summary>
public interface ILogsProviderService
{
	/// <summary>
	/// Retrieves logs based on level filter and limit
	/// </summary>
	/// <param name="level">Log level filter (optional)</param>
	/// <param name="limit">Maximum number of logs to retrieve</param>
	/// <returns>Log response containing filtered logs</returns>
	Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100);
}
