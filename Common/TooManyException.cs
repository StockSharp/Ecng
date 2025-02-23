namespace Ecng.Common;

using System;

/// <summary>
/// Represents an exception that is thrown when an operation encounters too many elements.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
public class TooManyException(string message) : InvalidOperationException(message)
{
}