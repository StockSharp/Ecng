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
	public void LogManager_WritesWhatIsPendingWhenDisposed()
	{
		var (fs, dataDir) = Config.CreateFs(nameof(MemoryFileSystem));

		var manager = ServicePath.CreateLogManager(fs, dataDir, LogLevels.Info);

		// Long enough that only the stop itself can deliver the line: a service that fails at start
		// is torn down before its first flush.
		manager.FlushInterval = TimeSpan.FromHours(1);

		manager.Application.AddErrorLog("the reason the service stopped");
		manager.Dispose();

		ReadLogs(fs, dataDir).Contains("the reason the service stopped").AssertTrue();
	}
}
