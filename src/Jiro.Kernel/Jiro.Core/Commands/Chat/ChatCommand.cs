using System.Text.Json;
using System.Web.Helpers;

namespace Jiro.Core.Commands.Chat;

[CommandModule("Chat")]
public class ChatCommand : ICommandBase
{
    private readonly IChatService _chatService;
    private readonly ICommandContext _commandContext;
    private readonly IChatStorageService _chatStorageService;

    public ChatCommand(IChatService chatService, ICommandContext commandContext, IChatStorageService chatStorageService)
    {
        _chatService = chatService;
        _commandContext = commandContext;
        _chatStorageService = chatStorageService;
    }

    [Command("init")]
    public async Task<ICommandResult> Init()
    {
        if (_commandContext.UserId == null)
            throw new JiroException("User not found");

        await _chatStorageService.CreateSessionAsync(_commandContext.UserId);

        return TextResult.Create("Session created");
    }

    [Command("chat")]
    public async Task<ICommandResult> Chat(string prompt)
    {
        _commandContext.Data.TryGetValue("sessionId", out var chatSessionId);

        var sessionId = chatSessionId as string;
        if (string.IsNullOrEmpty(sessionId))
            throw new JiroException("Session not found");

        var result = await _chatService.ChatAsync(prompt, sessionId);

        return TextResult.Create(result.Content);
    }

    [Command("getSessions")]
    public async Task<ICommandResult> GetSessions()
    {
        if (_commandContext.UserId == null)
            throw new JiroException("User not found");

        var sessions = await _chatStorageService.GetSessionIdsAsync(_commandContext.UserId);

        return TextResult.Create(JsonSerializer.Serialize(sessions));
    }

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