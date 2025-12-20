namespace Ecng.Logging;

using System.Text;

using Ecng.Localization;

/// <summary>
/// The strategy logger that records the data to the debug window.
/// </summary>
public class DebugLogListener : LogListener
{
	/// <inheritdoc />
	protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
	{
		var sb = new StringBuilder();

		var currLevel = LogLevels.Info;

		foreach (var message in messages)
		{
			if (message.IsDispose)
			{
				if (sb.Length > 0)
					Dump(currLevel, sb);

				Dispose();
				return;
			}

			if (message.Level != currLevel)
			{
				Dump(currLevel, sb);
				currLevel = message.Level;
			}

			sb.AppendFormat("{0} {1}", message.Source.Name, message.Message).AppendLine();
		}

		if (sb.Length > 0)
			Dump(currLevel, sb);
	}

	private static void Dump(LogLevels level, StringBuilder builder)
	{
		var str = builder.ToString();

		switch (level)
		{
			case LogLevels.Off:
				break;
			case LogLevels.Inherit:
			case LogLevels.Verbose:
			case LogLevels.Debug:
			case LogLevels.Info:
				Trace.TraceInformation(str);
				break;
			case LogLevels.Warning:
				Trace.TraceWarning(str);
				break;
			case LogLevels.Error:
				Trace.TraceError(str);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value.".Localize());
		}

		builder.Clear();
	}
}