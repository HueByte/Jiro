using System.Linq.Expressions;
using System.Runtime.InteropServices;
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

            CommandResponse result = new()
            {
                IsSuccess = true
            };

            try
            {
                if (command.IsAsync)
                {
                    OnLog?.Invoke("Running command [{0}]", new object[] { command.Name });
                    var task = command.Descriptor((CommandBase)command.Instance!, new object[] { prompt });
                    if (task is Task<ICommandResult> commandTask)
                    {
                        result.Data = await commandTask;
                    }
                    else
                    {
                        await task;
                    }
                }
                else
                {
                    result.Data = command.Descriptor.Invoke((CommandBase)command.Instance!, new object[] { prompt });
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Errors.Add(ex.Message);
            }

            return result;
        }
    }
}