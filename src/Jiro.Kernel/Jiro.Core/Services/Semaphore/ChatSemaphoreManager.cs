using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.Semaphore;

public class ChatSemaphoreManager : IChatSemaphoreManager
{
	private readonly ILogger<ChatSemaphoreManager> _logger;
	private readonly ConcurrentDictionary<string, SemaphoreSlim> _personaSemaphore = new();

	public ChatSemaphoreManager (ILogger<ChatSemaphoreManager> logger)
	{
		_logger = logger;
	}

	public SemaphoreSlim GetOrCreateInstanceSemaphore (string instanceId)
	{
		return _personaSemaphore.GetOrAdd(instanceId, _ => new SemaphoreSlim(1, 1));
	}
}
