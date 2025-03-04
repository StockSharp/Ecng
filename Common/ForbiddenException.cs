namespace Ecng.Common;

using System;

/// <summary>
/// Exception thrown when an operation is forbidden.
/// </summary>
/// <param name="message">The error message that explains the reason for the exception.</param>
public class ForbiddenException(string message) : InvalidOperationException(message)
{
}