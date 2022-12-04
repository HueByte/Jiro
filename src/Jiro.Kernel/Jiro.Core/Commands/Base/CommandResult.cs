namespace Jiro.Core.Commands.Base
{
    public class CommandResult : ICommandResult
    {
        public object? Data { get; set; }

        public CommandResult Create(object? data)
        {
            Data = data;
            return this;
        }
    }
}