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
    public HandledException(string ExceptionMessage) : base(ExceptionMessage) { }
}

public class TokenException : Exception
{
    public TokenException(string ExceptionMessage) : base(ExceptionMessage) { }
}