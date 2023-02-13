using System.Reflection;
using Jiro.Core.Base;

namespace Jiro.Core.Entities
{
    /// <summary>
    /// Represents a command instance
    /// </summary>
    public class Command
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAsync { get; set; } = false;
        public object? Instance { get; set; } = default;
        public CommandType CommandType { get; set; }
        public Func<CommandBase, object[], Task> Descriptor = default!;
    }
}