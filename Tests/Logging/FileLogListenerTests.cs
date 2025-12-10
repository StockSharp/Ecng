namespace Ecng.Tests.Logging;

using System.Text;

using Ecng.Common;
using Ecng.Logging;

[TestClass]
public class FileLogListenerTests
{
	private class DummySource(string name) : Disposable, ILogSource
	{
		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = name;
		public ILogSource Parent { get; set; }
		public event Action<ILogSource> ParentRemoved { add { } remove { } }
		public LogLevels LogLevel { get; set; } = LogLevels.Info;
		public DateTimeOffset CurrentTime => CurrentTimeUtc;
		public DateTime CurrentTimeUtc => DateTime.UtcNow;
		public bool IsRoot { get; set; }

		public event Action<LogMessage> Log { add { } remove { } }
	}

	private static string ReadAllText(IFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	private static void WriteAllText(IFileSystem fs, string path, string content)
	{
		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, Encoding.UTF8);
		writer.Write(content);
	}

	[TestMethod]
	public void TryDoHistoryPolicy_MoveDirectories()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		// create two dated subdirectories older than HistoryAfter (yesterday and day before)
		var yesterday = DateTime.Today.AddDays(-1);
		var dayBefore = DateTime.Today.AddDays(-2);

		var dir1 = Path.Combine(root, yesterday.ToString("yyyy_MM_dd"));
		var dir2 = Path.Combine(root, dayBefore.ToString("yyyy_MM_dd"));
		fs.CreateDirectory(dir1);
		fs.CreateDirectory(dir2);

		// Add a file to each directory so they're not empty
		WriteAllText(fs, Path.Combine(dir1, "test.txt"), "test1");
		WriteAllText(fs, Path.Combine(dir2, "test.txt"), "test2");

		// create a simple message to trigger TryDoHistoryPolicy via WriteMessages
		var src = new DummySource("test");
		var msg = new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m");

		var historyDir = Path.Combine(root, "history");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyDir
		};

		listener.WriteMessages([msg]);

		var moved1 = Path.Combine(historyDir, Path.GetFileName(dir1));
		var moved2 = Path.Combine(historyDir, Path.GetFileName(dir2));

		fs.DirectoryExists(moved1).AssertTrue("First directory was not moved to history.");
		fs.DirectoryExists(moved2).AssertTrue("Second directory was not moved to history.");
		fs.DirectoryExists(dir1).AssertFalse();
		fs.DirectoryExists(dir2).AssertFalse();
	}

	[TestMethod]
	public void TryDoHistoryPolicy_MoveFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var yesterday = DateTime.Today.AddDays(-1);
		var dayBefore = DateTime.Today.AddDays(-2);

		var file1 = Path.Combine(root, yesterday.ToString("yyyy_MM_dd") + "_log.txt");
		var file2 = Path.Combine(root, dayBefore.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(fs, file1, "a");
		WriteAllText(fs, file2, "b");

		var src = new DummySource("test");
		var msg = new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m");

		var historyPath = Path.Combine(root, "history");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyPath
		};

		listener.WriteMessages([msg]);

		// verify files moved into history folder
		var moved1 = Path.Combine(historyPath, Path.GetFileName(file1));
		var moved2 = Path.Combine(historyPath, Path.GetFileName(file2));

		fs.FileExists(moved1).AssertTrue("First file was not moved to history.");
		fs.FileExists(moved2).AssertTrue("Second file was not moved to history.");
	}

	[TestMethod]
	public void WritesToSingleFile_WhenSeparateByDatesNone()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "single",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			var src = new DummySource("src");
			var msg = new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "hello123");

			listener.WriteMessages([msg]);
		}

		var file = Path.Combine(root, "single.txt");
		fs.FileExists(file).AssertTrue();
		var content = ReadAllText(fs, file);
		content.AssertContains("hello123");
	}

	[TestMethod]
	public void CreatesDatePrefixedFile_WhenSeparateByDatesFileName()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.FileName,
			DirectoryDateFormat = "yyyy_MM_dd"
		};

		var src = new DummySource("s");
		listener.WriteMessages([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m1")]);

		var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		var path = Path.Combine(root, todayPref);
		fs.FileExists(path).AssertTrue();
	}

	[TestMethod]
	public void CreatesSubdirectory_WhenSeparateByDatesSubDirectories()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.SubDirectories,
			DirectoryDateFormat = "yyyy_MM_dd"
		};

		var src = new DummySource("s");
		listener.WriteMessages([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m2")]);

		var sub = Path.Combine(root, DateTime.Today.ToString("yyyy_MM_dd"));
		fs.DirectoryExists(sub).AssertTrue();
		var file = Path.Combine(sub, "log" + listener.Extension);
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public void RotationBySizeAndCount_CreatesRollingFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "rot",
			MaxLength = 100, // small
			MaxCount = 2
		};

		var src = new DummySource("s");
		for (var i = 0; i < 200; i++)
		{
			listener.WriteMessages([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, new string('x', 20))]);
		}

		var baseFile = Path.Combine(root, "rot" + listener.Extension);
		var f1 = Path.Combine(root, "rot.1" + listener.Extension);
		var f2 = Path.Combine(root, "rot.2" + listener.Extension);

		fs.FileExists(baseFile).AssertTrue();
		(fs.FileExists(f1) || fs.FileExists(f2)).AssertTrue();
	}

	[TestMethod]
	public void AppendMode_AppendsToExistingFile()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var file = Path.Combine(root, "app.txt");
		WriteAllText(fs, file, "start\n");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "app",
			Append = true
		})
		{
			var src = new DummySource("s");
			listener.WriteMessages([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "more")]);
		}

		var content = ReadAllText(fs, file);
		content.AssertContains("start");
		content.AssertContains("more");
	}

	[TestMethod]
	public void WriteChildDataToRootFile_UsesParentName()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var parent = new DummySource("parent") { IsRoot = true };
		var child = new DummySource("child") { Parent = parent };

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.None,
			WriteChildDataToRootFile = true
		};

		listener.WriteMessages([new LogMessage(child, DateTime.UtcNow, LogLevels.Info, "cmsg")]);

		var file = Path.Combine(root, "parent" + listener.Extension);
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public void WriteSourceId_IncludesSourceIdInLog()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("s");
		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "sid",
			WriteSourceId = true
		})
		{
			listener.WriteMessages([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "mm")]);
		}

		var file = Path.Combine(root, "sid.txt");
		var content = ReadAllText(fs, file);
		content.AssertContains(src.Id.ToString());
	}

	[TestMethod]
	public void CustomExtension_IsApplied()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "e",
			Extension = ".logx"
		};

		listener.WriteMessages([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "x")]);

		var file = Path.Combine(root, "e.logx");
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public void GetFileName_SanitizesInvalidChars()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var bad = "bad:name*?";
		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = bad
		};

		listener.WriteMessages([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "z")]);

		// Expect file created with invalid chars replaced by '_'
		var expected = new string([.. bad.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)]) + listener.Extension;
		fs.FileExists(Path.Combine(root, expected)).AssertTrue();
	}

	[TestMethod]
	public void HistoryPolicy_Delete_RemovesOldFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		var old = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		WriteAllText(fs, Path.Combine(root, old), "old");

		listener.WriteMessages([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "t")]);

		fs.FileExists(Path.Combine(root, old)).AssertFalse();
	}

	[TestMethod]
	public void HistoryPolicy_Compression_CreatesZipAndDeletesOriginal()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		var oldName = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		var oldPath = Path.Combine(root, oldName);
		WriteAllText(fs, oldPath, "old");

		listener.WriteMessages([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "t")]);

		var zipPath = Path.Combine(root, Path.GetFileNameWithoutExtension(oldPath) + ".zip");
		fs.FileExists(zipPath).AssertTrue();
		fs.FileExists(oldPath).AssertFalse();
	}

	[TestMethod]
	public void HistoryPolicy_Move_FilesMovedToHistory()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var history = Path.Combine(root, "history");
		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		var oldName = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		var oldPath = Path.Combine(root, oldName);
		WriteAllText(fs, oldPath, "old");

		listener.WriteMessages([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "t")]);

		var moved = Path.Combine(history, Path.GetFileName(oldPath));
		fs.FileExists(moved).AssertTrue();
	}
}