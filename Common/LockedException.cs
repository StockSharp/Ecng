namespace Ecng.Common;

using System;

/// <summary>
/// Represents the exception that is thrown when an operation is attempted on a locked resource.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
public class LockedException(string message) : InvalidOperationException(message)
{
}
