using Jiro.Core.Constants;
using Jiro.Core.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Jiro.Core.Services.Context;

/// <summary>
/// Minimal instance model for API response deserialization.
/// </summary>
internal class InstanceModel
{
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Service for accessing and caching instance metadata.
/// </summary>
public class InstanceMetadataAccessor : IInstanceMetadataAccessor
{
    private readonly ILogger<InstanceMetadataAccessor> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IInstanceContext _instanceContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationOptions _applicationOptions;
    private const int MEMORY_CACHE_EXPIRATION_DAYS = 1;
    private const string STARTUP_INSTANCE_CACHE_KEY = "StartupInstanceId";

    /// <summary>
    /// Initializes a new instance of the InstanceMetadataAccessor class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="memoryCache">The memory cache instance.</param>
    /// <param name="instanceContext">The instance context service.</param>
    /// <param name="httpClientFactory">The HTTP client factory for API calls.</param>
    /// <param name="applicationOptions">The application options for API configuration.</param>
    public InstanceMetadataAccessor(
        ILogger<InstanceMetadataAccessor> logger,
        IMemoryCache memoryCache,
        IInstanceContext instanceContext,
        IHttpClientFactory httpClientFactory,
        IOptions<ApplicationOptions> applicationOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _instanceContext = instanceContext ?? throw new ArgumentNullException(nameof(instanceContext));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _applicationOptions = applicationOptions?.Value ?? throw new ArgumentNullException(nameof(applicationOptions));
    }

    /// <summary>
    /// Gets the instance ID from cache or fetches it from API if not cached.
    /// The instance ID should be consistent throughout the application lifetime.
    /// </summary>
    /// <param name="sessionId">Not used - kept for interface compatibility.</param>
    /// <returns>The instance ID if found, null otherwise.</returns>
    public async Task<string?> GetInstanceIdAsync(string sessionId)
    {
        // Check if we already have the instance ID cached from startup
        if (_memoryCache.TryGetValue(STARTUP_INSTANCE_CACHE_KEY, out string? cachedInstanceId) && !string.IsNullOrWhiteSpace(cachedInstanceId))
        {
            return cachedInstanceId;
        }

        // If not cached, fetch from API and cache it
        _logger.LogInformation("Instance ID not found in cache, fetching from API");
        var instanceId = await FetchInstanceIdFromApiAsync(_applicationOptions.ApiKey);
        
        if (!string.IsNullOrWhiteSpace(instanceId))
        {
            // Cache the instance ID for the application lifetime
            _memoryCache.Set(STARTUP_INSTANCE_CACHE_KEY, instanceId, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));
            _logger.LogInformation("Instance ID fetched and cached: {InstanceId}", instanceId);
        }

        return instanceId;
    }

    /// <summary>
    /// Gets the current instance ID from the cached API result.
    /// Falls back to instance context only if cache is empty.
    /// </summary>
    /// <returns>The current instance ID if available, null otherwise.</returns>
    public string? GetCurrentInstanceId()
    {
        // Try to get from startup cache (primary source)
        if (_memoryCache.TryGetValue(STARTUP_INSTANCE_CACHE_KEY, out string? cachedInstanceId) && !string.IsNullOrWhiteSpace(cachedInstanceId))
        {
            return cachedInstanceId;
        }

        // Fallback to instance context (legacy support)
        var instanceId = _instanceContext.InstanceId;
        
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            _logger.LogWarning("Current instance ID is null or empty in both cache and instance context");
        }
        else
        {
            _logger.LogDebug("Using instance ID from context as fallback: {InstanceId}", instanceId);
        }

        return instanceId;
    }

    /// <summary>
    /// Invalidates the cached instance ID. Since instance ID is global, this clears the main cache.
    /// </summary>
    /// <param name="sessionId">Not used - kept for interface compatibility.</param>
    public void InvalidateInstanceCache(string sessionId)
    {
        _memoryCache.Remove(STARTUP_INSTANCE_CACHE_KEY);
        _logger.LogInformation("Invalidated global instance ID cache");
    }

    /// <summary>
    /// Clears all cached instance metadata.
    /// </summary>
    public void ClearInstanceCache()
    {
        // Note: IMemoryCache doesn't provide a way to remove entries by pattern
        // This is a limitation of IMemoryCache. For more advanced cache invalidation,
        // consider using a distributed cache like Redis or implementing a custom cache wrapper
        _logger.LogInformation("ClearInstanceCache called - note that IMemoryCache doesn't support pattern-based removal");
        
        // Clear the startup instance cache specifically
        _memoryCache.Remove(STARTUP_INSTANCE_CACHE_KEY);
        
        // If we need to track all cache keys for bulk removal, we would need to implement
        // a custom cache wrapper that maintains a list of keys
    }

    /// <summary>
    /// Fetches the instance ID from the Jiro API using the provided API key.
    /// </summary>
    /// <param name="apiKey">The API key to use for authentication.</param>
    /// <returns>The instance ID if successful, null otherwise.</returns>
    public async Task<string?> FetchInstanceIdFromApiAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("FetchInstanceIdFromApiAsync called with null or empty apiKey");
            return null;
        }

        try
        {
            using var httpClient = _httpClientFactory.CreateClient(HttpClients.JIRO);
            
            var requestUri = $"api/instance/GetInstanceIdByApiKey?apiKey={Uri.EscapeDataString(apiKey)}";
            _logger.LogDebug("Fetching instance ID from API endpoint: {RequestUri}", requestUri);

            var response = await httpClient.GetAsync(requestUri);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Deserialize the instance model and extract the Id
                string? instanceId = null;
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var instanceModel = JsonSerializer.Deserialize<InstanceModel>(responseContent, options);
                    
                    if (instanceModel != null && !string.IsNullOrWhiteSpace(instanceModel.Id))
                    {
                        instanceId = instanceModel.Id;
                    }
                    else
                    {
                        _logger.LogWarning("API response deserialized but Id is null or empty: {ResponseContent}", responseContent);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize instance model JSON response: {ResponseContent}", responseContent);
                }

                if (!string.IsNullOrWhiteSpace(instanceId))
                {
                    _logger.LogInformation("Successfully fetched instance ID from API: {InstanceId}", instanceId);
                    return instanceId;
                }
                else
                {
                    _logger.LogWarning("API returned empty or null instance ID");
                    return null;
                }
            }
            else
            {
                _logger.LogError("Failed to fetch instance ID from API. Status: {StatusCode}, Reason: {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed while fetching instance ID from API");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching instance ID from API");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching instance ID from API");
            return null;
        }
    }

    /// <summary>
    /// Initializes the instance ID by fetching it from the API and caching it.
    /// Should be called during application startup.
    /// </summary>
    /// <param name="apiKey">The API key to use for authentication.</param>
    /// <returns>The fetched instance ID if successful, null otherwise.</returns>
    public async Task<string?> InitializeInstanceIdAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("InitializeInstanceIdAsync called with null or empty apiKey");
            return null;
        }

        try
        {
            var instanceId = await FetchInstanceIdFromApiAsync(apiKey);
            
            if (!string.IsNullOrWhiteSpace(instanceId))
            {
                // Cache the instance ID for the application lifetime
                _memoryCache.Set(STARTUP_INSTANCE_CACHE_KEY, instanceId, TimeSpan.FromDays(1));
                _logger.LogInformation("Instance ID cached successfully for startup: {InstanceId}", instanceId);
                return instanceId;
            }
            else
            {
                _logger.LogError("Failed to initialize instance ID - API returned null or empty value");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during instance ID initialization");
            return null;
        }
    }
}