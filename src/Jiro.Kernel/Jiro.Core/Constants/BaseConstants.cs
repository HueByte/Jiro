namespace Jiro.Core.Constants;

/// <summary>
/// Contains environment variable names used throughout the Jiro application.
/// </summary>
public class Environment
{
	/// <summary>
	/// The environment variable name for configuring the number of messages to fetch from the cache.
	/// </summary>
	public const string MessageFetchCount = "JIRO_MESSAGE_FETCH_COUNT";
}

/// <summary>
/// Contains cache key constants used for storing and retrieving data from the cache.
/// </summary>
public class CacheKeys
{
	/// <summary>
	/// The cache key for storing the core persona message.
	/// </summary>
	public const string CorePersonaMessageKey = $"{AgentMetadata.CachePrefix}::Persona";

	/// <summary>
	/// The cache key for storing the computed persona message.
	/// </summary>
	public const string ComputedPersonaMessageKey = $"{AgentMetadata.CachePrefix}::ComputedPersona";

	/// <summary>
	/// The cache key for storing session information.
	/// </summary>
	public const string SessionsKey = $"{AgentMetadata.CachePrefix}::Sessions";

	/// <summary>
	/// The cache key for storing session data.
	/// </summary>
	public const string SessionKey = $"{AgentMetadata.CachePrefix}::Session";
}

/// <summary>
/// Contains AI model identifiers and configuration constants.
/// </summary>
public class AI
{
	/// <summary>
	/// The identifier for the GPT-3.5 Turbo model.
	/// </summary>
	public const string Gpt3Model = "gpt-3.5-turbo";

	/// <summary>
	/// The identifier for the GPT-4 Turbo model.
	/// </summary>
	public const string Gpt4Model = "gpt-4-turbo";

	/// <summary>
	/// The identifier for the GPT-4o Mini model.
	/// </summary>
	public const string Gpt4oMiniModel = "gpt-4o-mini";

	/// <summary>
	/// The identifier for the GPT-4o Turbo model.
	/// </summary>
	public const string Gpt4oModel = "gpt-4o-turbo";
}

/// <summary>
/// Contains file system path constants used by the application.
/// </summary>
public class Paths
{
	/// <summary>
	/// The base directory path for storing message files.
	/// </summary>
	public static string MessageBasePath = Path.Join(AppContext.BaseDirectory, "Messages");
}

/// <summary>
/// Contains metadata information about the Jiro agent.
/// </summary>
public class AgentMetadata
{
	/// <summary>
	/// The name of the agent.
	/// </summary>
	public const string Name = "Jiro";

	/// <summary>
	/// The prefix used for cache keys to identify Jiro-related cached data.
	/// </summary>
	public const string CachePrefix = "JIRO";
}
