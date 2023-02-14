using Jiro.Core.Base;
using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.CommandHandler
{
    public class CommandHandlerService : ICommandHandlerService
    {
        private readonly CommandsContainer _commandModule;
        private readonly IServiceScopeFactory _scopeFactory;
        public event Action<string, object[]>? OnLog;
        public CommandHandlerService(CommandsContainer commandModule, IServiceScopeFactory scopeFactory)
        {
            _commandModule = commandModule;
            _scopeFactory = scopeFactory;
        }

        public async Task<CommandResponse> ExecuteCommandAsync(string prompt)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            string[] tokens = prompt.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

            var commandName = GetCommandName(tokens);
            var command = GetCommand(commandName);
            var commandInstance = GetCommandInstance(command.Module, scope);
            var args = GetCommandArgs(command.Name, tokens);

            CommandResponse commandResult = new()
            {
                CommandName = command.Name,
                CommandType = command.CommandType
            };

            try
            {
                if (commandInstance is null)
                    throw new CommandException(commandName, "Command instance is null");

                if (command.IsAsync)
                {
                    OnLog?.Invoke("Running command [{0}] [{1}]", new object[] { command.Name, command.CommandType.ToString() });

                    var task = command.Descriptor((ICommandBase)commandInstance, args);

                    if (task is Task<ICommandResult> commandTask)
                    {
                        commandResult.Result = await commandTask;
                    }
                    else
                    {
                        await task;
                    }
                }
                else
                {
                    commandResult.Result = (ICommandResult)command.Descriptor.Invoke((ICommandBase)commandInstance, args);
                }
            }
            catch (CommandException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new CommandException(commandName, exception.Message);
            }

            return commandResult;
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

        private static object? GetCommandInstance(Type type, IServiceScope scope) => scope.ServiceProvider.GetRequiredService(type);

        private object[] GetCommandArgs(string commandName, string[] tokens)
        {
            object[]? args;

            if (commandName == _commandModule.DefaultCommand)
                args = new object[] { string.Join(' ', tokens) };

            else if (tokens.Length >= 3)
                args = tokens[2..].Cast<object>().ToArray();

            else
                args = Array.Empty<object>();

            return args;
        }
    }
}