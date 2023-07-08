using System.Net.Http.Json;
using Jiro.Commands.Exceptions;
using Jiro.Core.Constants;
using Jiro.Core.Options;
using Jiro.Core.Services.GPTService.Models.ChatGPT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.GPTService;

public class ChatGPTService : IChatService
{
    private readonly ILogger _logger;
    private readonly ChatGptOptions _chatGptOptions;
    private readonly HttpClient _client;
    private readonly IChatGPTStorageService _storageService;
    private readonly ITokenizerService _tokenizerService;
    private readonly ICurrentUserService _currentUserService;
    public ChatGPTService(ILogger<ChatGPTService> logger, IChatGPTStorageService storageService, IHttpClientFactory clientFactory, IOptions<ChatGptOptions> chatGptOptions, ITokenizerService tokenizerService, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _storageService = storageService;
        _client = clientFactory.CreateClient(HttpClients.CHAT_GPT_CLIENT);
        _chatGptOptions = chatGptOptions.Value;
        _tokenizerService = tokenizerService;
        _currentUserService = currentUserService;
    }

    public async Task<string> ChatAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new Exception("The prompt cannot be empty");

        if (string.IsNullOrEmpty(_currentUserService.UserId))
            throw new Exception("You must be logged in to use this command");

        string userId = _currentUserService.UserId;

        var session = _storageService.GetOrCreateSession(userId);

        ChatMessage message = new()
        {
            Role = "user",
            Content = prompt
        };

        session.Request.Messages.Add(message);

        ChatGPTResponse? body;
        try
        {
            await ReduceTokenCount(session);

            var result = await _client.PostAsJsonAsync(ApiEndpoints.CHAT_GPT_COMPLETIONS, session.Request);

            if (result.IsSuccessStatusCode)
            {
                body = await result.Content.ReadFromJsonAsync<ChatGPTResponse>();

                if (body is null) throw new CommandException("ChatGPT", "The interaction failed");
            }
            else
            {
                var errMessage = await result.Content.ReadAsStringAsync();
                _logger.LogError("Open AI: \n{err}", errMessage);

                throw new CommandException("ChatGPT", "The interaction failed");
            }
        }
        catch (Exception)
        {
            session.Request.Messages.RemoveAt(session.Request.Messages.Count - 1);
            throw;
        }

        _logger.LogInformation("[ChatGPT] Tokens consumed: {tokens}", body.Usage.TotalTokens);

        ChatMessage responseMessage = new()
        {
            Role = body.Choices[0].Message.Role,
            Content = body.Choices[0].Message.Content
        };

        session.Request.Messages.Add(responseMessage);

        return responseMessage.Content;
    }

    private async Task ReduceTokenCount(ChatGPTSession session)
    {
        session.Request.Messages = await _tokenizerService.ReduceTokenCountAsync(session.Request.Messages);
    }

    private async Task<int> GetTokenCount(List<ChatMessage> messages)
    {
        var combinedText = string.Join(' ', messages.Select(e => e.Content));

        return await _tokenizerService.GetTokenCountAsync(combinedText);
    }
}