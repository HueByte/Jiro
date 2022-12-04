namespace Jiro.Core.Commands.Base
{
    public class CommandInfo
    {
        public string Name { get; } = string.Empty;
        public bool IsAsync { get; } = false;
        public Type Container { get; } = default!;
        public Func<CommandBase, object[], Task> Descriptor = default!;

        public CommandInfo(string name, bool isAsync, Type container, Func<CommandBase, object[], Task> descriptor)
        {
            Name = name;
            IsAsync = isAsync;
            Container = container;
            Descriptor = descriptor;
        }
    }
}