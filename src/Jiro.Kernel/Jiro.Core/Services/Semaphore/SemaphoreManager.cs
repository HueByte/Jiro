using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Semaphore;

/// <summary>
/// Manages semaphores for different instances to ensure thread-safe access to resources.
/// </summary>
public class SemaphoreManager : ISemaphoreManager
{
	private readonly ILogger<SemaphoreManager> _logger;
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _personaSemaphore = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="SemaphoreManager"/> class.
	/// </summary>
	/// <param name="logger">The logger instance for logging semaphore operations.</param>
	public SemaphoreManager(ILogger<SemaphoreManager> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Retrieves an existing semaphore for the specified instance or creates a new one if it doesn't exist.
	/// Each semaphore is configured to allow only one concurrent operation (maxCount = 1).
	/// </summary>
	/// <param name="instanceId">The unique identifier for the instance that needs semaphore protection.</param>
	/// <returns>A <see cref="SemaphoreSlim"/> instance associated with the specified instance ID.</returns>
	public SemaphoreSlim GetOrCreateInstanceSemaphore(string instanceId)
	{
		return _personaSemaphore.GetOrAdd(instanceId, static _ => new SemaphoreSlim(1, 1));
	}
}
