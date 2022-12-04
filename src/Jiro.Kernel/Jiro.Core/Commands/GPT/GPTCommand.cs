using Jiro.Core.Attributes;
using Jiro.Core.Commands.Base;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Commands.GPT
{
    [CommandContainer("GPT")]
    public class GPTCommand : CommandBase
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
            return new CommandResult().Create(result);
        }
    }
}