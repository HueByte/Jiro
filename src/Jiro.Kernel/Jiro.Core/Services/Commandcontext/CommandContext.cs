using Jiro.Core.Services.Context;

namespace Jiro.Core.Services.CommandContext;

public class CommandContext : InstanceContext, ICommandContext
{
	// InstanceId and SessionId are inherited from InstanceContext
	public Dictionary<string, object> Data { get; } = [];

	public void SetData(IEnumerable<KeyValuePair<string, object>> data)
	{
		foreach ((string key, object value) in data)
		{
			if (!Data.TryAdd(key, value))
				Data[key] = value;
		}
	}

	public void SetCurrentInstance(string? instanceId)
	{
		if (string.IsNullOrEmpty(instanceId))
			throw new JiroException(new ArgumentException(null, nameof(instanceId)), "Something went wrong with parsing current instance", "Try to relogin");

		InstanceId = instanceId;
	}

	public void SetSessionId(string sessionId)
	{
		// Allow empty sessionId - it will trigger new session creation
		SessionId = sessionId;
	}
}
