namespace Ecng.Logging;

using System.IO;

/// <summary>
/// Provides stream logging functionality by wrapping an underlying stream. 
/// It logs read and write operations using supplied formatting functions and log levels.
/// </summary>
/// <param name="underlying">The underlying stream to wrap.</param>
/// <param name="logs">The logging receiver used to output log messages.</param>
/// <param name="formatRead">A function that formats the read data for logging.</param>
/// <param name="formatWrite">A function that formats the written data for logging.</param>
/// <param name="level">The log level of the log messages.</param>
public class LoggableStream(Stream underlying, ILogReceiver logs,
	Func<byte[], string> formatRead,
	Func<byte[], string> formatWrite,
	LogLevels level)
	: DumpableStream(underlying)
{
	/// <summary>
	/// Flushes the read data buffer to the log using the provided formatting function.
	/// </summary>
	public void FlushRead()
	{
		var arr = GetReadDump();
		logs.AddLog(level, () => formatRead(arr));
	}

	/// <summary>
	/// Flushes the write data buffer to the log using the provided formatting function.
	/// </summary>
	public void FlushWrite()
	{
		var arr = GetWriteDump();
		logs.AddLog(level, () => formatWrite(arr));
	}

	/// <summary>
	/// Clears all buffers and flushes both read and write data to the log.
	/// </summary>
	/// <remarks>
	/// This method overrides the base Flush implementation.
	/// </remarks>
	public override void Flush()
	{
		base.Flush();

		FlushRead();
		FlushWrite();
	}
}
