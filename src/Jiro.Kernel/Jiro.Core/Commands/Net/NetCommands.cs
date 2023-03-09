using Jiro.Core.Constants;

namespace Jiro.Core.Commands.Net
{
    [CommandModule("Net")]
    public class NetCommands : ICommandBase
    {
        private readonly HttpClient _jiroClient;
        public NetCommands(IHttpClientFactory clientFactory)
        {
            _jiroClient = clientFactory.CreateClient(HttpClients.JIRO);
        }

        [Command("GET")]
        public async Task<ICommandResult> Get(string url)
        {
            var response = await _jiroClient.GetStringAsync(url);
            response = WrapInMarkdownCodeBlock(response);
            return TextResult.Create(response);
        }

        private static string WrapInMarkdownCodeBlock(string text) => $"```html\n{text}\n```";
    }
}