namespace Jiro.Core.Services.Semaphore;

public interface IChatSemaphoreManager
{
	SemaphoreSlim GetOrCreateInstanceSemaphore (ulong instanceId);
}
