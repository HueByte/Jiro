namespace Jiro.App.Models;

/// <summary>
/// Represents metadata for a command.
/// </summary>
public class CommandMetadata
{
	/// <summary>
	/// Gets or sets the name of the command.
	/// </summary>
	public string CommandName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the description of the command.
	/// </summary>
	public string CommandDescription { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the syntax of the command.
	/// </summary>
	public string CommandSyntax { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the parameters of the command.
	/// </summary>
	public Dictionary<string, Type> Parameters { get; set; } = new();

	/// <summary>
	/// Gets or sets the name of the module containing the command.
	/// </summary>
	public string ModuleName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the keywords associated with the command.
	/// </summary>
	public string[] Keywords { get; set; } = Array.Empty<string>();
}
