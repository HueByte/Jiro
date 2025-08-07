using System.Diagnostics;
using System.Text.RegularExpressions;

using Jiro.Commands.Exceptions;
using Jiro.Core.Services.CommandContext;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.CommandHandler;

/// <summary>
/// Service responsible for parsing, executing, and handling commands within the Jiro system.
/// </summary>
public partial class CommandHandlerService : ICommandHandlerService
{
	private readonly CommandsContext _commandsModule;
	private readonly ILogger _logger;
	private readonly Regex pattern = RegexCommandParserPattern();

	/// <summary>
	/// Event triggered when a log message is generated during command execution.
	/// </summary>
	public event Action<string, object[]>? OnLog;

	/// <summary>
	/// Initializes a new instance of the <see cref="CommandHandlerService"/> class.
	/// </summary>
	/// <param name="commandModule">The commands context containing available commands.</param>
	/// <param name="logger">The logger instance for recording command execution information.</param>
	public CommandHandlerService(CommandsContext commandModule, ILogger<CommandHandlerService> logger)
	{
		_commandsModule = commandModule;
		_logger = logger;
	}

	/// <summary>
	/// Executes a command based on the provided prompt string.
	/// </summary>
	/// <param name="scopedProvider">The scoped service provider for dependency injection.</param>
	/// <param name="prompt">The command prompt to parse and execute.</param>
	/// <returns>A task that represents the asynchronous operation, containing the command response.</returns>
	public async Task<CommandResponse> ExecuteCommandAsync(IServiceProvider scopedProvider, string prompt)
	{
		CommandResponse? result = null;
		var watch = Stopwatch.StartNew();
		var tokens = ParseTokens(prompt);
		var commandName = GetCommandName(tokens);
		CommandInfo? command = null;

		try
		{
			// Ensure session ID is available before command execution
			await EnsureSessionIdAsync(scopedProvider);

			command = GetCommand(commandName);
			// Pass tokens without the command name (skip first token if it's a command)
			var argTokens = tokens.Length > 1 && tokens[0].StartsWith('$') ? tokens[1..] : tokens;
			result = await command.ExecuteAsync(scopedProvider, _commandsModule, argTokens);
			result.IsSuccess = true;
			result.CommandName = command.Name;
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

	/// <summary>
	/// Extracts the command name from the parsed tokens.
	/// </summary>
	/// <param name="tokens">The array of parsed tokens from the command prompt.</param>
	/// <returns>The command name, or the default command if no valid command is found.</returns>
	private string GetCommandName(string[] tokens)
	{
		if (tokens.Length >= 1)
		{
			var commandSeg = tokens[0].ToLower();
			if (commandSeg.StartsWith('$') && commandSeg.Length > 1)
				return commandSeg[1..];
		}

		return _commandsModule.DefaultCommand;
	}

	/// <summary>
	/// Retrieves the command information for the specified command name.
	/// </summary>
	/// <param name="commandName">The name of the command to retrieve.</param>
	/// <returns>The command information object.</returns>
	/// <exception cref="Exception">Thrown when no command matching the requirements is found.</exception>
	private CommandInfo GetCommand(string? commandName)
	{
		if (string.IsNullOrEmpty(commandName) || !_commandsModule.Commands.TryGetValue(commandName, out CommandInfo? commandInfo))
			_commandsModule.Commands.TryGetValue(_commandsModule.DefaultCommand, out commandInfo);

		if (commandInfo is null)
			throw new Exception("Couldn't find any command that meets the requirements");

		return commandInfo;
	}

	/// <summary>
	/// Parses the input string into individual tokens, handling quoted strings.
	/// </summary>
	/// <param name="input">The input string to parse.</param>
	/// <returns>An array of parsed tokens.</returns>
	private string[] ParseTokens(string input)
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

	/// <summary>
	/// Ensures that a session ID is available in the command context, creating one if necessary.
	/// </summary>
	/// <param name="scopedProvider">The scoped service provider for dependency injection.</param>
	private async Task EnsureSessionIdAsync(IServiceProvider scopedProvider)
	{
		var commandContext = scopedProvider.GetRequiredService<ICommandContext>();

		// If no sessionId provided, generate a new one
		if (string.IsNullOrEmpty(commandContext.SessionId))
		{
			var sessionId = Guid.NewGuid().ToString();
			commandContext.SetSessionId(sessionId);

			// Store sessionId in context data for response
			commandContext.Data["generatedSessionId"] = sessionId;

			_logger.LogInformation("Generated new SessionId: '{NewSessionId}' for command execution", sessionId);
		}
		else
		{
			_logger.LogInformation("Using existing SessionId: '{SessionId}' for command execution", commandContext.SessionId);
		}

		await Task.CompletedTask;
	}

	/// <summary>
	/// Gets the compiled regex pattern for parsing command tokens, including quoted strings.
	/// </summary>
	/// <returns>A compiled regex pattern for command parsing.</returns>
	[GeneratedRegex("[\\\"].+?[\\\"]|[^ ]+", RegexOptions.Compiled)]
	private static partial Regex RegexCommandParserPattern();
}
