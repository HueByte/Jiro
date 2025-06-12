namespace Jiro.Core.Services.Semaphore;

public interface IChatSemaphoreManager
{
	SemaphoreSlim GetOrCreateInstanceSemaphore (string instanceId);
}
