namespace Jiro.Core.Options;

/// <summary>
/// Configuration options for JiroCloud API and services integration.
/// These options can be overridden using JIRO_ prefixed environment variables.
/// </summary>
public class JiroCloudOptions : IOption
{
	/// <summary>
	/// Configuration section name for JiroCloud options.
	/// </summary>
	public const string JiroCloud = "JiroCloud";

	/// <summary>
	/// Gets or sets the API key for JiroCloud authentication.
	/// Can be overridden with JIRO_JiroCloud__ApiKey environment variable.
	/// </summary>
	public string ApiKey { get; set; } = "your-jirocloud-api-key-here";

	/// <summary>
	/// Gets or sets the gRPC configuration for JiroCloud communication.
	/// </summary>
	public GrpcOptions Grpc { get; set; } = new();

	/// <summary>
	/// Gets or sets the WebSocket configuration for JiroCloud communication.
	/// </summary>
	public WebSocketOptions WebSocket { get; set; } = new();

	/// <summary>
	/// Configuration options for gRPC communication with JiroCloud.
	/// </summary>
	public class GrpcOptions
	{
		/// <summary>
		/// Gets or sets the maximum number of retry attempts for gRPC calls.
		/// Can be overridden with JIRO_JiroCloud__Grpc__MaxRetries environment variable.
		/// </summary>
		public int MaxRetries { get; set; } = 3;

		/// <summary>
		/// Gets or sets the gRPC server URL.
		/// Can be overridden with JIRO_JiroCloud__Grpc__ServerUrl environment variable.
		/// </summary>
		public string ServerUrl { get; set; } = "https://localhost:5001";

		/// <summary>
		/// Gets or sets the gRPC timeout in milliseconds.
		/// Can be overridden with JIRO_JiroCloud__Grpc__TimeoutMs environment variable.
		/// </summary>
		public int TimeoutMs { get; set; } = 30000;
	}

	/// <summary>
	/// Configuration options for WebSocket communication with JiroCloud.
	/// </summary>
	public class WebSocketOptions
	{
		/// <summary>
		/// Gets or sets the handshake timeout in milliseconds.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__HandshakeTimeoutMs environment variable.
		/// </summary>
		public int HandshakeTimeoutMs { get; set; } = 15000;

		/// <summary>
		/// Gets or sets additional headers to send with WebSocket connections.
		/// </summary>
		public Dictionary<string, string> Headers { get; set; } = new()
		{
			["User-Agent"] = "Jiro-Bot/1.0"
		};

		/// <summary>
		/// Gets or sets the SignalR hub URL for WebSocket connections.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__HubUrl environment variable.
		/// </summary>
		public string HubUrl { get; set; } = "https://localhost:5001/instanceHub";

		/// <summary>
		/// Gets or sets the keep-alive interval in milliseconds.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__KeepAliveIntervalMs environment variable.
		/// </summary>
		public int KeepAliveIntervalMs { get; set; } = 15000;

		/// <summary>
		/// Gets or sets the maximum number of reconnection attempts.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__ReconnectionAttempts environment variable.
		/// </summary>
		public int ReconnectionAttempts { get; set; } = 5;

		/// <summary>
		/// Gets or sets the delay between reconnection attempts in milliseconds.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__ReconnectionDelayMs environment variable.
		/// </summary>
		public int ReconnectionDelayMs { get; set; } = 5000;

		/// <summary>
		/// Gets or sets the server timeout in milliseconds.
		/// Can be overridden with JIRO_JiroCloud__WebSocket__ServerTimeoutMs environment variable.
		/// </summary>
		public int ServerTimeoutMs { get; set; } = 30000;
	}
}