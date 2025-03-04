namespace Ecng.Common;

using System;

/// <summary>
/// Represents an exception that is thrown when a duplicate operation is performed.
/// </summary>
/// <param name="message">A message that describes the error.</param>
public class DuplicateException(string message) : InvalidOperationException(message)
{
}