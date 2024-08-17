using Jiro.Core.Options;
using Microsoft.Extensions.Caching.Memory;
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
        var memorySession = await _chatStorageService.GetSessionAsync(sessionId);
        if (memorySession is null)
        {
            throw new JiroException("Session not found");
        }

        OpenAI.Chat.Message newUserMessage = new OpenAI.Chat.Message(Role.User, prompt);
        memorySession.Messages.Add(newUserMessage);

        var chatRequest = new ChatRequest(memorySession.Messages, Model.GPT4o, maxTokens: 2000);
        var response = await _aiClient.ChatEndpoint.GetCompletionAsync(chatRequest);
        var aiMessage = response.FirstChoice;

        OpenAI.Chat.Message aiResponse = new OpenAI.Chat.Message(Role.Assistant, aiMessage.Message);
        memorySession.Messages.Add(aiResponse);

        await _chatStorageService.AppendMessagesAsync(sessionId, [newUserMessage, aiResponse]);

        return aiResponse;
    }

    public Task<string> ChatAsync(string prompt)
    {
        throw new JiroException("This functionality is no longer supported");
    }
}