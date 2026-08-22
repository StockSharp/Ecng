namespace Ecng.Server.Utils;

using System;

using Microsoft.Extensions.Logging;

/// <summary>
/// Extensions for <see cref="ILogger"/> that the framework does not provide.
/// </summary>
public static class LoggerExtensions
{
	/// <summary>
	/// Records an exception with no message of its own.
	/// </summary>
	/// <param name="logger">Logger.</param>
	/// <param name="exception">The exception to record.</param>
	/// <remarks>
	/// Every framework overload that takes an exception also demands a message, so a catch block with nothing
	/// to add beyond the exception has to invent one -- and an invented message is what a reader sees instead
	/// of the failure. This says only what happened.
	/// </remarks>
	public static void LogError(this ILogger logger, Exception exception)
	{
		if (logger is null)
			throw new ArgumentNullException(nameof(logger));

		if (exception is null)
			throw new ArgumentNullException(nameof(exception));

		logger.LogError(exception, string.Empty);
	}
}
