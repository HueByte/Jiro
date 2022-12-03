using System.Reflection;

namespace Jiro.Core.Entities
{
    public class Command
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAsync { get; set; } = false;
        public object? Instance { get; set; } = default;
        public MethodInfo Action { get; set; } = default!;
    }
}