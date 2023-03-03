using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Results;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Commands.GPT
{
    [CommandModule("GPT")]
    public class GPTCommand : ICommandBase
    {
        private readonly IChatService _chatService;
        private readonly IChatGPTStorageService _storageService;
        public GPTCommand(IChatService chatService, IChatGPTStorageService storageService)
        {
            _chatService = chatService;
            _storageService = storageService;
        }

        [Command("chat")]
        public async Task<ICommandResult> Chat(string prompt)
        {
            var result = await _chatService.ChatAsync(prompt);

            return TextResult.Create(result);
        }

        [Command("reset", commandDescription: "Clears the current session")]
        public Task ClearSession()
        {
            string tempUser = "tempUser";
            _storageService.RemoveSession(tempUser);

            return Task.CompletedTask;
        }

        [Command("loading-test")]
        public async Task LoadingTest()
        {
            await Task.Delay(10000);
        }
    }
}