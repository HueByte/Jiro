using Jiro.Core.Options;
using Jiro.Core.Utils;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

namespace Jiro.Core.Services.Chat;

public class ChatService : IChatService
{
    private readonly OpenAIClient _aiClient;
    private readonly IChatStorageService _chatStorageService;
    private readonly ChatOptions _chatOptions;

    public ChatService(OpenAIClient aiClient, IChatStorageService chatStorageService, IOptions<ChatOptions> chatOptions)
    {
        _aiClient = aiClient;
        _chatStorageService = chatStorageService;
        _chatOptions = chatOptions.Value;
    }

    public async Task<OpenAI.Chat.Message> ChatAsync(string prompt, string sessionId)
    {
        var session = await _chatStorageService.GetSessionAsync(sessionId);
        if (session is null)
        {
            throw new JiroException("Session not found");
        }

        OpenAI.Chat.Message messageRequest = new OpenAI.Chat.Message(Role.User, prompt);

        var sessionMessages = session.Messages.Select(message => new OpenAI.Chat.Message(AppUtils.GetRole(message.Role), message.Content));
        sessionMessages.Append(messageRequest);

        var chatRequest = new ChatRequest(sessionMessages, Model.GPT4o, maxTokens: _chatOptions.TokenLimit);
        var response = await _aiClient.ChatEndpoint.GetCompletionAsync(chatRequest);
        var aiMessage = response.FirstChoice;


        await AppendMessagesToSessionAsync(sessionId, [messageRequest, aiMessage.Message]);
        return new Message(Role.Assistant, aiMessage.Message);
    }

    private async Task AppendMessagesToSessionAsync(string sessionId, List<Message> messages)
    {
        List<Core.Models.Message> newMessages = new();
        foreach (var message in messages)
        {
            newMessages.Add(new Core.Models.Message()
            {
                Role = message.Role.ToString(),
                Content = message.Content,
                ChatSessionId = sessionId
            });
        }

        await _chatStorageService.AppendMessagesToSessionAsync(sessionId, newMessages);
    }

    public async Task<string?> CreateChatSessionAsync(string userId)
    {
        var session = await _chatStorageService.CreateSessionAsync(userId);
        return session.Id;
    }

    public Task<Core.Models.ChatSession?> GetSessionAsync(string sessionId)
    {
        return _chatStorageService.GetSessionAsync(sessionId);
    }
}