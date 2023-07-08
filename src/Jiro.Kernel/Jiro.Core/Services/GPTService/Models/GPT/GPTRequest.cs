using System.Text.Json.Serialization;

namespace Jiro.Core.Services.GPTService.Models.GPT;

public class GPTRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public int TopP { get; set; }

    [JsonPropertyName("n")]
    public int N { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }

    [JsonPropertyName("logprobs")]
    public object Logprobs { get; set; } = null!;

    [JsonPropertyName("stop")]
    public string Stop { get; set; } = string.Empty;
}