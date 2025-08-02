using Jiro.Core.Services.CommandHandler;

using Microsoft.Extensions.Logging;

namespace Jiro.App.Setup;

/// <summary>
/// Provides configuration for application events, particularly for logging events from command handlers.
/// </summary>
public class EventsConfigurator
{
	private readonly ILogger _logger;
	private readonly ICommandHandlerService _commandHandlerService;

	/// <summary>
	/// Initializes a new instance of the <see cref="EventsConfigurator"/> class.
	/// </summary>
	/// <param name="logger">The logger instance for recording events.</param>
	/// <param name="commandHandlerService">The command handler service to configure events for.</param>
	public EventsConfigurator(ILogger<EventsConfigurator> logger, ICommandHandlerService commandHandlerService)
	{
		_logger = logger;
		_commandHandlerService = commandHandlerService;
	}

	/// <summary>
	/// Configures logging events by subscribing to the command handler's OnLog event.
	/// </summary>
	public void ConfigureLoggingEvents()
	{
		_commandHandlerService.OnLog += OnCommandLog;
	}

	/// <summary>
	/// Handles command log events by forwarding the message and arguments to the logger as information-level logs.
	/// </summary>
	/// <param name="message">The log message template.</param>
	/// <param name="args">The arguments to format into the message template.</param>
	public void OnCommandLog(string message, object[] args) => _logger.LogInformation(message, args);
}
