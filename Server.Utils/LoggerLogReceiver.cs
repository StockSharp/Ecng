namespace Ecng.Server.Utils;

using System;

using Microsoft.Extensions.Logging;

// BaseLogSource has a LogLevel of its own, so the framework one needs a name here.
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

using Ecng.Common;
using Ecng.Logging;

/// <summary>
/// A log source that writes what it is given into an <see cref="ILogger"/>.
/// </summary>
/// <remarks>
/// The counterpart of <see cref="LogManagerLoggerProvider"/>, for code that has a logger and must hand a
/// log to a component built around <see cref="ILogSource"/>. Set as a component's parent, it collects
/// what the component and its children log and passes it on, so the component needs no other arrangement
/// and the code holding it needs nothing but its logger.
/// </remarks>
public class LoggerLogReceiver : BaseLogReceiver
{
	private readonly ILogger _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="LoggerLogReceiver"/>.
	/// </summary>
	/// <param name="logger">The logger to write into.</param>
	/// <param name="name">Source name, shown on the messages it passes on.</param>
	public LoggerLogReceiver(ILogger logger, string name)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		Name = name;
	}

	/// <inheritdoc />
	protected override void RaiseLog(LogMessage message)
	{
		base.RaiseLog(message);

		if (message is null)
			return;

		_logger.Log(ToLevel(message.Level), "{Source}: {Message}", message.Source?.Name, message.Message);
	}

	private static MsLogLevel ToLevel(LogLevels level) => level switch
	{
		LogLevels.Verbose => MsLogLevel.Trace,
		LogLevels.Debug => MsLogLevel.Debug,
		LogLevels.Info => MsLogLevel.Information,
		LogLevels.Warning => MsLogLevel.Warning,
		LogLevels.Error => MsLogLevel.Error,
		_ => MsLogLevel.None,
	};
}
