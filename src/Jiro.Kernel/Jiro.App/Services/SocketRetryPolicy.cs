using Microsoft.AspNetCore.SignalR.Client;

namespace Jiro.App.Services;

/// <summary>
/// Custom retry policy: first 5 retries are short, then 1 minute constant interval
/// </summary>
public class SocketRetryPolicy : IRetryPolicy
{
	private static readonly TimeSpan[] _delays = new[]
	{
		TimeSpan.Zero,                // 0: immediately
        TimeSpan.FromSeconds(2),      // 1: after 2s
        TimeSpan.FromSeconds(10),     // 2: after 10s
        TimeSpan.FromSeconds(30),     // 3: after 30s
        TimeSpan.FromSeconds(60)      // 4: after 60s
    };
	private static readonly TimeSpan _constantDelay = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Returns the next retry delay based on the retry context.
	/// </summary>
	/// <param name="retryContext">The retry context provided by SignalR.</param>
	/// <returns>The delay before the next retry attempt.</returns>
	public TimeSpan? NextRetryDelay(RetryContext retryContext)
	{
		if (retryContext.PreviousRetryCount < _delays.Length)
		{
			return _delays[retryContext.PreviousRetryCount];
		}
		return _constantDelay;
	}
}
