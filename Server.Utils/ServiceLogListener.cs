namespace Ecng.Server.Utils;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;

using Ecng.Serialization;
using Ecng.Logging;

/// <summary>
/// The logger recording the data to a <see cref="ILogger"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceLogListener"/>.
/// </remarks>
/// <param name="logger">Logger.</param>
public class ServiceLogListener(ILogger logger) : ILogListener
{
	private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	bool ILogListener.CanSave => false;

	void IDisposable.Dispose()
	{
		GC.SuppressFinalize(this);
	}

	void IPersistable.Load(SettingsStorage storage)
	{
	}

	void IPersistable.Save(SettingsStorage storage)
	{
	}

	void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
	{
		foreach (var message in messages)
		{
			switch (message.Level)
			{
				//case LogLevels.Inherit:
				//case LogLevels.Off:
				//	break;
				case LogLevels.Verbose:
					_logger.Log(LogLevel.Trace, message.Message);
					break;
				case LogLevels.Debug:
					_logger.Log(LogLevel.Debug, message.Message);
					break;
				case LogLevels.Info:
					_logger.Log(LogLevel.Information, message.Message);
					break;
				case LogLevels.Warning:
					_logger.Log(LogLevel.Warning, message.Message);
					break;
				case LogLevels.Error:
					_logger.Log(LogLevel.Error, message.Message);
					break;
				//default:
				//	break;
			}
		}
	}
}