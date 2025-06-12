namespace Jiro.Core.Constants;

public class Environment
{
	public const string MessageFetchCount = "JIRO_MESSAGE_FETCH_COUNT";
}

public class CacheKeys
{
	public const string CorePersonaMessageKey = "Persona";
	public const string ComputedPersonaMessageKey = "ComputedPersona";
	public const string ServerTogglesCreated = "ServerTogglesCreated";
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
	public static string TTSBasePath = Path.Join(AppContext.BaseDirectory, "TTS");
	public static string TTSBaseOutputPath = Path.Join(TTSBasePath, "output");
}

public class AgentMetadata
{
	public const string Name = "Jiro";
}
