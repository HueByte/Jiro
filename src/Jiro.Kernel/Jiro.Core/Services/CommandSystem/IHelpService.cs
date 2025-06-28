namespace Jiro.Core.Services.CommandSystem;

public interface IHelpService
{
	string HelpMessage { get; }

	void CreateHelpMessage();
}
