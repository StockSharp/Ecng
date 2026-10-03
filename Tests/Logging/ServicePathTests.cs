namespace Ecng.Tests.Logging;

using Ecng.IO;
using Ecng.Logging;
using Ecng.Server.Utils;

/// <summary>
/// The log a service keeps through <see cref="ServicePath"/>.
/// </summary>
[TestClass]
public class ServicePathTests : BaseTestClass
{
	private static string ReadLogs(IFileSystem fs, string dataDir)
	{
		var logs = Path.Combine(dataDir, "Logs");

		if (!fs.DirectoryExists(logs))
			return string.Empty;

		var text = new StringBuilder();

		foreach (var file in fs.EnumerateFiles(logs, "*", SearchOption.AllDirectories))
		{
			using var reader = new StreamReader(fs.OpenRead(file), Encoding.UTF8);
			text.Append(reader.ReadToEnd());
		}

		return text.ToString();
	}

	[TestMethod]
	public async Task LogManager_WritesWhatIsPendingWhenDisposed()
	{
		var (fs, dataDir) = Config.CreateFs(nameof(MemoryFileSystem));

		var manager = await ServicePath.CreateLogManagerAsync(fs, dataDir, LogLevels.Info, CancellationToken);

		// Long enough that only the stop itself can deliver the line: a service that fails at start
		// is torn down before its first flush.
		manager.FlushInterval = TimeSpan.FromHours(1);

		manager.Application.AddErrorLog("the reason the service stopped");
		manager.Dispose();

		ReadLogs(fs, dataDir).Contains("the reason the service stopped").AssertTrue();
	}

	[TestMethod]
	public async Task TheSettingsWrittenAtTheFirstStartAreReadAtTheNext()
	{
		var (fs, dataDir) = Config.CreateFs(nameof(MemoryFileSystem));

		using (var first = await ServicePath.CreateLogManagerAsync(fs, dataDir, LogLevels.Warning, CancellationToken))
			first.Listeners.Count.AssertEqual(1, "the first start sets up the file listener");

		using var second = await ServicePath.CreateLogManagerAsync(fs, dataDir, LogLevels.Info, CancellationToken);

		second.Listeners.Count.AssertEqual(1, "the next start reads the listener back rather than adding another");
		second.Application.LogLevel.AssertEqual(LogLevels.Warning, "the level is the one stored, not the default passed now");
	}
}
