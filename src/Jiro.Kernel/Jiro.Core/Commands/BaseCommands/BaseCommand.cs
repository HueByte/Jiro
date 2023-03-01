using System.Text;
using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Models;
using Jiro.Core.Base.Results;
using Jiro.Core.Interfaces.IServices;

namespace Jiro.Core.Commands.BaseCommands
{
    [CommandModule("BaseCommands")]
    public class BaseCommand : ICommandBase
    {
        private readonly CommandsContainer _commandsContainer;
        private readonly IHelpService _helpService;
        public BaseCommand(CommandsContainer commandsContainer, IHelpService helpService)
        {
            _commandsContainer = commandsContainer;
            _helpService = helpService;
        }

        [Command("help", commandDescription: "Shows all available commands and their syntax")]
        public async Task<ICommandResult> Help()
        {
            return TextResult.Create(_helpService.HelpMessage);
        }
    }
}