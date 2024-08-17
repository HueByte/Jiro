namespace Jiro.Core.Commands.GPT;

[CommandModule("GPT")]
[Obsolete]
public class GPTCommand : ICommandBase
{
    private readonly IChatService _chatService;
    private readonly IChatGPTStorageService _storageService;
    private readonly ICommandContext _currentUserService;
    public GPTCommand(IChatService chatService, IChatGPTStorageService storageService, ICommandContext currentUserService)
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