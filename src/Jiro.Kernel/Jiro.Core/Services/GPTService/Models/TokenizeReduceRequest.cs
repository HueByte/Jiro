using Jiro.Core.Services.GPTService.Models.ChatGPT;

namespace Jiro.Core.Services.GPTService.Models
{
    public class TokenizeReduceRequest
    {
        public List<ChatMessage> Messages { get; set; } = null!;
    }
}