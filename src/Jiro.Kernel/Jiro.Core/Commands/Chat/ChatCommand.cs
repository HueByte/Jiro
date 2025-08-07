using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.Conversation;
using Jiro.Core.Services.MessageCache;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Commands.Chat;

/// <summary>
/// Command module that provides chat functionality using AI conversation services.
/// </summary>
[CommandModule("Chat")]
public class ChatCommand : ICommandBase
{
	/// <summary>
	/// The personalized conversation service for AI chat interactions.
	/// </summary>
	private readonly IPersonalizedConversationService _chatService;

	/// <summary>
	/// The command context for accessing session information.
	/// </summary>
	private readonly ICommandContext _commandContext;

	/// <summary>
	/// The message manager for handling chat messages.
	/// </summary>
	private readonly IMessageManager _messageManager;

	/// <summary>
	/// The instance metadata accessor for retrieving instance information.
	/// </summary>
	private readonly IInstanceMetadataAccessor _instanceMetadataAccessor;

	/// <summary>
	/// The logger for this command.
	/// </summary>
	private readonly ILogger<ChatCommand> _logger;

	/// <summary>
	/// Initializes a new instance of the ChatCommand class.
	/// </summary>
	/// <param name="chatService">The personalized conversation service.</param>
	/// <param name="commandContext">The command context.</param>
	/// <param name="messageManager">The message manager.</param>
	/// <param name="instanceMetadataAccessor">The instance metadata accessor.</param>
	/// <param name="logger">The logger for this command.</param>
	/// <exception cref="ArgumentNullException">Thrown when any of the required parameters is null.</exception>
	public ChatCommand(IPersonalizedConversationService chatService, ICommandContext commandContext, IMessageManager messageManager, IInstanceMetadataAccessor instanceMetadataAccessor, ILogger<ChatCommand> logger)
	{
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager), "Chat storage service cannot be null.");
		_chatService = chatService ?? throw new ArgumentNullException(nameof(chatService), "Chat service cannot be null.");
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext), "Command context cannot be null.");
		_instanceMetadataAccessor = instanceMetadataAccessor ?? throw new ArgumentNullException(nameof(instanceMetadataAccessor), "Instance metadata accessor cannot be null.");
		_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
	}

	/// <summary>
	/// Processes a chat prompt using the AI conversation service and returns the response.
	/// </summary>
	/// <param name="prompt">The user's chat prompt or message.</param>
	/// <returns>A task representing the asynchronous operation that returns the AI's response.</returns>
	/// <exception cref="JiroException">Thrown when the session is not found.</exception>
	[Command("chat")]
	public async Task<ICommandResult> Chat(string prompt)
	{
		var sessionId = _commandContext.SessionId;
		_logger.LogInformation("Chat command started with SessionId: '{SessionId}' (IsEmpty: {IsEmpty})",
			sessionId ?? "null", string.IsNullOrEmpty(sessionId));

		// Session ID is now guaranteed to be available from CommandHandler
		if (string.IsNullOrEmpty(sessionId))
		{
			throw new JiroException("Session ID should have been created by CommandHandler but is still empty");
		}

		var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? _instanceMetadataAccessor.GetCurrentInstanceId() ?? "";
		var result = await _chatService.ChatAsync(instanceId, sessionId, prompt);

		_logger.LogInformation("Chat completed for SessionId: '{SessionId}'", sessionId);

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
