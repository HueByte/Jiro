using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.System.Models;

namespace Jiro.App.Models;

/// <summary>
/// Represents the response for commands metadata.
/// </summary>
public class CommandsMetadataResponse : SyncResponse
{
	/// <summary>
	/// Gets or sets the list of command metadata.
	/// </summary>
	public List<CommandMetadata> Commands { get; set; } = new();
}
