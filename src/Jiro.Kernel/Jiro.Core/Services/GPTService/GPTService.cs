using System.Net.Http.Json;
using System.Text.Json;
using Jiro.Core.Constants;
using Jiro.Core.Options;
using Jiro.Core.Services.GPTService.Models;
using Jiro.Core.Services.GPTService.Models.GPT;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jiro.Core.Services.GPTService
{
    public class GPTService : IChatService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _client;
        private readonly GptOptions _gptOptions;
        public GPTService(ILogger<GPTService> logger, IHttpClientFactory clientFactory, IOptions<GptOptions> options)
        {
            _logger = logger;
            _client = clientFactory.CreateClient(HttpClients.GPT_CLIENT);
            _gptOptions = options.Value;
        }

        public async Task<string> ChatAsync(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
                throw new Exception("Prompt for GPT was empty");

            var aiContext = _gptOptions.SingleGpt?.ContextMessage;

            if (string.IsNullOrEmpty(aiContext)) aiContext = "";
            if (!(prompt.EndsWith('?') || prompt.EndsWith('.'))) prompt += '.';

            aiContext += prompt + "\nJiro$ ";

            GPTRequest req = new()
            {
                Model = _gptOptions.SingleGpt?.Model ?? "text-davinci-003",
                MaxTokens = _gptOptions.SingleGpt is not null ? _gptOptions.SingleGpt.TokenLimit : 500,
                Prompt = aiContext,
                Temperature = 0.7,
                Stop = _gptOptions.SingleGpt?.Stop ?? "\n",
                N = 1
            };

            var response = await _client.PostAsJsonAsync(ApiEndpoints.GPT_COMPLETIONS, req);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content?.ReadFromJsonAsync<GPTResponse>()!;
                var responseText = result?.Choices?.First()?.Text!;

                _logger.LogInformation("Tokens used: {tokens}", result?.Usage?.TotalTokens);

                if (_gptOptions.FineTune)
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