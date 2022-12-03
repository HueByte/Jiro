using Jiro.Core.Attributes;

namespace Jiro.Core.Commands.GPT
{
    [CommandContainer("GPT")]
    public class GPTCommand
    {
        [Command("chat")]
        public async Task<string> Chat(string prompt)
        {
            await Task.Delay(1000);
            return "Funny response";
        }
    }
}