using Jiro.Core.Base;
using Jiro.Core.Entities;
using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Services.CommandHandler
{
    public class CommandHandlerService : ICommandHandlerService
    {
        private readonly ILogger _logger;
        private readonly CommandsContainer _commandModule;
        private readonly IServiceScopeFactory _scopeFactory;
        public event Action<string, object[]> OnLog;
        public CommandHandlerService(ILogger<CommandHandlerService> logger, CommandsContainer commandModule, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _commandModule = commandModule;
            _scopeFactory = scopeFactory;
        }

        public async Task<CommandResponse> ExecuteCommandAsync(string prompt)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            string[] tokens = prompt.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);

            var commandName = GetCommandName(tokens);
            var command = GetCommand(commandName, scope);
            var args = GetCommandArgs(command, tokens);

            CommandResponse commandResult = new() { IsSuccess = true, CommandName = command.Name };

            try
            {
                if (command.IsAsync)
                {
                    OnLog?.Invoke("Running command [{0}]", new object[] { command.Name });

                    var task = command.Descriptor((CommandBase)command.Instance!, args);

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
                    commandResult.Result = (ICommandResult)command.Descriptor.Invoke((CommandBase)command.Instance!, args);
                }
            }
            catch (Exception ex)
            {
                commandResult.IsSuccess = false;
                commandResult.Errors.Add(ex.Message);
                _logger.LogError(ex, "Error while executing command [{name}]", command.Name);
            }

            return commandResult;
        }

        private string GetCommandName(string[] tokens)
        {
            if (tokens.Length >= 2)
            {
                var commandParam = tokens[0].ToLower();
                if (commandParam.StartsWith("command") || commandParam.StartsWith('$'))
                    return tokens[1].ToLower();
            }

            return _commandModule.DefaultCommand;
        }

        private object[] GetCommandArgs(Command command, string[] tokens)
        {
            object[]? args;

            if (command.Name == _commandModule.DefaultCommand)
                args = new object[] { string.Join(' ', tokens) };

            else if (tokens.Length >= 3)
                args = tokens[2..].Cast<object>().ToArray();

            else
                args = Array.Empty<object>();

            return args;
        }

        private Command GetCommand(string? commandName, IServiceScope scope)
        {
            if (string.IsNullOrEmpty(commandName) || !_commandModule.Commands.TryGetValue(commandName, out CommandInfo? commandInfo))
                _commandModule.Commands.TryGetValue(_commandModule.DefaultCommand, out commandInfo);

            if (commandInfo is null)
                throw new Exception("Couldn't find this command");

            return new()
            {
                Name = commandInfo.Name,
                Descriptor = commandInfo.Descriptor,
                IsAsync = commandInfo.IsAsync,
                Instance = scope.ServiceProvider.GetRequiredService(commandInfo.Module),
            };
        }
    }
}