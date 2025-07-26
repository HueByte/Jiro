using System.Net.Http.Json;
using System.Text.Json;

using Jiro.Core.Constants;
using Jiro.Core.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.Context;

/// <summary>
/// User model for API response deserialization.
/// </summary>
public class UserModel
{
	/// <summary>
	/// Gets or sets the unique identifier for the user.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the username of the user.
	/// </summary>
	public string UserName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the email address of the user.
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the date and time when the user account was created.
	/// </summary>
	public DateTime AccountCreatedDate { get; set; }
}

/// <summary>
/// Instance data model for API response deserialization.
/// </summary>
public class InstanceDataModel
{
	/// <summary>
	/// Gets or sets the unique identifier for the instance.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the display name of the instance.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the description of the instance.
	/// </summary>
	public string Description { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the date and time when the instance was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the date and time when the instance was last online.
	/// </summary>
	public DateTime LastOnline { get; set; }

	/// <summary>
	/// Gets or sets the API key associated with this instance.
	/// </summary>
	public string ApiKey { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the unique identifier of the user who owns this instance.
	/// </summary>
	public string UserId { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the user information associated with this instance.
	/// </summary>
	public UserModel? User { get; set; }
}

/// <summary>
/// Root response model for instance metadata API.
/// </summary>
public class InstanceMetadataResponse
{
	/// <summary>
	/// Gets or sets the instance data returned from the API.
	/// Contains detailed information about the instance including user data.
	/// </summary>
	public InstanceDataModel? Data { get; set; }
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
	private const string STARTUP_METADATA_CACHE_KEY = "StartupInstanceMetadata";

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
		var metadata = await FetchInstanceMetadataFromApiAsync(_applicationOptions.ApiKey);
		
		if (metadata?.Data != null && !string.IsNullOrWhiteSpace(metadata.Data.Id))
		{
			var instanceId = metadata.Data.Id;
			// Cache both the instance ID and full metadata for the application lifetime
			_memoryCache.Set(STARTUP_INSTANCE_CACHE_KEY, instanceId, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));
			_memoryCache.Set(STARTUP_METADATA_CACHE_KEY, metadata, TimeSpan.FromDays(MEMORY_CACHE_EXPIRATION_DAYS));
			_logger.LogInformation("Instance metadata fetched and cached: {InstanceId} - {Name}", instanceId, metadata.Data.Name);
			return instanceId;
		}

		return null;
	}

	/// <summary>
	/// Gets the cached instance metadata if available.
	/// </summary>
	/// <returns>The cached instance metadata if available, null otherwise.</returns>
	public InstanceMetadataResponse? GetCachedInstanceMetadata()
	{
		if (_memoryCache.TryGetValue(STARTUP_METADATA_CACHE_KEY, out InstanceMetadataResponse? cachedMetadata))
		{
			return cachedMetadata;
		}

		_logger.LogDebug("Instance metadata not found in cache");
		return null;
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
		_memoryCache.Remove(STARTUP_METADATA_CACHE_KEY);
		_logger.LogInformation("Invalidated global instance ID and metadata cache");
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

		// Clear the startup instance cache and metadata cache specifically
		_memoryCache.Remove(STARTUP_INSTANCE_CACHE_KEY);
		_memoryCache.Remove(STARTUP_METADATA_CACHE_KEY);

		// If we need to track all cache keys for bulk removal, we would need to implement
		// a custom cache wrapper that maintains a list of keys
	}

	/// <summary>
	/// Fetches the instance ID from the Jiro API using the provided API key (interface compatibility).
	/// </summary>
	/// <param name="apiKey">The API key to use for authentication.</param>
	/// <returns>The instance ID if successful, null otherwise.</returns>
	public async Task<string?> FetchInstanceIdFromApiAsync(string apiKey)
	{
		var metadata = await FetchInstanceMetadataFromApiAsync(apiKey);
		return metadata?.Data?.Id;
	}

	/// <summary>
	/// Fetches the instance metadata from the Jiro API using the provided API key.
	/// </summary>
	/// <param name="apiKey">The API key to use for authentication.</param>
	/// <returns>The instance metadata response if successful, null otherwise.</returns>
	public async Task<InstanceMetadataResponse?> FetchInstanceMetadataFromApiAsync(string apiKey)
	{
		if (string.IsNullOrWhiteSpace(apiKey))
		{
			_logger.LogWarning("FetchInstanceMetadataFromApiAsync called with null or empty apiKey");
			return null;
		}

		try
		{
			using var httpClient = _httpClientFactory.CreateClient(HttpClients.JIRO);

			var requestUri = $"api/instance/GetInstanceMetadata?apiKey={Uri.EscapeDataString(apiKey)}";
			_logger.LogDebug("Fetching instance metadata from API endpoint: {RequestUri}", requestUri);

			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};

			var response = await httpClient.GetFromJsonAsync<InstanceMetadataResponse>(requestUri, jsonOptions);

			if (response?.Data != null && !string.IsNullOrWhiteSpace(response.Data.Id))
			{
				_logger.LogInformation("Successfully fetched instance metadata from API: {InstanceId} - {Name}", response.Data.Id, response.Data.Name);
				return response;
			}
			else
			{
				_logger.LogWarning("API returned null response or empty instance ID");
				return null;
			}
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "HTTP request failed while fetching instance metadata from API");
			return null;
		}
		catch (TaskCanceledException ex)
		{
			_logger.LogError(ex, "Request timeout while fetching instance metadata from API");
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Unexpected error while fetching instance metadata from API");
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
			var metadata = await FetchInstanceMetadataFromApiAsync(apiKey);

			if (metadata?.Data != null && !string.IsNullOrWhiteSpace(metadata.Data.Id))
			{
				var instanceId = metadata.Data.Id;
				// Cache both the instance ID and full metadata for the application lifetime
				_memoryCache.Set(STARTUP_INSTANCE_CACHE_KEY, instanceId, TimeSpan.FromDays(1));
				_memoryCache.Set(STARTUP_METADATA_CACHE_KEY, metadata, TimeSpan.FromDays(1));
				_logger.LogInformation("Instance metadata cached successfully for startup: {InstanceId} - {Name}", instanceId, metadata.Data.Name);
				return instanceId;
			}
			else
			{
				_logger.LogError("Failed to initialize instance ID - API returned null or empty metadata");
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
