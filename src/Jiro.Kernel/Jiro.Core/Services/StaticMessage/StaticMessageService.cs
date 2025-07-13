using Jiro.Core.Constants;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.StaticMessage;

/// <summary>
/// Manages static markdown messages from the file system with caching capabilities.
/// This service handles persona messages, system prompts, and other static content stored as markdown files.
/// </summary>
public class StaticMessageService : IStaticMessageService
{
    private readonly ILogger<StaticMessageService> _logger;
    private readonly IMemoryCache _memoryCache;
    private const int DEFAULT_CACHE_EXPIRATION_MINUTES = 5;

    /// <summary>
    /// Initializes a new instance of the StaticMessageService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="memoryCache">The memory cache instance.</param>
    public StaticMessageService(ILogger<StaticMessageService> logger, IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Retrieves a static message from cache or file system.
    /// </summary>
    /// <param name="key">The message key/identifier.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the message content or null if not found.</returns>
    public async Task<string?> GetStaticMessageAsync(string key)
    {
        try
        {
            // Check cache first
            if (_memoryCache.TryGetValue(key, out string? cachedMessage))
            {
                _logger.LogDebug("Static message found in cache for key: {Key}", key);
                return cachedMessage;
            }

            // Try to load from file system
            var filePath = Path.Join(Constants.Paths.MessageBasePath, $"{key}.md");
            if (File.Exists(filePath))
            {
                _logger.LogDebug("Loading static message from file: {FilePath}", filePath);
                var message = await File.ReadAllTextAsync(filePath);

                // Cache the message with default expiration
                _memoryCache.Set(key, message, TimeSpan.FromMinutes(DEFAULT_CACHE_EXPIRATION_MINUTES));

                _logger.LogInformation("Static message loaded and cached for key: {Key}", key);
                return message;
            }

            _logger.LogWarning("Static message file not found for key: {Key} at path: {FilePath}", key, filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving static message for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Retrieves the core persona message used for AI interactions.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the persona message or null if not found.</returns>
    public async Task<string?> GetPersonaCoreMessageAsync()
    {
        return await GetStaticMessageAsync(Constants.CacheKeys.CorePersonaMessageKey);
    }

    /// <summary>
    /// Clears a specific static message from cache.
    /// </summary>
    /// <param name="key">The message key to clear from cache.</param>
    public void InvalidateStaticMessage(string key)
    {
        _memoryCache.Remove(key);
        _logger.LogInformation("Invalidated static message cache for key: {Key}", key);
    }

    /// <summary>
    /// Clears all static message cache entries.
    /// </summary>
    public void ClearStaticMessageCache()
    {
        // Remove common static message cache entries
        _memoryCache.Remove(Constants.CacheKeys.ComputedPersonaMessageKey);
        _memoryCache.Remove(Constants.CacheKeys.CorePersonaMessageKey);

        _logger.LogInformation("Cleared static message cache entries");
    }

    /// <summary>
    /// Sets a static message in cache with custom expiration.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="message">The message content.</param>
    /// <param name="expirationMinutes">The expiration time in minutes.</param>
    public void SetStaticMessage(string key, string message, int expirationMinutes)
    {
        _memoryCache.Set(key, message, TimeSpan.FromMinutes(expirationMinutes));
        _logger.LogInformation("Set static message in cache with key {Key} and expiration {Minutes} minutes", key, expirationMinutes);
    }
}
