namespace Jiro.App.Options;

// TODO: Follow options pattern for gRPC configuration
/// <summary>
/// Configuration options for the gRPC service
/// </summary>
/// <summary>
/// Configuration options for gRPC communication
/// </summary>
public class GrpcOptions
{

	/// <summary>
	/// Timeout for gRPC calls in milliseconds
	/// </summary>
	public int TimeoutMs { get; set; } = 30000;

	/// <summary>
	/// Maximum number of retry attempts for failed calls
	/// </summary>
	public int MaxRetries { get; set; } = 3;
}
