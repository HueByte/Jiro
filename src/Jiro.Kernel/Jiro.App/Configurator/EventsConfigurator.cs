using Jiro.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace Jiro.App.Configurator;

public class EventsConfigurator
{
    private readonly ILogger _logger;
    private readonly ICommandHandlerService _commandHandlerService;
    public EventsConfigurator(ILogger<EventsConfigurator> logger, ICommandHandlerService commandHandlerService)
    {
        _logger = logger;
        _commandHandlerService = commandHandlerService;
    }
    public void ConfigureLoggingEvents()
    {
        _commandHandlerService.OnLog += OnCommandLog;
    }

    public void OnCommandLog(string message, object[] args) => _logger.LogInformation(message, args);
}