namespace Ecng.Tests.Logging;

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using Ecng.Logging;
using Ecng.IO;

[TestClass]
public class FileLogListenerTests : BaseTestClass
{
	#region Infrastructure

	private IFileSystem _fs;
	private string _root;
	private FileSystemType _fsType;

	private void InitFs(FileSystemType fsType)
	{
		_fsType = fsType;

		if (fsType == FileSystemType.Local)
		{
			_fs = LocalFileSystem.Instance;
			_root = _fs.GetTempPath();
			_fs.CreateDirectory(_root);
		}
		else
		{
			_fs = new MemoryFileSystem();
			_root = "/logs";
			_fs.CreateDirectory(_root);
		}
	}

	[TestCleanup]
	public void Cleanup()
	{
		if (_fsType == FileSystemType.Local && _fs != null && _root != null)
		{
			try { if (_fs.DirectoryExists(_root)) _fs.DeleteDirectory(_root, true); } catch { }
		}
	}

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

	// Regex for log line with date: "yyyy/MM/dd HH:mm:ss.fff|Level  |Source    |Message"
	private static readonly Regex _logLineWithDateRegex = new(
		@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\|.{7}\|.+\|.+$",
		RegexOptions.Compiled);

	// Regex for log line without date: "HH:mm:ss.fff|Level  |Source    |Message"
	private static readonly Regex _logLineWithoutDateRegex = new(
		@"^\d{2}:\d{2}:\d{2}\.\d{3}\|.{7}\|.+\|.+$",
		RegexOptions.Compiled);

	// Regex for log line with source ID: "....|Level  |Source    |SourceId            |Message"
	private static readonly Regex _logLineWithSourceIdRegex = new(
		@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\|.{7}\|.+\|.+\|.+$",
		RegexOptions.Compiled);

	private string ReadAllText(string path)
	{
		using var stream = _fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	private void WriteAllText(string path, string content)
	{
		using var stream = _fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, Encoding.UTF8);
		writer.Write(content);
	}

	private string[] ReadAllLines(string path)
	{
		var content = ReadAllText(path);
		return content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
	}

	private Dictionary<string, string> ReadZipContents(string zipPath)
	{
		var result = new Dictionary<string, string>();
		using var zipStream = _fs.OpenRead(zipPath);
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
		foreach (var entry in archive.Entries)
		{
			using var entryStream = entry.Open();
			using var reader = new StreamReader(entryStream, Encoding.UTF8);
			result[entry.FullName] = reader.ReadToEnd();
		}
		return result;
	}

	#endregion

	#region Exact Format Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_ExactFormat_WithDate(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("TestSrc");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "exact",
			SeparateByDates = SeparateByDateModes.None // Date included in content
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Warning, "TestMessage")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "exact.txt"));

		lines.Length.AssertEqual(1);

		// Verify exact format: "yyyy/MM/dd HH:mm:ss.fff|Warning|TestSrc   |TestMessage"
		_logLineWithDateRegex.IsMatch(lines[0]).AssertTrue($"Line format mismatch: {lines[0]}");

		// Parse and verify parts
		var parts = lines[0].Split('|');
		parts.Length.AssertEqual(4);

		// DateTime part: "yyyy/MM/dd HH:mm:ss.fff" = 23 chars
		parts[0].Length.AssertEqual(23);

		// Level part: 7 chars padded
		parts[1].Length.AssertEqual(7);
		parts[1].AssertEqual("Warning");

		// Source part: 10 chars padded
		parts[2].Length.AssertEqual(10);
		parts[2].AssertEqual("TestSrc   ");

		// Message part
		parts[3].AssertEqual("TestMessage");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_ExactFormat_WithoutDate(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("Src");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "nodate",
			SeparateByDates = SeparateByDateModes.FileName // Date NOT in content
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Error, "ErrMsg")], CancellationToken);
		}

		var todayFile = DateTime.Today.ToString("yyyy_MM_dd") + "_nodate.txt";
		var lines = ReadAllLines(Path.Combine(_root, todayFile));

		lines.Length.AssertEqual(1);

		// Verify format without date: "HH:mm:ss.fff|Error  |Src       |ErrMsg"
		_logLineWithoutDateRegex.IsMatch(lines[0]).AssertTrue($"Line format mismatch: {lines[0]}");

		var parts = lines[0].Split('|');
		parts.Length.AssertEqual(4);

		// Time part: "HH:mm:ss.fff" = 12 chars
		parts[0].Length.AssertEqual(12);

		// Level: "Error  " (7 chars)
		parts[1].Length.AssertEqual(7);
		parts[1].AssertEqual("Error  ");

		// Source: "Src       " (10 chars)
		parts[2].Length.AssertEqual(10);
		parts[2].AssertEqual("Src       ");

		parts[3].AssertEqual("ErrMsg");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_ExactFormat_InfoLevelEmpty(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("App");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "info",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "InfoMessage")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "info.txt"));
		var parts = lines[0].Split('|');

		// Info level should be empty (7 spaces)
		parts[1].Length.AssertEqual(7);
		parts[1].AssertEqual("       ");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_ExactFormat_WithSourceId(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("Src");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "srcid",
			SeparateByDates = SeparateByDateModes.None,
			WriteSourceId = true
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Debug, "Msg")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "srcid.txt"));

		lines.Length.AssertEqual(1);
		_logLineWithSourceIdRegex.IsMatch(lines[0]).AssertTrue($"Line format mismatch: {lines[0]}");

		var parts = lines[0].Split('|');
		parts.Length.AssertEqual(5);

		// Source ID field contains GUID (36 chars) padded to 20 chars minimum
		// But if GUID is longer, it won't be truncated
		var sourceIdPart = parts[3];
		sourceIdPart.Trim().AssertEqual(src.Id.ToString());

		parts[4].AssertEqual("Msg");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_DateFormat_Correct(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 13, 45, 59, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "date",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "date.txt"));
		var datePart = lines[0].Split('|')[0];

		// By default IsLocalTime=false, so UTC time is used
		var expected = msgTime.ToString("yyyy/MM/dd HH:mm:ss.fff");

		datePart.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_DateFormat_WithLocalTime(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 13, 45, 59, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "local",
			SeparateByDates = SeparateByDateModes.None,
			IsLocalTime = true
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "local.txt"));
		var datePart = lines[0].Split('|')[0];

		// With IsLocalTime=true, time should be converted to local
		var localTime = msgTime.ToLocalTime();
		var expected = localTime.ToString("yyyy/MM/dd HH:mm:ss.fff");

		datePart.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_TimeOnlyFormat_Correct(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 8, 5, 3, 7, DateTimeKind.Utc);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "time",
			SeparateByDates = SeparateByDateModes.FileName
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var todayFile = DateTime.Today.ToString("yyyy_MM_dd") + "_time.txt";
		var lines = ReadAllLines(Path.Combine(_root, todayFile));
		var timePart = lines[0].Split('|')[0];

		// By default IsLocalTime=false
		var expected = msgTime.ToString("HH:mm:ss.fff");

		timePart.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_AllLevels_CorrectPadding(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("S");

		var levels = new[] { LogLevels.Debug, LogLevels.Info, LogLevels.Warning, LogLevels.Error };
		var expectedLevels = new[] { "Debug  ", "       ", "Warning", "Error  " };

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "levels",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			var token = CancellationToken;

			foreach (var level in levels)
			{
				await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, level, $"Msg{level}")], token);
			}
		}

		var lines = ReadAllLines(Path.Combine(_root, "levels.txt"));

		lines.Length.AssertEqual(4);

		for (var i = 0; i < 4; i++)
		{
			var parts = lines[i].Split('|');
			parts[1].AssertEqual(expectedLevels[i], $"Level {levels[i]} padding mismatch");
		}
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_LongSourceName_NotTruncated(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("VeryLongSourceName");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "long",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "M")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "long.txt"));
		var parts = lines[0].Split('|');

		// Source name longer than 10 chars - should still be there (no truncation, but padded to 10 min)
		parts[2].AssertEqual("VeryLongSourceName");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_ShortSourceName_PaddedTo10(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("Ab");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "short",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "M")], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "short.txt"));
		var parts = lines[0].Split('|');

		parts[2].Length.AssertEqual(10);
		parts[2].AssertEqual("Ab        ");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task LogLine_MultipleMessages_ExactOrder(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("Src");
		var baseTime = new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "multi",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([
				new LogMessage(src, baseTime, LogLevels.Info, "First"),
				new LogMessage(src, baseTime.AddSeconds(1), LogLevels.Warning, "Second"),
				new LogMessage(src, baseTime.AddSeconds(2), LogLevels.Error, "Third")
			], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "multi.txt"));

		lines.Length.AssertEqual(3);

		lines[0].Split('|')[3].AssertEqual("First");
		lines[1].Split('|')[3].AssertEqual("Second");
		lines[2].Split('|')[3].AssertEqual("Third");

		// Verify times are in order
		var time1 = lines[0].Split('|')[0];
		var time2 = lines[1].Split('|')[0];
		var time3 = lines[2].Split('|')[0];

		string.Compare(time1, time2, StringComparison.Ordinal).AssertLess(0);
		string.Compare(time2, time3, StringComparison.Ordinal).AssertLess(0);
	}

	#endregion

	#region Basic Write Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WritesToSingleFile_WhenSeparateByDatesNone(FileSystemType fsType)
	{
		InitFs(fsType);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "single",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			var src = new DummySource("src");
			var msg = new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "hello123");

			await listener.WriteMessagesAsync([msg], CancellationToken);
		}

		var file = Path.Combine(_root, "single.txt");
		_fs.FileExists(file).AssertTrue();
		var content = ReadAllText(file);
		content.AssertContains("hello123");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteMessages_CorrectFormat_ContainsAllFields(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("TestSource");
		var messageTime = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "format",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, messageTime, LogLevels.Warning, "Test message content")], CancellationToken);
		}

		var content = ReadAllText(Path.Combine(_root, "format.txt"));

		// Should contain: date/time | level | source | message
		content.AssertContains("|");
		content.AssertContains("Warning");
		content.AssertContains("TestSource");
		content.AssertContains("Test message content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteMessages_MultipleMessages_CorrectOrder(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("src");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "order",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([
				new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "First"),
				new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "Second"),
				new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "Third")
			], CancellationToken);
		}

		var lines = ReadAllLines(Path.Combine(_root, "order.txt"));
		var contentLines = lines.Where(l => !string.IsNullOrEmpty(l)).ToArray();

		contentLines.Length.AssertEqual(3);
		contentLines[0].AssertContains("First");
		contentLines[1].AssertContains("Second");
		contentLines[2].AssertContains("Third");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteMessages_DifferentLogLevels_CorrectlyFormatted(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("src");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "levels",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([
				new LogMessage(src, DateTime.UtcNow, LogLevels.Debug, "debug msg"),
				new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "info msg"),
				new LogMessage(src, DateTime.UtcNow, LogLevels.Warning, "warning msg"),
				new LogMessage(src, DateTime.UtcNow, LogLevels.Error, "error msg")
			], CancellationToken);
		}

		var content = ReadAllText(Path.Combine(_root, "levels.txt"));

		content.AssertContains("Debug");
		content.AssertContains("info msg");
		content.AssertContains("Warning");
		content.AssertContains("Error");
	}

	#endregion

	#region SeparateByDates Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task CreatesDatePrefixedFile_WhenSeparateByDatesFileName(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.FileName,
			DirectoryDateFormat = "yyyy_MM_dd"
		};

		var src = new DummySource("s");
		await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m1")], CancellationToken);

		var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		var path = Path.Combine(_root, todayPref);
		_fs.FileExists(path).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task CreatesSubdirectory_WhenSeparateByDatesSubDirectories(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.SubDirectories,
			DirectoryDateFormat = "yyyy_MM_dd"
		};

		var src = new DummySource("s");
		await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m2")], CancellationToken);

		var sub = Path.Combine(_root, DateTime.Today.ToString("yyyy_MM_dd"));
		_fs.DirectoryExists(sub).AssertTrue();
		var file = Path.Combine(sub, "log" + listener.Extension);
		_fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task SeparateByDatesFileName_DoesNotIncludeDateInContent(FileSystemType fsType)
	{
		InitFs(fsType);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.FileName
		})
		{
			var src = new DummySource("s");
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "test")], CancellationToken);
		}

		var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log.txt";
		var content = ReadAllText(Path.Combine(_root, todayPref));

		// When SeparateByDates is FileName, date should NOT be in log line (only time)
		// The format should be "HH:mm:ss.fff|..." not "yyyy/MM/dd HH:mm:ss.fff|..."
		var lines = content.Split('\n').Where(l => !string.IsNullOrEmpty(l)).ToArray();
		lines.Length.AssertGreater(0);
		// First char should be digit (hour), not year
		char.IsDigit(lines[0][0]).AssertTrue();
		// Should not contain date separator in the beginning
		lines[0].StartsWith(DateTime.Today.ToString("yyyy")).AssertFalse();
	}

	#endregion

	#region Rolling (MaxLength/MaxCount) Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Rolling_CreatesRollingFiles_WhenMaxLengthExceeded(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "rot",
			MaxLength = 100,
			MaxCount = 3
		};

		var token = CancellationToken;

		var src = new DummySource("s");

		// Write enough to trigger multiple rollovers
		for (var i = 0; i < 50; i++)
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, $"Message number {i:D3}")], token);
		}

		var baseFile = Path.Combine(_root, "rot.txt");
		var f1 = Path.Combine(_root, "rot.1.txt");
		var f2 = Path.Combine(_root, "rot.2.txt");

		_fs.FileExists(baseFile).AssertTrue("Base file should exist");
		_fs.FileExists(f1).AssertTrue("Rolling file .1 should exist");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Rolling_RespectMaxCount_DeletesOldFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "rot",
			MaxLength = 50, // Very small to force many rollovers
			MaxCount = 2
		};

		var token = CancellationToken;

		var src = new DummySource("s");

		// Write many messages to force multiple rollovers
		for (var i = 0; i < 100; i++)
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, $"Msg{i:D3}")], token);
		}

		var baseFile = Path.Combine(_root, "rot.txt");
		var f1 = Path.Combine(_root, "rot.1.txt");
		var f2 = Path.Combine(_root, "rot.2.txt");
		var f3 = Path.Combine(_root, "rot.3.txt");

		_fs.FileExists(baseFile).AssertTrue("Base file should exist");
		// With MaxCount=2, files .3 and beyond should be deleted
		_fs.FileExists(f3).AssertFalse("File .3 should be deleted due to MaxCount=2");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Rolling_NewestDataInBaseFile(FileSystemType fsType)
	{
		InitFs(fsType);

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "rot",
			MaxLength = 500, // Larger to ensure some data stays in base file
			MaxCount = 5
		})
		{
			var src = new DummySource("s");

			var token = CancellationToken;

			// Write messages with identifiable content
			for (var i = 1; i <= 10; i++)
			{
				await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, $"MSG_{i:D3}")], token);
			}
		}

		var baseFile = Path.Combine(_root, "rot.txt");

		_fs.FileExists(baseFile).AssertTrue("Base file should exist");

		var baseContent = ReadAllText(baseFile);
		var f1 = Path.Combine(_root, "rot.1.txt");
		var inBase = baseContent.Contains("MSG_010");
		var inF1 = _fs.FileExists(f1) && ReadAllText(f1).Contains("MSG_010");

		// The newest message should be present either in the base file
		// or in the first rolling file, depending on rollover timing.
		(inBase || inF1).AssertTrue("Latest message not found in base or .1 file");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Rolling_NoRollingWhenMaxLengthZero(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "noroll",
			MaxLength = 0, // Disabled
			MaxCount = 2
		};

		var token = CancellationToken;

		var src = new DummySource("s");

		for (var i = 0; i < 100; i++)
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, $"Message {i}")], token);
		}

		var baseFile = Path.Combine(_root, "noroll.txt");
		var f1 = Path.Combine(_root, "noroll.1.txt");

		_fs.FileExists(baseFile).AssertTrue();
		_fs.FileExists(f1).AssertFalse("No rolling should occur when MaxLength=0");
	}

	#endregion

	#region History Policy - Delete Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Delete_RemovesOldFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var threeDaysAgo = DateTime.Today.AddDays(-3);

		var oldFile1 = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		var oldFile2 = Path.Combine(_root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(oldFile1, "old content 1");
		WriteAllText(oldFile2, "old content 2");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		_fs.FileExists(oldFile1).AssertFalse("Old file 1 should be deleted");
		_fs.FileExists(oldFile2).AssertFalse("Old file 2 should be deleted");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Delete_RemovesOldDirectories(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDir = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd"));
		_fs.CreateDirectory(oldDir);
		WriteAllText(Path.Combine(oldDir, "log.txt"), "old log");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		_fs.DirectoryExists(oldDir).AssertFalse("Old directory should be deleted");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Delete_KeepsRecentFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		// File from today should NOT be deleted
		var todayFile = Path.Combine(_root, DateTime.Today.ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(todayFile, "today content");

		// Old file should be deleted
		var oldFile = Path.Combine(_root, DateTime.Today.AddDays(-5).ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(oldFile, "old content");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		_fs.FileExists(todayFile).AssertTrue("Today's file should be kept");
		_fs.FileExists(oldFile).AssertFalse("Old file should be deleted");
	}

	#endregion

	#region History Policy - Compression Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Compression_CreatesZipAndDeletesOriginal(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(_root, oldName);
		WriteAllText(oldPath, "original content for compression");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");

		_fs.FileExists(zipPath).AssertTrue("Zip file should be created");
		_fs.FileExists(oldPath).AssertFalse("Original file should be deleted after compression");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Compression_ZipContainsCorrectContent(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(_root, oldName);
		var originalContent = "This is the original log content\nLine 2\nLine 3";
		WriteAllText(oldPath, originalContent);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");
		var zipContents = ReadZipContents(zipPath);

		zipContents.Count.AssertEqual(1);
		zipContents.ContainsKey(oldName).AssertTrue("Zip should contain file with original name");
		zipContents[oldName].AssertEqual(originalContent);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Compression_DirectoryToZip(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDir = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd"));
		_fs.CreateDirectory(oldDir);

		WriteAllText(Path.Combine(oldDir, "log1.txt"), "content 1");
		WriteAllText(Path.Combine(oldDir, "log2.txt"), "content 2");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + ".zip");

		_fs.FileExists(zipPath).AssertTrue("Zip file should be created from directory");
		_fs.DirectoryExists(oldDir).AssertFalse("Original directory should be deleted");

		var zipContents = ReadZipContents(zipPath);
		zipContents.Count.AssertEqual(2);
		zipContents.Values.Any(v => v == "content 1").AssertTrue();
		zipContents.Values.Any(v => v == "content 2").AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Compression_MultipleFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var threeDaysAgo = DateTime.Today.AddDays(-3);

		var oldFile1 = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		var oldFile2 = Path.Combine(_root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(oldFile1, "content 1");
		WriteAllText(oldFile2, "content 2");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zip1 = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");
		var zip2 = Path.Combine(_root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");

		_fs.FileExists(zip1).AssertTrue("First zip should be created");
		_fs.FileExists(zip2).AssertTrue("Second zip should be created");
		_fs.FileExists(oldFile1).AssertFalse();
		_fs.FileExists(oldFile2).AssertFalse();
	}

	#endregion

	#region History Policy - Move Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Move_FilesMovedToHistory(FileSystemType fsType)
	{
		InitFs(fsType);

		var history = Path.Combine(_root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(_root, oldName);
		WriteAllText(oldPath, "to be moved");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedPath = Path.Combine(history, oldName);

		_fs.FileExists(movedPath).AssertTrue("File should be moved to history");
		_fs.FileExists(oldPath).AssertFalse("Original file should not exist");

		ReadAllText(movedPath).AssertEqual("to be moved");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Move_DirectoriesMovedToHistory(FileSystemType fsType)
	{
		InitFs(fsType);

		var history = Path.Combine(_root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDirName = twoDaysAgo.ToString("yyyy_MM_dd");
		var oldDir = Path.Combine(_root, oldDirName);
		_fs.CreateDirectory(oldDir);
		WriteAllText(Path.Combine(oldDir, "log.txt"), "dir content");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedDir = Path.Combine(history, oldDirName);

		_fs.DirectoryExists(movedDir).AssertTrue("Directory should be moved to history");
		_fs.DirectoryExists(oldDir).AssertFalse("Original directory should not exist");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_Move_PreservesContent(FileSystemType fsType)
	{
		InitFs(fsType);

		var history = Path.Combine(_root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDirName = twoDaysAgo.ToString("yyyy_MM_dd");
		var oldDir = Path.Combine(_root, oldDirName);
		_fs.CreateDirectory(oldDir);

		var originalContent1 = "Log entry 1\nLog entry 2";
		var originalContent2 = "Another log file";
		WriteAllText(Path.Combine(oldDir, "app.txt"), originalContent1);
		WriteAllText(Path.Combine(oldDir, "error.txt"), originalContent2);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedDir = Path.Combine(history, oldDirName);

		ReadAllText(Path.Combine(movedDir, "app.txt")).AssertEqual(originalContent1);
		ReadAllText(Path.Combine(movedDir, "error.txt")).AssertEqual(originalContent2);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task TryDoHistoryPolicy_MoveDirectories(FileSystemType fsType)
	{
		InitFs(fsType);

		var yesterday = DateTime.Today.AddDays(-1);
		var dayBefore = DateTime.Today.AddDays(-2);

		var dir1 = Path.Combine(_root, yesterday.ToString("yyyy_MM_dd"));
		var dir2 = Path.Combine(_root, dayBefore.ToString("yyyy_MM_dd"));
		_fs.CreateDirectory(dir1);
		_fs.CreateDirectory(dir2);

		WriteAllText(Path.Combine(dir1, "test.txt"), "test1");
		WriteAllText(Path.Combine(dir2, "test.txt"), "test2");

		var historyDir = Path.Combine(_root, "history");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyDir
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("test"), DateTime.UtcNow, LogLevels.Info, "m")], CancellationToken);

		var moved1 = Path.Combine(historyDir, Path.GetFileName(dir1));
		var moved2 = Path.Combine(historyDir, Path.GetFileName(dir2));

		_fs.DirectoryExists(moved1).AssertTrue("First directory was not moved to history.");
		_fs.DirectoryExists(moved2).AssertTrue("Second directory was not moved to history.");
		_fs.DirectoryExists(dir1).AssertFalse();
		_fs.DirectoryExists(dir2).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task TryDoHistoryPolicy_MoveFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		var yesterday = DateTime.Today.AddDays(-1);
		var dayBefore = DateTime.Today.AddDays(-2);

		var file1 = Path.Combine(_root, yesterday.ToString("yyyy_MM_dd") + "_log.txt");
		var file2 = Path.Combine(_root, dayBefore.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(file1, "a");
		WriteAllText(file2, "b");

		var historyPath = Path.Combine(_root, "history");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyPath
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("test"), DateTime.UtcNow, LogLevels.Info, "m")], CancellationToken);

		var moved1 = Path.Combine(historyPath, Path.GetFileName(file1));
		var moved2 = Path.Combine(historyPath, Path.GetFileName(file2));

		_fs.FileExists(moved1).AssertTrue("First file was not moved to history.");
		_fs.FileExists(moved2).AssertTrue("Second file was not moved to history.");
	}

	#endregion

	#region History Policy - None Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task HistoryPolicy_None_DoesNothing(FileSystemType fsType)
	{
		InitFs(fsType);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldFile = Path.Combine(_root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(oldFile, "old content");

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.None,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		_fs.FileExists(oldFile).AssertTrue("Old file should NOT be touched when HistoryPolicy=None");
	}

	#endregion

	#region Append Mode Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task AppendMode_AppendsToExistingFile(FileSystemType fsType)
	{
		InitFs(fsType);

		var file = Path.Combine(_root, "app.txt");
		WriteAllText(file, "start\n");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "app",
			Append = true
		})
		{
			var src = new DummySource("s");
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "more")], CancellationToken);
		}

		var content = ReadAllText(file);
		content.AssertContains("start");
		content.AssertContains("more");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task AppendMode_False_OverwritesFile(FileSystemType fsType)
	{
		InitFs(fsType);

		var file = Path.Combine(_root, "overwrite.txt");
		WriteAllText(file, "original content that should be gone");

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "overwrite",
			Append = false
		})
		{
			var src = new DummySource("s");
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "new content")], CancellationToken);
		}

		var content = ReadAllText(file);
		content.Contains("original content").AssertFalse("Original content should be overwritten");
		content.AssertContains("new content");
	}

	#endregion

	#region Source Name Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteChildDataToRootFile_UsesParentName(FileSystemType fsType)
	{
		InitFs(fsType);

		var parent = new DummySource("parent") { IsRoot = true };
		var child = new DummySource("child") { Parent = parent };

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.None,
			WriteChildDataToRootFile = true
		};

		await listener.WriteMessagesAsync([new LogMessage(child, DateTime.UtcNow, LogLevels.Info, "cmsg")], CancellationToken);

		var file = Path.Combine(_root, "parent" + listener.Extension);
		_fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteChildDataToRootFile_False_UsesChildName(FileSystemType fsType)
	{
		InitFs(fsType);

		var parent = new DummySource("parent") { IsRoot = true };
		var child = new DummySource("child") { Parent = parent };

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.None,
			WriteChildDataToRootFile = false
		};

		await listener.WriteMessagesAsync([new LogMessage(child, DateTime.UtcNow, LogLevels.Info, "cmsg")], CancellationToken);

		var childFile = Path.Combine(_root, "child" + listener.Extension);
		var parentFile = Path.Combine(_root, "parent" + listener.Extension);

		_fs.FileExists(childFile).AssertTrue("Child file should be created");
		_fs.FileExists(parentFile).AssertFalse("Parent file should not be created");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task WriteSourceId_IncludesSourceIdInLog(FileSystemType fsType)
	{
		InitFs(fsType);

		var src = new DummySource("s");
		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "sid",
			WriteSourceId = true
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "mm")], CancellationToken);
		}

		var file = Path.Combine(_root, "sid.txt");
		var content = ReadAllText(file);
		content.AssertContains(src.Id.ToString());
	}

	#endregion

	#region Extension and Filename Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task CustomExtension_IsApplied(FileSystemType fsType)
	{
		InitFs(fsType);

		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = "e",
			Extension = ".logx"
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "x")], CancellationToken);

		var file = Path.Combine(_root, "e.logx");
		_fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetFileName_SanitizesInvalidChars(FileSystemType fsType)
	{
		InitFs(fsType);

		var bad = "bad:name*?";
		using var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			FileName = bad
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "z")], CancellationToken);

		var expected = new string([.. bad.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)]) + listener.Extension;
		_fs.FileExists(Path.Combine(_root, expected)).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task MultipleSourcesCreateSeparateFiles(FileSystemType fsType)
	{
		InitFs(fsType);

		var src1 = new DummySource("Source1") { IsRoot = true };
		var src2 = new DummySource("Source2") { IsRoot = true };

		using (var listener = new FileLogListener(_fs)
		{
			LogDirectory = _root,
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([
				new LogMessage(src1, DateTime.UtcNow, LogLevels.Info, "from source 1"),
				new LogMessage(src2, DateTime.UtcNow, LogLevels.Info, "from source 2")
			], CancellationToken);
		}

		var file1 = Path.Combine(_root, "Source1.txt");
		var file2 = Path.Combine(_root, "Source2.txt");

		_fs.FileExists(file1).AssertTrue();
		_fs.FileExists(file2).AssertTrue();

		ReadAllText(file1).AssertContains("from source 1");
		ReadAllText(file2).AssertContains("from source 2");
	}

	#endregion
}
