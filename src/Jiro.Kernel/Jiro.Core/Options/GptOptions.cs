using System.ComponentModel.DataAnnotations;

namespace Jiro.Core.Options;

public class GptOptions
{
    public const string Gpt = "Gpt";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public bool FineTune { get; set; }

    [Required]
    public bool UseChatGpt { get; set; }
    [Required]
    public bool Enable { get; set; }

    public ChatGptOptions? ChatGpt { get; set; }
    public SingleGptOptions? SingleGpt { get; set; }
}

public class ChatGptOptions
{
    public const string ChatGpt = "ChatGpt";

    public string SystemMessage { get; set; } = string.Empty;
}

public class SingleGptOptions
{
    public const string SingleGpt = "SingleGpt";

    public int TokenLimit { get; set; } = 500;
    public string ContextMessage { get; set; } = string.Empty;
    public string? Stop { get; set; }
    public string? Model { get; set; }
}