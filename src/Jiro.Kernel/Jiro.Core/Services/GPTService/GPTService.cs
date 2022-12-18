using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
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
        private readonly HttpClient _client;
        public GPTService(ILogger<GPTService> logger, IConfiguration config, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _config = config;
            _client = clientFactory.CreateClient(HttpClientNames.GPT_CLIENT);
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new Exception("Prompt for GPT was empty");

            var aiContext = _config.GetSection("GPT:ContextMessage").Get<string>();

            if (string.IsNullOrEmpty(aiContext)) aiContext = "";
            if (!(prompt.EndsWith('?') || prompt.EndsWith('.'))) prompt += '.';

            aiContext += prompt + "\nJiro$";

            GPTRequest model = new()
            {
                Model = "text-davinci-003",
                MaxTokens = 300,
                Prompt = aiContext,
                Temperature = 0.7,
                Stop = _config.GetSection("GPT:Stop").Get<string>() ?? "\n",
                N = 1
            };

            var response = await _client.PostAsJsonAsync(ApiEndpoints.GPT_COMPLETIONS, model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content?.ReadFromJsonAsync<GPTResponse>()!;
                var responseText = result?.Choices?.First()?.Text!;

                _logger.LogInformation("Tokens used: {tokens}", result?.Usage?.TotalTokens);

                if (_config.GetSection("GPT:FineTune").Get<bool>())
                {
                    await FineTuneAsync(aiContext, responseText);
                }

                return responseText.Trim();
            }
            else
            {
                var failedResponse = await response.Content.ReadAsStringAsync();
                _logger.LogError("{response}", failedResponse);

                throw new Exception("GPT request wasn't successful");
            }
        }

        private static async Task<bool> FineTuneAsync(string prompt, string completion)
        {
            GPTFineTune fineTune = new()
            {
                Prompt = prompt,
                Completion = completion
            };

            var jsonAppend = JsonSerializer.Serialize(fineTune);
            await File.AppendAllTextAsync(AppContext.BaseDirectory + "Model.jsonl", $"{jsonAppend}\n");

            return true;
        }
    }
}