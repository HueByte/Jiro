using System.Text.Json.Serialization;

namespace Jiro.Core.Services.GPTService.Models
{
    public class GPTFineTune
    {
        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("completion")]
        public string Completion { get; set; } = string.Empty;
    }
}