namespace Ecng.Common;

/// <summary>
/// Represents an exception that is thrown when an operation is attempted on an expired object or state.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
public class ExpiredException(string message) : InvalidOperationException(message)
{
}