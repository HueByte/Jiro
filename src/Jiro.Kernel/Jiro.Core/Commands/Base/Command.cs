using System.Reflection;
using Jiro.Core.Commands.Base;

namespace Jiro.Core.Entities
{
    public class Command
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAsync { get; set; } = false;
        public object? Instance { get; set; } = default;
        public Func<CommandBase, object[], Task> Descriptor = default!;
    }
}