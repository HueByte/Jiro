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

        [Command("help", commandDescription: "Shows all available commands and their syntax")]
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
                    var parameters = command.Value.Parameters.Select(e => e.ParamType.Name);
                    string parametersString = parameters.Any() ? $"<span style=\"color: DeepPink;\">[ {string.Join(", ", parameters)} ]</span><br />" : string.Empty;

                    messageBuilder.AppendLine($"- {command.Key} {parametersString}");
                    if (!string.IsNullOrEmpty(command.Value.CommandDescription)) messageBuilder.AppendLine($"{command.Value.CommandDescription}<br />");
                    if (!string.IsNullOrEmpty(command.Value.CommandSyntax)) messageBuilder.AppendLine($"Syntax:<span style=\"color: DeepPink;\"> {command.Value.CommandSyntax}</span><br />");
                }

                messageBuilder.AppendLine();
            }

            return TextResult.Create(messageBuilder.ToString());
        }
    }
}