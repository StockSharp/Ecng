namespace Ecng.Tests.Logging;

using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

using Ecng.Logging;
using Ecng.IO;

[TestClass]
public class FileLogListenerTests : BaseTestClass
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

	private static string[] ReadAllLines(IFileSystem fs, string path)
	{
		var content = ReadAllText(fs, path);
		return content.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
	}

	private static Dictionary<string, string> ReadZipContents(IFileSystem fs, string zipPath)
	{
		var result = new Dictionary<string, string>();
		using var zipStream = fs.OpenRead(zipPath);
		using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
		foreach (var entry in archive.Entries)
		{
			using var entryStream = entry.Open();
			using var reader = new StreamReader(entryStream, Encoding.UTF8);
			result[entry.FullName] = reader.ReadToEnd();
		}
		return result;
	}

	#region Exact Format Tests

	[TestMethod]
	public async Task LogLine_ExactFormat_WithDate()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("TestSrc");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "exact",
			SeparateByDates = SeparateByDateModes.None // Date included in content
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Warning, "TestMessage")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "exact.txt"));

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
	public async Task LogLine_ExactFormat_WithoutDate()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("Src");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "nodate",
			SeparateByDates = SeparateByDateModes.FileName // Date NOT in content
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Error, "ErrMsg")], CancellationToken);
		}

		var todayFile = DateTime.Today.ToString("yyyy_MM_dd") + "_nodate.txt";
		var lines = ReadAllLines(fs, Path.Combine(root, todayFile));

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
	public async Task LogLine_ExactFormat_InfoLevelEmpty()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("App");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "info",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "InfoMessage")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "info.txt"));
		var parts = lines[0].Split('|');

		// Info level should be empty (7 spaces)
		parts[1].Length.AssertEqual(7);
		parts[1].AssertEqual("       ");
	}

	[TestMethod]
	public async Task LogLine_ExactFormat_WithSourceId()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("Src");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "srcid",
			SeparateByDates = SeparateByDateModes.None,
			WriteSourceId = true
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Debug, "Msg")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "srcid.txt"));

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
	public async Task LogLine_DateFormat_Correct()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 13, 45, 59, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "date",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "date.txt"));
		var datePart = lines[0].Split('|')[0];

		// By default IsLocalTime=false, so UTC time is used
		var expected = msgTime.ToString("yyyy/MM/dd HH:mm:ss.fff");

		datePart.AssertEqual(expected);
	}

	[TestMethod]
	public async Task LogLine_DateFormat_WithLocalTime()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 13, 45, 59, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "local",
			SeparateByDates = SeparateByDateModes.None,
			IsLocalTime = true
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "local.txt"));
		var datePart = lines[0].Split('|')[0];

		// With IsLocalTime=true, time should be converted to local
		var localTime = msgTime.ToLocalTime();
		var expected = localTime.ToString("yyyy/MM/dd HH:mm:ss.fff");

		datePart.AssertEqual(expected);
	}

	[TestMethod]
	public async Task LogLine_TimeOnlyFormat_Correct()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("S");
		var msgTime = new DateTime(2024, 12, 25, 8, 5, 3, 7, DateTimeKind.Utc);

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "time",
			SeparateByDates = SeparateByDateModes.FileName
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, msgTime, LogLevels.Info, "X")], CancellationToken);
		}

		var todayFile = DateTime.Today.ToString("yyyy_MM_dd") + "_time.txt";
		var lines = ReadAllLines(fs, Path.Combine(root, todayFile));
		var timePart = lines[0].Split('|')[0];

		// By default IsLocalTime=false
		var expected = msgTime.ToString("HH:mm:ss.fff");

		timePart.AssertEqual(expected);
	}

	[TestMethod]
	public async Task LogLine_AllLevels_CorrectPadding()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("S");

		var levels = new[] { LogLevels.Debug, LogLevels.Info, LogLevels.Warning, LogLevels.Error };
		var expectedLevels = new[] { "Debug  ", "       ", "Warning", "Error  " };

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var lines = ReadAllLines(fs, Path.Combine(root, "levels.txt"));

		lines.Length.AssertEqual(4);

		for (var i = 0; i < 4; i++)
		{
			var parts = lines[i].Split('|');
			parts[1].AssertEqual(expectedLevels[i], $"Level {levels[i]} padding mismatch");
		}
	}

	[TestMethod]
	public async Task LogLine_LongSourceName_NotTruncated()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("VeryLongSourceName");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "long",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "M")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "long.txt"));
		var parts = lines[0].Split('|');

		// Source name longer than 10 chars - should still be there (no truncation, but padded to 10 min)
		parts[2].AssertEqual("VeryLongSourceName");
	}

	[TestMethod]
	public async Task LogLine_ShortSourceName_PaddedTo10()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("Ab");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "short",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "M")], CancellationToken);
		}

		var lines = ReadAllLines(fs, Path.Combine(root, "short.txt"));
		var parts = lines[0].Split('|');

		parts[2].Length.AssertEqual(10);
		parts[2].AssertEqual("Ab        ");
	}

	[TestMethod]
	public async Task LogLine_MultipleMessages_ExactOrder()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("Src");
		var baseTime = new DateTime(2024, 1, 15, 10, 0, 0, 0, DateTimeKind.Utc);

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var lines = ReadAllLines(fs, Path.Combine(root, "multi.txt"));

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
	public async Task WritesToSingleFile_WhenSeparateByDatesNone()
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

			await listener.WriteMessagesAsync([msg], CancellationToken);
		}

		var file = Path.Combine(root, "single.txt");
		fs.FileExists(file).AssertTrue();
		var content = ReadAllText(fs, file);
		content.AssertContains("hello123");
	}

	[TestMethod]
	public async Task WriteMessages_CorrectFormat_ContainsAllFields()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("TestSource");
		var messageTime = new DateTime(2024, 6, 15, 14, 30, 45, 123, DateTimeKind.Utc);

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "format",
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([new LogMessage(src, messageTime, LogLevels.Warning, "Test message content")], CancellationToken);
		}

		var content = ReadAllText(fs, Path.Combine(root, "format.txt"));

		// Should contain: date/time | level | source | message
		content.AssertContains("|");
		content.AssertContains("Warning");
		content.AssertContains("TestSource");
		content.AssertContains("Test message content");
	}

	[TestMethod]
	public async Task WriteMessages_MultipleMessages_CorrectOrder()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("src");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var lines = ReadAllLines(fs, Path.Combine(root, "order.txt"));
		var contentLines = lines.Where(l => !string.IsNullOrEmpty(l)).ToArray();

		contentLines.Length.AssertEqual(3);
		contentLines[0].AssertContains("First");
		contentLines[1].AssertContains("Second");
		contentLines[2].AssertContains("Third");
	}

	[TestMethod]
	public async Task WriteMessages_DifferentLogLevels_CorrectlyFormatted()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src = new DummySource("src");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var content = ReadAllText(fs, Path.Combine(root, "levels.txt"));

		content.AssertContains("Debug");
		content.AssertContains("info msg");
		content.AssertContains("Warning");
		content.AssertContains("Error");
	}

	#endregion

	#region SeparateByDates Tests

	[TestMethod]
	public async Task CreatesDatePrefixedFile_WhenSeparateByDatesFileName()
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
		await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m1")], CancellationToken);

		var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log" + listener.Extension;
		var path = Path.Combine(root, todayPref);
		fs.FileExists(path).AssertTrue();
	}

	[TestMethod]
	public async Task CreatesSubdirectory_WhenSeparateByDatesSubDirectories()
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
		await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "m2")], CancellationToken);

		var sub = Path.Combine(root, DateTime.Today.ToString("yyyy_MM_dd"));
		fs.DirectoryExists(sub).AssertTrue();
		var file = Path.Combine(sub, "log" + listener.Extension);
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public async Task SeparateByDatesFileName_DoesNotIncludeDateInContent()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "log",
			SeparateByDates = SeparateByDateModes.FileName
		})
		{
			var src = new DummySource("s");
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "test")], CancellationToken);
		}

		var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log.txt";
		var content = ReadAllText(fs, Path.Combine(root, todayPref));

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
	public async Task Rolling_CreatesRollingFiles_WhenMaxLengthExceeded()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var baseFile = Path.Combine(root, "rot.txt");
		var f1 = Path.Combine(root, "rot.1.txt");
		var f2 = Path.Combine(root, "rot.2.txt");

		fs.FileExists(baseFile).AssertTrue("Base file should exist");
		fs.FileExists(f1).AssertTrue("Rolling file .1 should exist");
	}

	[TestMethod]
	public async Task Rolling_RespectMaxCount_DeletesOldFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var baseFile = Path.Combine(root, "rot.txt");
		var f1 = Path.Combine(root, "rot.1.txt");
		var f2 = Path.Combine(root, "rot.2.txt");
		var f3 = Path.Combine(root, "rot.3.txt");

		fs.FileExists(baseFile).AssertTrue("Base file should exist");
		// With MaxCount=2, files .3 and beyond should be deleted
		fs.FileExists(f3).AssertFalse("File .3 should be deleted due to MaxCount=2");
	}

	[TestMethod]
	public async Task Rolling_NewestDataInBaseFile()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var baseFile = Path.Combine(root, "rot.txt");

		fs.FileExists(baseFile).AssertTrue("Base file should exist");

		var baseContent = ReadAllText(fs, baseFile);
		var f1 = Path.Combine(root, "rot.1.txt");
		var inBase = baseContent.Contains("MSG_010");
		var inF1 = fs.FileExists(f1) && ReadAllText(fs, f1).Contains("MSG_010");

		// The newest message should be present either in the base file
		// or in the first rolling file, depending on rollover timing.
		(inBase || inF1).AssertTrue("Latest message not found in base or .1 file");
	}

	[TestMethod]
	public async Task Rolling_NoRollingWhenMaxLengthZero()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
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

		var baseFile = Path.Combine(root, "noroll.txt");
		var f1 = Path.Combine(root, "noroll.1.txt");

		fs.FileExists(baseFile).AssertTrue();
		fs.FileExists(f1).AssertFalse("No rolling should occur when MaxLength=0");
	}

	#endregion

	#region History Policy - Delete Tests

	[TestMethod]
	public async Task HistoryPolicy_Delete_RemovesOldFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var threeDaysAgo = DateTime.Today.AddDays(-3);

		var oldFile1 = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		var oldFile2 = Path.Combine(root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(fs, oldFile1, "old content 1");
		WriteAllText(fs, oldFile2, "old content 2");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		fs.FileExists(oldFile1).AssertFalse("Old file 1 should be deleted");
		fs.FileExists(oldFile2).AssertFalse("Old file 2 should be deleted");
	}

	[TestMethod]
	public async Task HistoryPolicy_Delete_RemovesOldDirectories()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDir = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd"));
		fs.CreateDirectory(oldDir);
		WriteAllText(fs, Path.Combine(oldDir, "log.txt"), "old log");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		fs.DirectoryExists(oldDir).AssertFalse("Old directory should be deleted");
	}

	[TestMethod]
	public async Task HistoryPolicy_Delete_KeepsRecentFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		// File from today should NOT be deleted
		var todayFile = Path.Combine(root, DateTime.Today.ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(fs, todayFile, "today content");

		// Old file should be deleted
		var oldFile = Path.Combine(root, DateTime.Today.AddDays(-5).ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(fs, oldFile, "old content");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Delete,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		fs.FileExists(todayFile).AssertTrue("Today's file should be kept");
		fs.FileExists(oldFile).AssertFalse("Old file should be deleted");
	}

	#endregion

	#region History Policy - Compression Tests

	[TestMethod]
	public async Task HistoryPolicy_Compression_CreatesZipAndDeletesOriginal()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(root, oldName);
		WriteAllText(fs, oldPath, "original content for compression");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");

		fs.FileExists(zipPath).AssertTrue("Zip file should be created");
		fs.FileExists(oldPath).AssertFalse("Original file should be deleted after compression");
	}

	[TestMethod]
	public async Task HistoryPolicy_Compression_ZipContainsCorrectContent()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(root, oldName);
		var originalContent = "This is the original log content\nLine 2\nLine 3";
		WriteAllText(fs, oldPath, originalContent);

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");
		var zipContents = ReadZipContents(fs, zipPath);

		zipContents.Count.AssertEqual(1);
		zipContents.ContainsKey(oldName).AssertTrue("Zip should contain file with original name");
		zipContents[oldName].AssertEqual(originalContent);
	}

	[TestMethod]
	public async Task HistoryPolicy_Compression_DirectoryToZip()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDir = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd"));
		fs.CreateDirectory(oldDir);

		WriteAllText(fs, Path.Combine(oldDir, "log1.txt"), "content 1");
		WriteAllText(fs, Path.Combine(oldDir, "log2.txt"), "content 2");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zipPath = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + ".zip");

		fs.FileExists(zipPath).AssertTrue("Zip file should be created from directory");
		fs.DirectoryExists(oldDir).AssertFalse("Original directory should be deleted");

		var zipContents = ReadZipContents(fs, zipPath);
		zipContents.Count.AssertEqual(2);
		zipContents.Values.Any(v => v == "content 1").AssertTrue();
		zipContents.Values.Any(v => v == "content 2").AssertTrue();
	}

	[TestMethod]
	public async Task HistoryPolicy_Compression_MultipleFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var threeDaysAgo = DateTime.Today.AddDays(-3);

		var oldFile1 = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		var oldFile2 = Path.Combine(root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");

		WriteAllText(fs, oldFile1, "content 1");
		WriteAllText(fs, oldFile2, "content 2");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Compression,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var zip1 = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");
		var zip2 = Path.Combine(root, threeDaysAgo.ToString("yyyy_MM_dd") + "_log.zip");

		fs.FileExists(zip1).AssertTrue("First zip should be created");
		fs.FileExists(zip2).AssertTrue("Second zip should be created");
		fs.FileExists(oldFile1).AssertFalse();
		fs.FileExists(oldFile2).AssertFalse();
	}

	#endregion

	#region History Policy - Move Tests

	[TestMethod]
	public async Task HistoryPolicy_Move_FilesMovedToHistory()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var history = Path.Combine(root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldName = twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt";
		var oldPath = Path.Combine(root, oldName);
		WriteAllText(fs, oldPath, "to be moved");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedPath = Path.Combine(history, oldName);

		fs.FileExists(movedPath).AssertTrue("File should be moved to history");
		fs.FileExists(oldPath).AssertFalse("Original file should not exist");

		ReadAllText(fs, movedPath).AssertEqual("to be moved");
	}

	[TestMethod]
	public async Task HistoryPolicy_Move_DirectoriesMovedToHistory()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var history = Path.Combine(root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDirName = twoDaysAgo.ToString("yyyy_MM_dd");
		var oldDir = Path.Combine(root, oldDirName);
		fs.CreateDirectory(oldDir);
		WriteAllText(fs, Path.Combine(oldDir, "log.txt"), "dir content");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedDir = Path.Combine(history, oldDirName);

		fs.DirectoryExists(movedDir).AssertTrue("Directory should be moved to history");
		fs.DirectoryExists(oldDir).AssertFalse("Original directory should not exist");
	}

	[TestMethod]
	public async Task HistoryPolicy_Move_PreservesContent()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var history = Path.Combine(root, "history");
		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldDirName = twoDaysAgo.ToString("yyyy_MM_dd");
		var oldDir = Path.Combine(root, oldDirName);
		fs.CreateDirectory(oldDir);

		var originalContent1 = "Log entry 1\nLog entry 2";
		var originalContent2 = "Another log file";
		WriteAllText(fs, Path.Combine(oldDir, "app.txt"), originalContent1);
		WriteAllText(fs, Path.Combine(oldDir, "error.txt"), originalContent2);

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = history
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "trigger")], CancellationToken);

		var movedDir = Path.Combine(history, oldDirName);

		ReadAllText(fs, Path.Combine(movedDir, "app.txt")).AssertEqual(originalContent1);
		ReadAllText(fs, Path.Combine(movedDir, "error.txt")).AssertEqual(originalContent2);
	}

	[TestMethod]
	public async Task TryDoHistoryPolicy_MoveDirectories()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var yesterday = DateTime.Today.AddDays(-1);
		var dayBefore = DateTime.Today.AddDays(-2);

		var dir1 = Path.Combine(root, yesterday.ToString("yyyy_MM_dd"));
		var dir2 = Path.Combine(root, dayBefore.ToString("yyyy_MM_dd"));
		fs.CreateDirectory(dir1);
		fs.CreateDirectory(dir2);

		WriteAllText(fs, Path.Combine(dir1, "test.txt"), "test1");
		WriteAllText(fs, Path.Combine(dir2, "test.txt"), "test2");

		var historyDir = Path.Combine(root, "history");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.SubDirectories,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyDir
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("test"), DateTime.UtcNow, LogLevels.Info, "m")], CancellationToken);

		var moved1 = Path.Combine(historyDir, Path.GetFileName(dir1));
		var moved2 = Path.Combine(historyDir, Path.GetFileName(dir2));

		fs.DirectoryExists(moved1).AssertTrue("First directory was not moved to history.");
		fs.DirectoryExists(moved2).AssertTrue("Second directory was not moved to history.");
		fs.DirectoryExists(dir1).AssertFalse();
		fs.DirectoryExists(dir2).AssertFalse();
	}

	[TestMethod]
	public async Task TryDoHistoryPolicy_MoveFiles()
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

		var historyPath = Path.Combine(root, "history");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.Move,
			HistoryAfter = TimeSpan.FromDays(1),
			HistoryMove = historyPath
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("test"), DateTime.UtcNow, LogLevels.Info, "m")], CancellationToken);

		var moved1 = Path.Combine(historyPath, Path.GetFileName(file1));
		var moved2 = Path.Combine(historyPath, Path.GetFileName(file2));

		fs.FileExists(moved1).AssertTrue("First file was not moved to history.");
		fs.FileExists(moved2).AssertTrue("Second file was not moved to history.");
	}

	#endregion

	#region History Policy - None Tests

	[TestMethod]
	public async Task HistoryPolicy_None_DoesNothing()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var twoDaysAgo = DateTime.Today.AddDays(-2);
		var oldFile = Path.Combine(root, twoDaysAgo.ToString("yyyy_MM_dd") + "_log.txt");
		WriteAllText(fs, oldFile, "old content");

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.FileName,
			HistoryPolicy = FileLogHistoryPolicies.None,
			HistoryAfter = TimeSpan.FromDays(1)
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "new")], CancellationToken);

		fs.FileExists(oldFile).AssertTrue("Old file should NOT be touched when HistoryPolicy=None");
	}

	#endregion

	#region Append Mode Tests

	[TestMethod]
	public async Task AppendMode_AppendsToExistingFile()
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
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "more")], CancellationToken);
		}

		var content = ReadAllText(fs, file);
		content.AssertContains("start");
		content.AssertContains("more");
	}

	[TestMethod]
	public async Task AppendMode_False_OverwritesFile()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";
		fs.CreateDirectory(root);

		var file = Path.Combine(root, "overwrite.txt");
		WriteAllText(fs, file, "original content that should be gone");

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "overwrite",
			Append = false
		})
		{
			var src = new DummySource("s");
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "new content")], CancellationToken);
		}

		var content = ReadAllText(fs, file);
		content.Contains("original content").AssertFalse("Original content should be overwritten");
		content.AssertContains("new content");
	}

	#endregion

	#region Source Name Tests

	[TestMethod]
	public async Task WriteChildDataToRootFile_UsesParentName()
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

		await listener.WriteMessagesAsync([new LogMessage(child, DateTime.UtcNow, LogLevels.Info, "cmsg")], CancellationToken);

		var file = Path.Combine(root, "parent" + listener.Extension);
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public async Task WriteChildDataToRootFile_False_UsesChildName()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var parent = new DummySource("parent") { IsRoot = true };
		var child = new DummySource("child") { Parent = parent };

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.None,
			WriteChildDataToRootFile = false
		};

		await listener.WriteMessagesAsync([new LogMessage(child, DateTime.UtcNow, LogLevels.Info, "cmsg")], CancellationToken);

		var childFile = Path.Combine(root, "child" + listener.Extension);
		var parentFile = Path.Combine(root, "parent" + listener.Extension);

		fs.FileExists(childFile).AssertTrue("Child file should be created");
		fs.FileExists(parentFile).AssertFalse("Parent file should not be created");
	}

	[TestMethod]
	public async Task WriteSourceId_IncludesSourceIdInLog()
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
			await listener.WriteMessagesAsync([new LogMessage(src, DateTime.UtcNow, LogLevels.Info, "mm")], CancellationToken);
		}

		var file = Path.Combine(root, "sid.txt");
		var content = ReadAllText(fs, file);
		content.AssertContains(src.Id.ToString());
	}

	#endregion

	#region Extension and Filename Tests

	[TestMethod]
	public async Task CustomExtension_IsApplied()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = "e",
			Extension = ".logx"
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "x")], CancellationToken);

		var file = Path.Combine(root, "e.logx");
		fs.FileExists(file).AssertTrue();
	}

	[TestMethod]
	public async Task GetFileName_SanitizesInvalidChars()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var bad = "bad:name*?";
		using var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			FileName = bad
		};

		await listener.WriteMessagesAsync([new LogMessage(new DummySource("s"), DateTime.UtcNow, LogLevels.Info, "z")], CancellationToken);

		var expected = new string([.. bad.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)]) + listener.Extension;
		fs.FileExists(Path.Combine(root, expected)).AssertTrue();
	}

	[TestMethod]
	public async Task MultipleSourcesCreateSeparateFiles()
	{
		var fs = new MemoryFileSystem();
		var root = "/logs";

		var src1 = new DummySource("Source1") { IsRoot = true };
		var src2 = new DummySource("Source2") { IsRoot = true };

		using (var listener = new FileLogListener(fs)
		{
			LogDirectory = root,
			SeparateByDates = SeparateByDateModes.None
		})
		{
			await listener.WriteMessagesAsync([
				new LogMessage(src1, DateTime.UtcNow, LogLevels.Info, "from source 1"),
				new LogMessage(src2, DateTime.UtcNow, LogLevels.Info, "from source 2")
			], CancellationToken);
		}

		var file1 = Path.Combine(root, "Source1.txt");
		var file2 = Path.Combine(root, "Source2.txt");

		fs.FileExists(file1).AssertTrue();
		fs.FileExists(file2).AssertTrue();

		ReadAllText(fs, file1).AssertContains("from source 1");
		ReadAllText(fs, file2).AssertContains("from source 2");
	}

	#endregion
}
