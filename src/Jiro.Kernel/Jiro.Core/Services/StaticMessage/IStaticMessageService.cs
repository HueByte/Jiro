using Microsoft.Extensions.Caching.Memory;

namespace Jiro.Core.Services.StaticMessage;

/// <summary>
/// Defines the contract for managing static markdown messages from the file system.
/// </summary>
public interface IStaticMessageService
{
    /// <summary>
    /// Retrieves a static message from cache or file system.
    /// </summary>
    /// <param name="key">The message key/identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the message content or null if not found.</returns>
    Task<string?> GetStaticMessageAsync(string key);

    /// <summary>
    /// Retrieves the core persona message used for AI interactions.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the persona message or null if not found.</returns>
    Task<string?> GetPersonaCoreMessageAsync();

    /// <summary>
    /// Clears a specific static message from cache.
    /// </summary>
    /// <param name="key">The message key to clear from cache.</param>
    void InvalidateStaticMessage(string key);

    /// <summary>
    /// Clears all static message cache entries.
    /// </summary>
    void ClearStaticMessageCache();

    /// <summary>
    /// Sets a static message in cache with custom expiration.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="message">The message content.</param>
    /// <param name="expirationMinutes">The expiration time in minutes.</param>
    void SetStaticMessage(string key, string message, int expirationMinutes);
}
