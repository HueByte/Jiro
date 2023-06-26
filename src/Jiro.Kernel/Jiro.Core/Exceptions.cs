namespace Jiro.Core;

public class JiroException : Exception
{
    public string UserMessage { get; set; }
    public string[] Details { get; set; }

    public JiroException(string userMessage) : this(userMessage, Array.Empty<string>()) { }
    public JiroException(string userMessage, params string[] details) : base(userMessage)
    {
        UserMessage = userMessage;
        Details = details;
    }

    public JiroException(Exception exception, string userMessage) : this(exception, userMessage, Array.Empty<string>()) { }
    public JiroException(Exception exception, string userMessage, params string[] details) : base(exception.Message, exception)
    {
        UserMessage = userMessage;
        Details = details;
    }
}

public class TokenException : Exception
{
    public TokenException(string exceptionMessage) : base(exceptionMessage) { }
}