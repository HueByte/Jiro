using Jiro.Core.Services.Logging.Models;

namespace Jiro.Core.Services.Logging;

/// <summary>
/// Service for retrieving and processing log files
/// </summary>
public interface ILogsProviderService
{
	/// <summary>
	/// Retrieves logs with optional filtering and limiting
	/// </summary>
	/// <param name="level">The log level to filter by (optional)</param>
	/// <param name="limit">Maximum number of log entries to return</param>
	/// <returns>A structured response containing log entries</returns>
	Task<LogsResponse> GetLogsAsync(string? level = null, int limit = 100);
}
