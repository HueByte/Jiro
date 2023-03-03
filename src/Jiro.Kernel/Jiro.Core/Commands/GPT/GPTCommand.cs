using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Results;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Commands.GPT
{
    [CommandModule("GPT")]
    public class GPTCommand : ICommandBase
    {
        private readonly IChatService _gptService;
        public GPTCommand(IChatService gptService)
        {
            _gptService = gptService;
        }

        [Command("chat")]
        public async Task<ICommandResult> Chat(string prompt)
        {
            var result = await _gptService.ChatAsync(prompt);

            return TextResult.Create(result);
        }
    }
}