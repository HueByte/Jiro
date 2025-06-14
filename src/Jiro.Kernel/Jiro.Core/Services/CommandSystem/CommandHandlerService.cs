using System.Diagnostics;
using System.Text.RegularExpressions;

using Jiro.Commands.Exceptions;

using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.CommandHandler;

public partial class CommandHandlerService : ICommandHandlerService
{
	private readonly CommandsContext _commandsModule;
	private readonly ILogger _logger;
	private readonly Regex pattern = RegexCommandParserPattern();
	public event Action<string, object[]>? OnLog;
	public CommandHandlerService (CommandsContext commandModule, ILogger<CommandHandlerService> logger)
	{
		_commandsModule = commandModule;
		_logger = logger;
	}

	public async Task<CommandResponse> ExecuteCommandAsync (IServiceProvider scopedProvider, string prompt)
	{
		CommandResponse? result = null;
		var watch = Stopwatch.StartNew();
		var tokens = ParseTokens(prompt);
		var commandName = GetCommandName(tokens);
		CommandInfo? command = null;

		try
		{
			command = GetCommand(commandName);
			result = await command.ExecuteAsync(scopedProvider, _commandsModule, tokens);
			result.IsSuccess = true;
		}
		catch (CommandException exception)
		{
			_logger.LogWarning(exception, "CommandException in command '{CommandName}' with prompt: {Prompt}", commandName, prompt);
			result = new()
			{
				CommandName = command?.Name ?? commandName ?? "unknown",
				CommandType = CommandType.Text,
				IsSuccess = false,
				Result = TextResult.Create(exception.Message),
			};
		}
		catch (Exception exception)
		{
			_logger.LogError(exception, "Command failed to execute: '{CommandName}' with prompt: {Prompt}", commandName, prompt);

			result = new()
			{
				CommandName = command?.Name ?? commandName ?? "unknown",
				CommandType = CommandType.Text,
				IsSuccess = false,
				Result = TextResult.Create("Command failed to execute"),
			};
		}
		finally
		{
			watch.Stop();
			OnLog?.Invoke($"Finished [{command?.Name ?? commandName ?? "unknown"}] command in {watch.ElapsedMilliseconds} ms", Array.Empty<object>());
		}

		return result;
	}

	private string GetCommandName (string[] tokens)
	{
		if (tokens.Length >= 1)
		{
			var commandSeg = tokens[0].ToLower();
			if (commandSeg.StartsWith('$') && commandSeg.Length > 1)
				return commandSeg[1..];
		}

		return _commandsModule.DefaultCommand;
	}

	private CommandInfo GetCommand (string? commandName)
	{
		if (string.IsNullOrEmpty(commandName) || !_commandsModule.Commands.TryGetValue(commandName, out CommandInfo? commandInfo))
			_commandsModule.Commands.TryGetValue(_commandsModule.DefaultCommand, out commandInfo);

		if (commandInfo is null)
			throw new Exception("Couldn't find any command that meets the requirements");

		return commandInfo;
	}

	private string[] ParseTokens (string input)
	{
		var matches = pattern.Matches(input);
		var tokens = new string[matches.Count];

		for (int i = 0; i < matches.Count; i++)
		{
			var match = matches[i].Value;
			if (match.StartsWith('\"') && match.EndsWith('\"'))
				tokens[i] = match[1..^1];
			else
				tokens[i] = match;
		}

		return tokens;
	}

	[GeneratedRegex("[\\\"].+?[\\\"]|[^ ]+", RegexOptions.Compiled)]
	private static partial Regex RegexCommandParserPattern ();
}
