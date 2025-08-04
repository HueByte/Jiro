using Jiro.Core.Constants;

namespace Jiro.Core.Commands.Net;

/// <summary>
/// Command module that provides network-related operations such as HTTP requests.
/// </summary>
[CommandModule("Net")]
public class NetCommands : ICommandBase
{
	/// <summary>
	/// The HTTP client used for making network requests.
	/// </summary>
	private readonly HttpClient _jiroClient;
	/// <summary>
	/// Initializes a new instance of the NetCommands class.
	/// </summary>
	/// <param name="clientFactory">The HTTP client factory for creating HTTP clients.</param>
	public NetCommands(IHttpClientFactory clientFactory)
	{
		_jiroClient = clientFactory.CreateClient(HttpClients.JIRO);
	}

	/// <summary>
	/// Performs an HTTP GET request to the specified URL and returns the response content.
	/// </summary>
	/// <param name="url">The URL to send the GET request to.</param>
	/// <returns>A task representing the asynchronous operation that returns the response content wrapped in a markdown code block.</returns>
	[Command("GET")]
	public async Task<ICommandResult> Get(string url)
	{
		var response = await _jiroClient.GetStringAsync(url);
		response = WrapInMarkdownCodeBlock(response);
		return TextResult.Create(response);
	}

	/// <summary>
	/// Wraps the provided text in a markdown HTML code block for better formatting.
	/// </summary>
	/// <param name="text">The text to wrap in a code block.</param>
	/// <returns>The text wrapped in markdown HTML code block syntax.</returns>
	private static string WrapInMarkdownCodeBlock(string text) => $"```html\n{text}\n```";
}
