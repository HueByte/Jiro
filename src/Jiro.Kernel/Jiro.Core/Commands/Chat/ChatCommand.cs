using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;

namespace Jiro.Core.Commands.Chat;

[CommandModule("Chat")]
public class ChatCommand : ICommandBase
{
	private readonly IPersonalizedConversationService _chatService;
	private readonly ICommandContext _commandContext;
	private readonly IMessageManager _messageManager;
	private readonly IInstanceMetadataAccessor _instanceMetadataAccessor;

	public ChatCommand(IPersonalizedConversationService chatService, ICommandContext commandContext, IMessageManager messageManager, IInstanceMetadataAccessor instanceMetadataAccessor)
	{
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager), "Chat storage service cannot be null.");
		_chatService = chatService ?? throw new ArgumentNullException(nameof(chatService), "Chat service cannot be null.");
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
		_instanceMetadataAccessor = instanceMetadataAccessor ?? throw new ArgumentNullException(nameof(instanceMetadataAccessor), "Instance metadata accessor cannot be null.");
	}

	[Command("chat")]
	public async Task<ICommandResult> Chat(string prompt)
	{
		var sessionId = _commandContext.SessionId;
		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException("Session not found");

		var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? _instanceMetadataAccessor.GetCurrentInstanceId() ?? "";
		var result = await _chatService.ChatAsync(instanceId, sessionId, prompt);

		return TextResult.Create(result);
	}

	/// <summary>
	/// Clears the current session by removing the session ID from the command context.
	/// </summary>
	/// <returns>A completed task.</returns>
	[Command("reset", commandDescription: "Clears the current session")]
	public Task ClearSession()
	{
		_commandContext.Data.TryGetValue("sessionId", out var chatSessionId);

		var sessionId = chatSessionId as string;
		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException("Session not found");

		_commandContext.Data.Remove("sessionId");

		return Task.CompletedTask;
	}
}
