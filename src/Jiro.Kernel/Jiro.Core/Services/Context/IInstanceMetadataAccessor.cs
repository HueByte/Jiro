namespace Jiro.Core.Services.Context;

/// <summary>
/// Service for accessing and caching instance metadata.
/// </summary>
public interface IInstanceMetadataAccessor
{
    /// <summary>
    /// Gets the instance ID from cache or fetches it from API if not cached.
    /// The instance ID should be consistent throughout the application lifetime.
    /// </summary>
    /// <param name="sessionId">Not used - kept for interface compatibility.</param>
    /// <returns>The instance ID if found, null otherwise.</returns>
    Task<string?> GetInstanceIdAsync(string sessionId);

    /// <summary>
    /// Gets the current instance ID from the cached API result.
    /// Falls back to instance context only if cache is empty.
    /// </summary>
    /// <returns>The current instance ID if available, null otherwise.</returns>
    string? GetCurrentInstanceId();

    /// <summary>
    /// Invalidates the cached instance ID. Since instance ID is global, this clears the main cache.
    /// </summary>
    /// <param name="sessionId">Not used - kept for interface compatibility.</param>
    void InvalidateInstanceCache(string sessionId);

    /// <summary>
    /// Clears all cached instance metadata.
    /// </summary>
    void ClearInstanceCache();

    /// <summary>
    /// Fetches the instance ID from the Jiro API using the provided API key.
    /// </summary>
    /// <param name="apiKey">The API key to use for authentication.</param>
    /// <returns>The instance ID if successful, null otherwise.</returns>
    Task<string?> FetchInstanceIdFromApiAsync(string apiKey);

    /// <summary>
    /// Initializes the instance ID by fetching it from the API and caching it.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="apiKey">The API key to use for authentication.</param>
    /// <returns>The fetched instance ID if successful, null otherwise.</returns>
    Task<string?> InitializeInstanceIdAsync(string apiKey);
}