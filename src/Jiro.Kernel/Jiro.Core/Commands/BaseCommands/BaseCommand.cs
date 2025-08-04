using Jiro.Core.Services.CommandSystem;

namespace Jiro.Core.Commands.BaseCommands;

/// <summary>
/// Base command module that provides fundamental commands available in all Jiro instances.
/// </summary>
[CommandModule("BaseCommands")]
public class BaseCommand : ICommandBase
{
	/// <summary>
	/// The help service used to generate command documentation.
	/// </summary>
	private readonly IHelpService _helpService;

	/// <summary>
	/// Initializes a new instance of the BaseCommand class.
	/// </summary>
	/// <param name="helpService">The help service for generating command documentation.</param>
	public BaseCommand(IHelpService helpService)
	{
		_helpService = helpService;
	}

	/// <summary>
	/// Displays help information for all available commands and their syntax.
	/// </summary>
	/// <returns>A task representing the asynchronous operation that returns command help information.</returns>
	[Command("help", commandDescription: "Shows all available commands and their syntax")]
	public Task<ICommandResult> Help()
	{
		var result = TextResult.Create(_helpService.HelpMessage);
		return Task.FromResult(result as ICommandResult);
	}
}
