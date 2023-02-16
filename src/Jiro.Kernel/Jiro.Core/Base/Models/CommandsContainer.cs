namespace Jiro.Core.Base.Models;

/// <summary>
/// Global container for all commands and modules
/// </summary>
public class CommandsContainer
{
    public string DefaultCommand { get; private set; } = string.Empty;
    public Dictionary<string, CommandModuleInfo> CommandModules { get; private set; } = new();
    public Dictionary<string, CommandInfo> Commands { get; private set; } = new();

    public void SetDefaultCommand(string defaultCommand) => DefaultCommand = defaultCommand;

    public void AddCommands(List<CommandInfo> commands)
    {
        foreach (var command in commands)
        {
            Commands.TryAdd(command.Name, command);
        }
    }

    public void AddModules(List<CommandModuleInfo> commandModules)
    {
        foreach (var commandModule in commandModules)
        {
            CommandModules.TryAdd(commandModule.Name, commandModule);
        }
    }
}
