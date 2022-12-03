namespace Jiro.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandContainerAttribute : Attribute
    {
        public string ContainerName { get; }

        public CommandContainerAttribute(string containerName)
        {
            ContainerName = containerName;
        }
    }
}