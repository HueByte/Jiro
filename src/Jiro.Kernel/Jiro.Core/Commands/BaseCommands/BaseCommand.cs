using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;

namespace Jiro.Core.Commands.BaseCommands
{
    [CommandModule("BaseCommands")]
    public class BaseCommand : CommandBase
    {
        public BaseCommand()
        {

        }

        [Command("help")]
        public async Task<ICommandResult> Help()
        {
            return CommandResult.Create("Help");
        }
    }
}