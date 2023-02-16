using System.Text;
using Jiro.Core.Base;
using Jiro.Core.Base.Attributes;
using Jiro.Core.Base.Models;
using Jiro.Core.Base.Results;

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
            var commands = _commandsContainer.Commands;
            var modules = _commandsContainer.CommandModules.Select(e => e.Value);

            StringBuilder messageBuilder = new();

            foreach (var module in modules)
            {
                if (module.Commands.Keys.Count == 0) continue;

                messageBuilder.AppendLine($"## {module.Name}");
                foreach (var command in module.Commands)
                {
                    var parameters = command.Value.Parameters.Select(e => e.Type.Name);
                    string parametersString = parameters.Any() ? $"[ {string.Join(", ", parameters)} ]" : string.Empty;

                    messageBuilder.AppendLine($"- {command.Key} {parametersString}");
                }

                messageBuilder.AppendLine();
            }

            return TextResult.Create(messageBuilder.ToString());
        }
    }
}