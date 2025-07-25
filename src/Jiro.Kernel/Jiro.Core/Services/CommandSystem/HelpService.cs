using System.Text;

namespace Jiro.Core.Services.CommandSystem;

/// <summary>
/// Provides functionality to generate and manage help messages for commands and modules.
/// </summary>
public class HelpService : IHelpService
{
	/// <inheritdoc/>
	public string HelpMessage
	{
		get; private set;
	}

	/// <inheritdoc/>
	public List<CommandMetadata> CommandMeta { get; } = new();

	/// <summary>
	/// The container for commands and modules.
	/// </summary>
	private readonly CommandsContext _commandsContainer;

	/// <summary>
	/// Initializes a new instance of the <see cref="HelpService"/> class.
	/// </summary>
	/// <param name="commandsContainer">The container for commands and modules.</param>
	public HelpService(CommandsContext commandsContainer)
	{
		HelpMessage = "";
		_commandsContainer = commandsContainer;
		CreateHelpMessage();
	}

	/// <inheritdoc/>
	public void CreateHelpMessage()
	{
		var commands = _commandsContainer.Commands;
		var modules = _commandsContainer.CommandModules.Select(static e => e.Value);

		StringBuilder messageBuilder = new();

		foreach (var module in modules)
		{
			if (module.Commands.Keys.Count == 0)
				continue;

			messageBuilder.AppendLine($"## {module.Name}");
			foreach (var command in module.Commands)
			{
				string header;
				string? description = null;
				string? syntax = null;

				var parameters = command.Value.Parameters.Select(static e => e?.ParamType.Name);
				string parametersString = parameters.Any() ? $"<span style=\"color: DeepPink;\">[ {string.Join(", ", parameters)} ]</span>" : string.Empty;

				header = $"- {command.Key} {parametersString}<br />";

				if (!string.IsNullOrEmpty(command.Value.CommandDescription))
					description = $"{command.Value.CommandDescription}<br />";

				if (!string.IsNullOrEmpty(command.Value.CommandSyntax))
					syntax = $"Syntax:<span style=\"color: DeepPink;\"> ${command.Value.CommandSyntax}</span><br />";

				messageBuilder.AppendLine(header);
				if (!string.IsNullOrEmpty(command.Value.CommandDescription))
					messageBuilder.AppendLine(description);
				if (!string.IsNullOrEmpty(command.Value.CommandSyntax))
					messageBuilder.AppendLine(syntax);

				CommandMetadata meta = new CommandMetadata()
				{
					CommandName = command.Key,
					CommandDescription = command.Value.CommandDescription ?? string.Empty,
					CommandSyntax = command.Value.CommandSyntax ?? string.Empty,
					ModuleName = module.Name,

					// TODO: Add proper parameter metadata for ML
					Parameters = command.Value.Parameters
						.Where(static p => p != null && p.ParamType != null)
						.Select(static (p, index) => new { Key = $"{p?.ToString() ?? string.Empty}_{index}", Value = p!.ParamType })
						.ToDictionary(static x => x.Key, static x => x.Value),

					// TODO: Implement proper keywords to commands
					Keywords = [string.Empty]
				};

				CommandMeta.Add(meta);
			}

			messageBuilder.AppendLine();
		}

		HelpMessage = messageBuilder.ToString();
	}
}
