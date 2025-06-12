namespace Jiro.Core.Options;

public class ChatOptions : IOption
{
    public const string Chat = "Chat";
    public bool Enabled { get; set; }
    public string SystemMessage { get; set; } = string.Empty;

    // To be removed later, store in secured location
    public string AuthToken { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokenLimit { get; set; } = 2000;
}
