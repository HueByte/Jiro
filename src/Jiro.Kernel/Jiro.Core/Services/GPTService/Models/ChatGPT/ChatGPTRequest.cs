using System.Text.Json.Serialization;

namespace Jiro.Core.Services.GPTService.Models.ChatGPT
{
    public class ChatGPTRequest
    {
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 64;
        public string Model { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = null!;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}