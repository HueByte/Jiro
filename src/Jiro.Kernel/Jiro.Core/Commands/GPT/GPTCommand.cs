using System.Collections.ObjectModel;
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
        private readonly IChatGPTStorageService _storageService;
        public GPTCommand(IChatService gptService, IChatGPTStorageService storageService)
        {
            _gptService = gptService;
            _storageService = storageService;
        }

        [Command("chat")]
        public async Task<ICommandResult> Chat(string prompt)
        {
            var result = await _gptService.ChatAsync(prompt);

            return TextResult.Create(result);
        }

        [Command("reset", commandDescription: "Clears the current session")]
        public Task ClearSession()
        {
            string tempUser = "tempUser";
            _storageService.RemoveSession(tempUser);

            return Task.CompletedTask;
        }
    }
}