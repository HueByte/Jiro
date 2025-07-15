namespace Jiro.Core.Services.CommandSystem;

/// <summary>
/// Provides functionality to generate and manage help messages for commands and modules.
/// </summary>
public interface IHelpService
{
	/// <summary>
	/// Gets the generated help message.
	/// </summary>
	string HelpMessage { get; }

	/// <summary>
	/// Creates the help message by iterating through commands and modules.
	/// </summary>
	void CreateHelpMessage();

	/// <summary>
	/// Gets the metadata for commands.
	/// </summary>
	List<CommandMetadata> CommandMeta { get; }
}
