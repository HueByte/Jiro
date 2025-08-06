using Jiro.Core.Services.CommandContext;
using Jiro.Core.Services.CommandSystem;
using Jiro.Core.Services.Context;
using Jiro.Core.Services.MessageCache;
using Jiro.Core.Services.System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jiro.Core.Commands.System;

/// <summary>
/// System administration commands for Jiro instance management
/// </summary>
[CommandModule("System")]
public class SystemCommand : ICommandBase
{
	private readonly ILogger<SystemCommand> _logger;
	private readonly ICommandContext _commandContext;
	private readonly IMessageManager _messageManager;
	private readonly IHelpService _helpService;
	private readonly IConfiguration _configuration;
	private readonly ILogsProviderService _logsProviderService;
	private readonly IConfigProviderService _configProviderService;
	private readonly IThemeService _themeService;
	private readonly IInstanceMetadataAccessor _instanceMetadataAccessor;

	/// <summary>
	/// Initializes a new instance of the SystemCommand class
	/// </summary>
	public SystemCommand(
		ILogger<SystemCommand> logger,
		ICommandContext commandContext,
		IMessageManager messageManager,
		IHelpService helpService,
		IConfiguration configuration,
		ILogsProviderService logsProviderService,
		IConfigProviderService configProviderService,
		IThemeService themeService,
		IInstanceMetadataAccessor instanceMetadataAccessor)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_commandContext = commandContext ?? throw new ArgumentNullException(nameof(commandContext));
		_messageManager = messageManager ?? throw new ArgumentNullException(nameof(messageManager));
		_helpService = helpService ?? throw new ArgumentNullException(nameof(helpService));
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		_logsProviderService = logsProviderService ?? throw new ArgumentNullException(nameof(logsProviderService));
		_configProviderService = configProviderService ?? throw new ArgumentNullException(nameof(configProviderService));
		_themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
		_instanceMetadataAccessor = instanceMetadataAccessor ?? throw new ArgumentNullException(nameof(instanceMetadataAccessor));
	}

	/// <summary>
	/// Retrieves system logs with optional filtering
	/// </summary>
	[Command("getLogs", commandDescription: "Retrieves system logs with optional level and limit filtering")]
	public async Task<ICommandResult> GetLogs(string? level = null, int limit = 100)
	{
		try
		{
			_logger.LogInformation("Getting system logs - Level: {Level}, Limit: {Limit}", level, limit);

			var logsResponse = await _logsProviderService.GetLogsAsync(level, limit);

			return JsonResult.Create(logsResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving system logs");
			return TextResult.Create("Error retrieving system logs: " + ex.Message);
		}
	}

	/// <summary>
	/// Retrieves all chat sessions for the current instance
	/// </summary>
	[Command("getSessions", commandDescription: "Retrieves all chat sessions for the current instance")]
	public async Task<ICommandResult> GetSessions()
	{
		try
		{
			var instanceId = await _instanceMetadataAccessor.GetInstanceIdAsync("") ?? _instanceMetadataAccessor.GetCurrentInstanceId();
			if (string.IsNullOrEmpty(instanceId))
			{
				return TextResult.Create("Instance ID not available");
			}

			_logger.LogInformation("Getting sessions for instance: {InstanceId}", instanceId);

			var sessions = await _messageManager.GetChatSessionsAsync(instanceId);
			var response = new
			{
				InstanceId = instanceId,
				TotalSessions = sessions.Count,
				CurrentSessionId = _commandContext.SessionId,
				Sessions = sessions
			};

			return JsonResult.Create(response);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving sessions");
			return TextResult.Create("Error retrieving sessions: " + ex.Message);
		}
	}

	/// <summary>
	/// Retrieves current system configuration
	/// </summary>
	[Command("getConfig", commandDescription: "Retrieves current system configuration")]
	public async Task<ICommandResult> GetConfig()
	{
		try
		{
			_logger.LogInformation("Getting system configuration");

			var configResponse = await _configProviderService.GetConfigAsync();

			return JsonResult.Create(configResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving configuration");
			return TextResult.Create("Error retrieving configuration: " + ex.Message);
		}
	}

	/// <summary>
	/// Updates system configuration (limited scope for security)
	/// </summary>
	[Command("updateConfig", commandDescription: "Updates system configuration")]
	public async Task<ICommandResult> UpdateConfig(string config)
	{
		try
		{
			_logger.LogInformation("Received configuration update request");

			var updateResponse = await _configProviderService.UpdateConfigAsync(config);

			return JsonResult.Create(updateResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating configuration");
			return TextResult.Create("Error updating configuration: " + ex.Message);
		}
	}

	/// <summary>
	/// Retrieves available custom themes
	/// </summary>
	[Command("getCustomThemes", commandDescription: "Retrieves available custom themes")]
	public async Task<ICommandResult> GetCustomThemes()
	{
		try
		{
			_logger.LogInformation("Getting custom themes");

			var themeResponse = await _themeService.GetCustomThemesAsync();

			return JsonResult.Create(themeResponse);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving custom themes");
			return TextResult.Create("Error retrieving custom themes: " + ex.Message);
		}
	}

	/// <summary>
	/// Retrieves metadata about available commands
	/// </summary>
	[Command("getCommandsMetadata", commandDescription: "Retrieves metadata about available commands")]
	public Task<ICommandResult> GetCommandsMetadata()
	{
		try
		{
			_logger.LogInformation("Getting commands metadata");

			var commandsMetadata = _helpService.CommandMeta;

			return Task.FromResult<ICommandResult>(JsonResult.Create(commandsMetadata));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving commands metadata");
			return Task.FromResult<ICommandResult>(TextResult.Create("Error retrieving commands metadata: " + ex.Message));
		}
	}

	/// <summary>
	/// Shows current session information - demonstrates session context availability
	/// </summary>
	[Command("sessionInfo", commandDescription: "Shows current session information")]
	public Task<ICommandResult> GetSessionInfo()
	{
		try
		{
			_logger.LogInformation("Getting session information");

			var sessionInfo = new
			{
				SessionId = _commandContext.SessionId ?? "null",
				InstanceId = _commandContext.InstanceId ?? "null",
				HasSessionData = !string.IsNullOrEmpty(_commandContext.SessionId),
				ContextDataCount = _commandContext.Data?.Count ?? 0
			};

			return Task.FromResult<ICommandResult>(JsonResult.Create(sessionInfo));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving session information");
			return Task.FromResult<ICommandResult>(TextResult.Create("Error retrieving session information: " + ex.Message));
		}
	}
}
