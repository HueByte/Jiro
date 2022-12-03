namespace Jiro.Core.Commands.Base
{
    public class CommandModule
    {
        public string DefaultCommand { get; private set; } = string.Empty;
        public Dictionary<string, CommandInfo> Commands { get; private set; } = new();

        public void SetDefaultCommand(string defaultCommand) => DefaultCommand = defaultCommand;
        public void AddCommands(List<CommandInfo> commands)
        {
            foreach (var command in commands)
            {
                Commands.TryAdd(command.Name, command);
            }
        }
    }
}