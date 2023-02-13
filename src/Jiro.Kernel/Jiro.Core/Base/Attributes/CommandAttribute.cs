using System.Reflection.Metadata;

namespace Jiro.Core.Base.Attributes
{
    /// <summary>
    /// Applied to a method within CommandModule class creates a command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; }
        public CommandType CommandType { get; }

        public CommandAttribute(string commandName, CommandType commandType = CommandType.Text)
        {
            CommandName = commandName;
            CommandType = commandType;
        }
    }
}