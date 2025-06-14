namespace Jiro.Core.Constants;

public class Environment
{
	public const string MessageFetchCount = "JIRO_MESSAGE_FETCH_COUNT";
}

public class CacheKeys
{
	public const string CorePersonaMessageKey = $"{AgentMetadata.CachePrefix}Persona";
	public const string ComputedPersonaMessageKey = $"{AgentMetadata.CachePrefix}ComputedPersona";
	public const string SessionsKey = $"{AgentMetadata.CachePrefix}Sessions";
}

public class AI
{
	public const string Gpt3Model = "gpt-3.5-turbo";
	public const string Gpt4Model = "gpt-4-turbo";
	public const string Gpt4oMiniModel = "gpt-4o-mini";
	public const string Gpt4oModel = "gpt-4o-turbo";
}

public class Paths
{
	public static string MessageBasePath = Path.Join(AppContext.BaseDirectory, "Messages");
}

public class AgentMetadata
{
	public const string Name = "Jiro";
	public const string CachePrefix = "JIRO_";
}
