namespace Jiro.App.Options;

/// <summary>
/// Configuration options for the gRPC service
/// </summary>
/// <summary>
/// Configuration options for gRPC communication
/// </summary>
public class GrpcOptions
{
	/// <summary>
	/// The URL of the gRPC server
	/// </summary>
	public string ServerUrl { get; set; } = "https://localhost:5001";

	/// <summary>
	/// Timeout for gRPC calls in milliseconds
	/// </summary>
	public int TimeoutMs { get; set; } = 30000;

	/// <summary>
	/// Maximum number of retry attempts for failed calls
	/// </summary>
	public int MaxRetries { get; set; } = 3;
}
