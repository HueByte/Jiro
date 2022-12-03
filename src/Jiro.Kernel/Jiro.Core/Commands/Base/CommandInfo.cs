using System.Reflection;
using Jiro.Core.Entities;

namespace Jiro.Core.Commands.Base
{
    public class CommandInfo
    {
        public string Name { get; } = string.Empty;
        public bool IsAsync { get; } = false;
        public Type Container { get; } = default!;
        public MethodInfo Action { get; }

        public CommandInfo(string name, bool isAsync, Type container, MethodInfo action)
        {
            Name = name;
            IsAsync = isAsync;
            Container = container;
            Action = action;
        }
    }
}