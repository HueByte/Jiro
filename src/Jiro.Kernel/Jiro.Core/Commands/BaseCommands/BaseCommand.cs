using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;

namespace Jiro.Core.Commands.BaseCommands
{
    [CommandModule("BaseCommands")]
    public class BaseCommand : ICommandBase
    {
        private readonly CommandsContainer _commandsContainer;
        public BaseCommand(CommandsContainer commandsContainer)
        {
            _commandsContainer = commandsContainer;
        }

        [Command("help")]
        public async Task<ICommandResult> Help()
        {
            var commands = _commandsContainer.Commands
                .Select(cmd => cmd.Key)
                .ToList();

            var helpMessage = $"Commands avaliable: {string.Join(", ", commands)}";

            return CommandResult.Create(helpMessage);
        }
    }
}