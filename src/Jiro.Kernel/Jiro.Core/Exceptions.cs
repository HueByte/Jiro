namespace Jiro.Core;

public class HandledExceptionList : Exception
{
    public ICollection<string> ExceptionMessages { get; set; }
    public HandledExceptionList(ICollection<string> exceptionMessages)
    {
        ExceptionMessages = exceptionMessages;
    }
}

public class HandledException : Exception
{
    public HandledException(string exceptionMessage) : base(exceptionMessage) { }
}

public class TokenException : Exception
{
    public TokenException(string exceptionMessage) : base(exceptionMessage) { }
}

public class CommandException : Exception
{
    public string CommandName { get; set; }
    public CommandException(string commandName, string exceptionMessage) : base(exceptionMessage)
    {
        CommandName = commandName;
    }
}