namespace Jiro.Core.Services.Semaphore;

/// <summary>
/// Defines the contract for managing semaphores across different instances to control concurrent access to resources.
/// </summary>
public interface ISemaphoreManager
{
	/// <summary>
	/// Retrieves an existing semaphore for the specified instance or creates a new one if it doesn't exist.
	/// </summary>
	/// <param name="instanceId">The unique identifier for the instance that needs semaphore protection.</param>
	/// <returns>A <see cref="SemaphoreSlim"/> instance associated with the specified instance ID.</returns>
	SemaphoreSlim GetOrCreateInstanceSemaphore(string instanceId);
}
