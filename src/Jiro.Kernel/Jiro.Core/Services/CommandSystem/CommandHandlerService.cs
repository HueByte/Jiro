using System.Diagnostics;
using System.Text.RegularExpressions;
using Jiro.Core.Base.Models;
using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;

namespace Jiro.Core.Services.CommandHandler
{
    public partial class CommandHandlerService : ICommandHandlerService
    {
        private readonly CommandsContext _commandsModule;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Regex pattern = RegexCommandParserPattern();
        public event Action<string, object[]>? OnLog;
        public CommandHandlerService(CommandsContext commandModule, IServiceScopeFactory scopeFactory)
        {
            _commandsModule = commandModule;
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
                result = await command.ExecuteAsync(scope, _commandsModule, tokens);
            }
            catch (CommandException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new AggregateException(new Exception[]
                {
                    new CommandException(command.Name, "Command failed to exectute"),
                    exception
                });
            }
            finally
            {
                watch.Stop();
                OnLog?.Invoke("Finished [{commandName}] command in {time} ms", new object[] { command.Name, watch.ElapsedMilliseconds });
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

            return _commandsModule.DefaultCommand;
        }

        private CommandInfo GetCommand(string? commandName)
        {
            if (string.IsNullOrEmpty(commandName) || !_commandsModule.Commands.TryGetValue(commandName, out CommandInfo? commandInfo))
                _commandsModule.Commands.TryGetValue(_commandsModule.DefaultCommand, out commandInfo);

            if (commandInfo is null)
                throw new Exception("Couldn't find any command that meets the requirements");

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