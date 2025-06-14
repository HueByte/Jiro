using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Semaphore;

public class SemaphoreManager : ISemaphoreManager
{
	private readonly ILogger<SemaphoreManager> _logger;
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _personaSemaphore = new();

	public SemaphoreManager (ILogger<SemaphoreManager> logger)
	{
		_logger = logger;
	}

	public SemaphoreSlim GetOrCreateInstanceSemaphore (string instanceId)
	{
		return _personaSemaphore.GetOrAdd(instanceId, _ => new SemaphoreSlim(1, 1));
	}
}
