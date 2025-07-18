using Jiro.Core.Services.CommandSystem;

namespace Jiro.Core.Commands.BaseCommands;

[CommandModule("BaseCommands")]
public class BaseCommand : ICommandBase
{
	private readonly IHelpService _helpService;
	public BaseCommand(IHelpService helpService)
	{
		_helpService = helpService;
	}

	[Command("help", commandDescription: "Shows all available commands and their syntax")]
	public Task<ICommandResult> Help()
	{
		var result = TextResult.Create(_helpService.HelpMessage);
		return Task.FromResult(result as ICommandResult);
	}
}
