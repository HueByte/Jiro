using System.Text.RegularExpressions;
using Jiro.Core.Base;
using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Services.CommandHandler
{
    public partial class CommandHandlerService : ICommandHandlerService
    {
        private readonly CommandsContainer _commandModule;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Regex pattern = RegexCommandParserPattern();
        public event Action<string, object[]>? OnLog;
        public CommandHandlerService(CommandsContainer commandModule, IServiceScopeFactory scopeFactory)
        {
            _commandModule = commandModule;
            _scopeFactory = scopeFactory;
        }

        public async Task<CommandResponse> ExecuteCommandAsync(string prompt)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var tokens = ParseTokens(prompt);

            var commandName = GetCommandName(tokens);
            var command = GetCommand(commandName);

            CommandResponse result = null;
            try
            {
                result = await command.ExecuteAsync(scope, _commandModule, tokens);
            }
            catch (CommandException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new CommandException(commandName, exception.Message);
            }

            return result;
        }

        private string GetCommandName(string[] tokens)
        {
            if (tokens.Length >= 2)
            {
                var commandPrefix = tokens[0].ToLower();
                if (commandPrefix == "command" || commandPrefix == "$")
                    return tokens[1].ToLower();
            }

            return _commandModule.DefaultCommand;
        }

        private CommandInfo GetCommand(string? commandName)
        {
            if (string.IsNullOrEmpty(commandName) || !_commandModule.Commands.TryGetValue(commandName, out CommandInfo? commandInfo))
                _commandModule.Commands.TryGetValue(_commandModule.DefaultCommand, out commandInfo);

            if (commandInfo is null)
                throw new Exception("Couldn't find this command, default command wasn't configured either");

            return commandInfo;
        }

        private string[] ParseTokens(string input)
        {
            var matches = pattern.Matches(input);
            var tokens = new string[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                tokens[i] = matches[i].Value;
            }

            return tokens;
        }

        [GeneratedRegex("[\\\"].+?[\\\"]|[^ ]+", RegexOptions.Compiled)]
        private static partial Regex RegexCommandParserPattern();
    }
}