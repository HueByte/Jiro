namespace Jiro.Core.Base.Attributes
{
    /// <summary>
    /// Applied to a class, marks it ready to be used as a command module
    /// </summary>
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