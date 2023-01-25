namespace Jiro.Core.Base.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandModuleAttribute : Attribute
    {
        public string ModuleName { get; }

        public CommandModuleAttribute(string moduleName)
        {
            ModuleName = moduleName;
        }
    }
}