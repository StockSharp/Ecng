namespace Ecng.Nuget;

/// <summary>
/// The logger for Nuget.
/// </summary>
/// <param name="logger"><see cref="ILogReceiver"/></param>
public class NugetLogger(ILogReceiver logger) : LoggerBase
{
	private readonly ILogReceiver _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	/// <inheritdoc />
	public override void Log(ILogMessage message)
	{
		switch (message.Level)
		{
			case LogLevel.Verbose:		_logger.LogVerbose(message.Message); break;
			case LogLevel.Debug:		_logger.LogDebug(message.Message);	break;
			case LogLevel.Information:	_logger.LogInfo(message.Message);	break;
			case LogLevel.Minimal:		_logger.LogInfo(message.Message);	break;
			case LogLevel.Warning:		_logger.LogWarning(message.Message); break;
			case LogLevel.Error:		_logger.LogError(message.Message);	break;

			default:
				throw new ArgumentOutOfRangeException(message.Level.ToString());
		}
	}

	/// <inheritdoc />
	public override Task LogAsync(ILogMessage message)
	{
		Log(message);
		return Task.CompletedTask;
	}
}