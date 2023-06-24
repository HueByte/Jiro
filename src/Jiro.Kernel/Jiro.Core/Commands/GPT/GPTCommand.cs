namespace Jiro.Core.Commands.GPT
{
    [CommandModule("GPT")]
    public class GPTCommand : ICommandBase
    {
        private readonly IChatService _chatService;
        private readonly IChatGPTStorageService _storageService;
        private readonly ICurrentUserService _currentUserService;
        public GPTCommand(IChatService chatService, IChatGPTStorageService storageService, ICurrentUserService currentUserService)
        {
            _chatService = chatService;
            _storageService = storageService;
            _currentUserService = currentUserService;
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
            _storageService.RemoveSession(_currentUserService.UserId!);

            return Task.CompletedTask;
        }
    }
}