namespace Jiro.Core.Services.Semaphore;

public interface ISemaphoreManager
{
	SemaphoreSlim GetOrCreateInstanceSemaphore(string instanceId);
}
