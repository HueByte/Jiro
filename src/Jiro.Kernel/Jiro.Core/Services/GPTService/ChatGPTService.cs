using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Options;
using Jiro.Core.Services.GPTService.Models.ChatGPT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.GPTService
{
    public class ChatGPTService : IChatService
    {
        private readonly ILogger _logger;
        private readonly GptOptions _options;
        private readonly ChatGptOptions _chatGptOptions;
        private readonly HttpClient _client;
        private readonly IChatGPTStorageService _storageService;
        public ChatGPTService(ILogger<ChatGPTService> logger, IChatGPTStorageService storageService, IHttpClientFactory clientFactory, IOptions<GptOptions> options, IOptions<ChatGptOptions> chatGptOptions)
        {
            _logger = logger;
            _storageService = storageService;
            _client = clientFactory.CreateClient(HttpClients.CHAT_GPT_CLIENT);
            _options = options.Value;
            _chatGptOptions = chatGptOptions.Value;
        }

        public async Task<string> ChatAsync(string prompt)
        {
            string userId = "tempUser";

            var session = _storageService.GetOrCreateSession(userId);

            ChatMessage message = new()
            {
                Role = "user",
                Content = prompt
            };

            session.Request.Messages.Add(message);

            var result = await _client.PostAsJsonAsync(ApiEndpoints.CHAT_GPT_COMPLETIONS, session.Request);

            ChatGPTResponse body = null!;
            if (result.IsSuccessStatusCode)
            {
                body = await result.Content.ReadFromJsonAsync<ChatGPTResponse>();
            }
            else
            {
                var errMessage = await result.Content.ReadAsStringAsync();
                _logger.LogError("Something went wrong \n{err}", errMessage);
                throw new CommandException("ChatGPT", "Failed to get response from ChatGPT API.");
            }

            _logger.LogInformation("[ChatGPT] tokens consumed: {tokens}", body.Usage.TotalTokens);

            ChatMessage response = new()
            {
                Role = body.Choices[0].Message.Role,
                Content = body.Choices[0].Message.Content
            };

            session.Request.Messages.Add(response);

            return response.Content;
        }
    }
}