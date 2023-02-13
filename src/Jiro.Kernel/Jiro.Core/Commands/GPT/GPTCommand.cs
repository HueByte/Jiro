using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Commands.GPT
{
    [CommandModule("GPT")]
    public class GPTCommand : ICommandBase
    {
        private readonly IGPTService _gptService;
        public GPTCommand(IGPTService gptService)
        {
            _gptService = gptService;
        }

        [Command("chat")]
        public async Task<ICommandResult> Chat(string prompt)
        {
            var result = await _gptService.ChatAsync(prompt);

            return CommandResult.Create(result);
        }
    }
}