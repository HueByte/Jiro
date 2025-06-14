using System.Text.Json;

using Jiro.Core.Commands.ComplexCommandResults;
using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;

namespace Jiro.Core.Commands.Chat;

[CommandModule("Chat")]
public class ChatCommand : ICommandBase
{
	private readonly IPersonalizedConversationService _chatService;
	private readonly ICommandContext _commandContext;
	private readonly IMessageManager _messageManager;

	public ChatCommand (IPersonalizedConversationService chatService, ICommandContext commandContext, IMessageManager messageManager)
	{
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager), "Chat storage service cannot be null.");
		_chatService = chatService ?? throw new ArgumentNullException(nameof(chatService), "Chat service cannot be null.");
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
	}

	[Command("chat")]
	public async Task<ICommandResult> Chat (string prompt)
	{
		var sessionId = _commandContext.SessionId;
		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException("Session not found");

		var result = await _chatService.ChatAsync(_commandContext.InstanceId ?? "", sessionId, prompt);

		return TextResult.Create(result);
	}

	[Command("getSessions")]
	public async Task<ICommandResult> GetSessions ()
	{
		if (_commandContext.InstanceId == null)
			throw new JiroException("User not found");

		var sessions = await _messageManager.GetChatSessionsAsync(_commandContext.InstanceId);

		return TextResult.Create(JsonSerializer.Serialize(sessions));
	}

	[Command("getSessionHistory")]
	public async Task<ICommandResult> GetSessionMessages (string sessionId)
	{
		if (_commandContext.InstanceId == null)
			throw new JiroException("User not found");

		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException("Session ID cannot be empty");

		var session = await _messageManager.GetSessionAsync(sessionId)
			?? throw new JiroException($"Session with ID {sessionId} not found");

		var data = new SessionResult(session);

		return TextResult.Create(JsonSerializer.Serialize(data));
	}

	[Command("reset", commandDescription: "Clears the current session")]
	public Task ClearSession ()
	{
		_commandContext.Data.TryGetValue("sessionId", out var chatSessionId);

		var sessionId = chatSessionId as string;
		if (string.IsNullOrEmpty(sessionId))
			throw new JiroException("Session not found");

		_commandContext.Data.Remove("sessionId");

		return Task.CompletedTask;
	}
}
