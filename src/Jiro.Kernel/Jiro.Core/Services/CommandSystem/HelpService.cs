using System.Text;

namespace Jiro.Core.Services.CommandSystem;

public class HelpService : IHelpService
{
    public string HelpMessage
    {
        get; private set;
    }
    private readonly CommandsContext _commandsContainer;

    public HelpService(CommandsContext commandsContainer)
    {
        HelpMessage = "";
        _commandsContainer = commandsContainer;
        CreateHelpMessage();
    }

    public void CreateHelpMessage()
    {
        var commands = _commandsContainer.Commands;
        var modules = _commandsContainer.CommandModules.Select(e => e.Value);

        StringBuilder messageBuilder = new();

        foreach (var module in modules)
        {
            if (module.Commands.Keys.Count == 0)
                continue;

            messageBuilder.AppendLine($"## {module.Name}");
            foreach (var command in module.Commands)
            {
                string header;
                string? description = null;
                string? syntax = null;

                var parameters = command.Value.Parameters.Select(e => e?.ParamType.Name);
                string parametersString = parameters.Any() ? $"<span style=\"color: DeepPink;\">[ {string.Join(", ", parameters)} ]</span>" : string.Empty;

                header = $"- {command.Key} {parametersString}<br />";

                if (!string.IsNullOrEmpty(command.Value.CommandDescription))
                    description = $"{command.Value.CommandDescription}<br />";

                if (!string.IsNullOrEmpty(command.Value.CommandSyntax))
                    syntax = $"Syntax:<span style=\"color: DeepPink;\"> ${command.Value.CommandSyntax}</span><br />";

                messageBuilder.AppendLine(header);
                if (!string.IsNullOrEmpty(command.Value.CommandDescription))
                    messageBuilder.AppendLine(description);
                if (!string.IsNullOrEmpty(command.Value.CommandSyntax))
                    messageBuilder.AppendLine(syntax);
            }

            messageBuilder.AppendLine();
        }

        HelpMessage = messageBuilder.ToString();
    }
}
