using Jiro.Core.Services.GPTService.Models.ChatGPT;

namespace Jiro.Core.Services.GPTService
{
    public class TokenizeReduceRequest
    {
        public List<ChatMessage> Messages { get; set; } = null!;
    }
}