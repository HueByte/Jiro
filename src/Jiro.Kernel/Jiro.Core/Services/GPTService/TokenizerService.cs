using System.Net.Http.Json;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.GPTService.Models;
using Jiro.Core.Services.GPTService.Models.ChatGPT;

namespace Jiro.Core.Services.GPTService
{

    public class TokenizerService : ITokenizerService
    {
        private readonly HttpClient _client;
        public TokenizerService(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient(HttpClients.TOKENIZER);
        }

        public async Task<List<ChatMessage>> ReduceTokenCountAsync(List<ChatMessage> messages)
        {
            TokenizeReduceRequest request = new() { Messages = messages };

            var result = await _client.PostAsJsonAsync("/reduce", request);

            if (!result.IsSuccessStatusCode)
            {
                var errMessage = await result.Content.ReadAsStringAsync();
                throw new Exception(errMessage);
            }

            var resultBody = await result.Content.ReadFromJsonAsync<List<ChatMessage>>();

            if (resultBody is null)
                return messages;

            return resultBody;
        }

        public async Task<int> GetTokenCountAsync(string text)
        {
            TokenizeCountRequest request = new() { Text = text };

            var result = await _client.PostAsJsonAsync("/tokenize", request);

            return Convert.ToInt32(await result.Content.ReadAsStringAsync());
        }
    }
}