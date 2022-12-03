using System.Net.Http.Json;
using Jiro.Core.Attributes;
using Jiro.Core.Constants;
using Jiro.Core.Interfaces.IServices;
using Jiro.Core.Services.GPTService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.GPTService
{
    public class GPTService : IGPTService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;
        public GPTService(ILogger<GPTService> logger, IConfiguration config, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _config = config;
            _clientFactory = clientFactory;
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new Exception("Prompt for GPT was empty");

            var aiContext = _config.GetSection("GPT:ContextMessage").Get<string>();

            if (string.IsNullOrEmpty(aiContext)) aiContext = "";
            if (!(prompt.EndsWith('?') || prompt.EndsWith('.'))) prompt += '.';

            aiContext += prompt + "\n[Huppy]:";

            GPTRequest model = new()
            {
                Model = "text-davinci-003",
                MaxTokens = 200,
                Prompt = aiContext,
                Temperature = 0.6,
                N = 1
            };

            var client = _clientFactory.CreateClient("GPT");

            var response = await client.PostAsJsonAsync(ApiEndpoints.TextDavinciCompletions, model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content?.ReadFromJsonAsync<GPTResponse>()!;
                return result?.Choices?.First()?.Text!;
            }
            else
            {
                var failedResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{response}", failedResponse);

                throw new Exception("GPT request wasn't successful");
            }
        }
    }
}