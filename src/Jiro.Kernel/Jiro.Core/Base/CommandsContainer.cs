using Jiro.Core.Entities;

namespace Jiro.Core.Base;

public class CommandsContainer
{
    public string DefaultCommand { get; private set; } = string.Empty;
    public Dictionary<string, CommandModule> CommandModules { get; private set; } = new();
    public Dictionary<string, CommandInfo> Commands { get; private set; } = new();

    public void SetDefaultCommand(string defaultCommand) => DefaultCommand = defaultCommand;

    public void AddCommands(List<CommandInfo> commands)
    {
        foreach (var command in commands)
        {
            Commands.TryAdd(command.Name, command);
        }
    }

    public void AddModules(List<CommandModule> commandModules)
    {
        foreach (var commandModule in commandModules)
        {
            CommandModules.TryAdd(commandModule.Name, commandModule);
        }
    }
}
