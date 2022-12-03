using System.Linq.Expressions;
using Jiro.Core.Commands.Base;
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
        public CommandHandlerService(ILogger<CommandHandlerService> logger, CommandsContainer commandModule, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _commandModule = commandModule;
            _scopeFactory = scopeFactory;
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
                Action = commandInfo.Action,
                IsAsync = commandInfo.IsAsync,
                Instance = scope.ServiceProvider.GetRequiredService(commandInfo.Container),
            };
        }

        private static (string?, string[]?) GetCommandNameFromPrompt(string prompt)
        {
            if (prompt.StartsWith("command"))
            {
                string[] tokens = prompt.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
                return (tokens[1].ToLower(), tokens[2..]);
            }

            return (null, null);
        }

        public async Task<CommandResponse> ExecuteCommandAsync(string prompt)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            (var commandName, var tokens) = GetCommandNameFromPrompt(prompt);

            var command = GetCommand(commandName, scope);

            object? result = null;

            if (command.IsAsync)
            {
                // temp solution
                var x = command.Action.Invoke(command.Instance, new object[] { prompt });
                var commandTask = (Task)command.Action.Invoke(command.Instance, new object[] { prompt })!;
                await commandTask;
                result = (object)((dynamic)commandTask).Result;
            }
            else
            {
                result = command.Action.Invoke(command.Instance, null);
            }

            return new CommandResponse() { Data = result };
        }
    }
}