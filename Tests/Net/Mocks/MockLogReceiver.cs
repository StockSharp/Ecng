namespace Ecng.Tests.Net;

using System.Collections.Concurrent;

using Ecng.Logging;

class MockLogReceiver : BaseLogReceiver
{
	public ConcurrentBag<LogMessage> LoggedMessages { get; } = [];

	public MockLogReceiver()
	{
		Log += OnLogMessage;
	}

	private void OnLogMessage(LogMessage message)
	{
		LoggedMessages.Add(message);
	}

	public bool HasErrors => LoggedMessages.Any(m => m.Level == LogLevels.Error);
	public IEnumerable<LogMessage> Errors => LoggedMessages.Where(m => m.Level == LogLevels.Error);
}
