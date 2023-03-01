namespace Jiro.Core.Interfaces.IServices
{
    public interface IHelpService
    {
        string HelpMessage { get; }
        void CreateHelpMessage();
    }
}