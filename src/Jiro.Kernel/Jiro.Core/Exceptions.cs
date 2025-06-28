namespace Jiro.Core;

/// <summary>
/// Represents an exception specific to the Jiro application domain with user-friendly messages and detailed error information.
/// </summary>
public class JiroException : Exception
{
	/// <summary>
	/// Gets or sets the user-friendly error message that can be displayed to end users.
	/// </summary>
	public string UserMessage { get; set; }

	/// <summary>
	/// Gets or sets additional error details that provide more context about the exception.
	/// </summary>
	public string[] Details { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroException"/> class with a user message.
	/// </summary>
	/// <param name="userMessage">The user-friendly error message.</param>
	public JiroException(string userMessage) : this(userMessage, []) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroException"/> class with a user message and additional details.
	/// </summary>
	/// <param name="userMessage">The user-friendly error message.</param>
	/// <param name="details">Additional error details.</param>
	public JiroException(string userMessage, params string[] details) : base(userMessage)
	{
		UserMessage = userMessage;
		Details = details;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroException"/> class with an inner exception and user message.
	/// </summary>
	/// <param name="exception">The inner exception that caused this exception.</param>
	/// <param name="userMessage">The user-friendly error message.</param>
	public JiroException(Exception exception, string userMessage) : this(exception, userMessage, []) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="JiroException"/> class with an inner exception, user message, and additional details.
	/// </summary>
	/// <param name="exception">The inner exception that caused this exception.</param>
	/// <param name="userMessage">The user-friendly error message.</param>
	/// <param name="details">Additional error details.</param>
	public JiroException(Exception exception, string userMessage, params string[] details) : base(exception.Message, exception)
	{
		UserMessage = userMessage;
		Details = details;
	}
}

/// <summary>
/// Represents an exception that occurs during token processing or authentication operations.
/// </summary>
public class TokenException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TokenException"/> class with the specified error message.
	/// </summary>
	/// <param name="exceptionMessage">The message that describes the error.</param>
	public TokenException(string exceptionMessage) : base(exceptionMessage) { }
}
