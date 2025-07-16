namespace Jiro.App.Models;

/// <summary>
/// Base class for WebSocket command parameters
/// </summary>
public abstract class WebSocketParametersBase
{
    /// <summary>
    /// Gets or sets the request ID for tracking purposes
    /// </summary>
    public string RequestId { get; set; } = string.Empty;
}

/// <summary>
/// Parameters for GetLogs command
/// </summary>
public class GetLogsParameters : WebSocketParametersBase
{
    /// <summary>
    /// Gets or sets the log level filter (optional)
    /// </summary>
    public string? Level { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of logs to retrieve (optional, defaults to 100)
    /// </summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Parameters for GetSessions command
/// </summary>
public class GetSessionsParameters : WebSocketParametersBase
{
    // Only contains RequestId from base class
}

/// <summary>
/// Parameters for GetConfig command
/// </summary>
public class GetConfigParameters : WebSocketParametersBase
{
    // Only contains RequestId from base class
}

/// <summary>
/// Parameters for UpdateConfig command
/// </summary>
public class UpdateConfigParameters : WebSocketParametersBase
{
    /// <summary>
    /// Gets or sets the configuration data to update
    /// </summary>
    public object ConfigData { get; set; } = default!;
}

/// <summary>
/// Parameters for GetCustomThemes command
/// </summary>
public class GetCustomThemesParameters : WebSocketParametersBase
{
    // Only contains RequestId from base class
}

/// <summary>
/// Parameters for GetCommandsMetadata command
/// </summary>
public class GetCommandsMetadataParameters : WebSocketParametersBase
{
    // Only contains RequestId from base class
}
