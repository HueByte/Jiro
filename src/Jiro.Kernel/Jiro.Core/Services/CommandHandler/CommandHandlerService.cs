using System.Diagnostics;
using System.Text.RegularExpressions;
using Jiro.Core.Base;
using Jiro.Core.Base.Models;
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
            var watch = Stopwatch.StartNew();
            var tokens = ParseTokens(prompt);
            var commandName = GetCommandName(tokens);
            var command = GetCommand(commandName);

            CommandResponse? result = null;

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
            finally
            {
                OnLog?.Invoke("Finished [{commandName}] command in {time} ms", new object[] { commandName, watch.ElapsedMilliseconds });
            }

            return result;
        }

        private string GetCommandName(string[] tokens)
        {
            if (tokens.Length >= 1)
            {
                var commandSeg = tokens[0].ToLower();
                if (commandSeg.StartsWith('$') && commandSeg.Length > 1)
                    return commandSeg[1..];
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
                var match = matches[i].Value;
                if (match.StartsWith('\"') && match.EndsWith('\"'))
                    tokens[i] = match[1..^1];
                else
                    tokens[i] = match;
            }

            return tokens;
        }

        [GeneratedRegex("[\\\"].+?[\\\"]|[^ ]+", RegexOptions.Compiled)]
        private static partial Regex RegexCommandParserPattern();
    }
}