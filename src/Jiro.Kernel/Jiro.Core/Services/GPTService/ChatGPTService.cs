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
        private readonly ChatGptOptions _chatGptOptions;
        private readonly HttpClient _client;
        private readonly HttpClient _tokenizerClient;
        private readonly IChatGPTStorageService _storageService;
        public ChatGPTService(ILogger<ChatGPTService> logger, IChatGPTStorageService storageService, IHttpClientFactory clientFactory, IOptions<ChatGptOptions> chatGptOptions)
        {
            _logger = logger;
            _storageService = storageService;
            _client = clientFactory.CreateClient(HttpClients.CHAT_GPT_CLIENT);
            _tokenizerClient = clientFactory.CreateClient(HttpClients.TOKENIZER);
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

            var tokens = await GetTokenCount(session.Request.Messages);
            while (tokens >= 4096)
            {
                _logger.LogInformation("Attempting to reduce token count for {user} session", userId);

                // remove user - assistant message pair
                if (session.Request.Messages.Count > 2)
                {
                    session.Request.Messages.RemoveAt(2);
                    session.Request.Messages.RemoveAt(1);
                }

                tokens = await GetTokenCount(session.Request.Messages);
            }

            ChatGPTResponse? body;
            try
            {
                var result = await _client.PostAsJsonAsync(ApiEndpoints.CHAT_GPT_COMPLETIONS, session.Request);

                if (result.IsSuccessStatusCode)
                {
                    body = await result.Content.ReadFromJsonAsync<ChatGPTResponse>();
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

        private async Task<int> GetTokenCount(List<ChatMessage> messages)
        {
            TokenizeRequest request = new(string.Join(' ', messages.Select(e => e.Content)));

            var result = await _tokenizerClient.PostAsJsonAsync("/tokenize", request);

            return Convert.ToInt32(await result.Content.ReadAsStringAsync());
        }

        private record TokenizeRequest(string Text);
    }
}