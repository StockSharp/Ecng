namespace Ecng.Tests.Logging;

using System.Text;

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
		public DateTimeOffset CurrentTime => DateTimeOffset.Now;
		public bool IsRoot { get; set; }
		public event Action<LogMessage> Log { add { } remove { } }
	}

	private static string ReadAllText(string path) => File.ReadAllText(path, Encoding.UTF8);

	private static void WithTemp(Action<string> action, string sub = null)
	{
		var root = Tests.Config.GetTempPath(sub ?? Guid.NewGuid().ToString());
		try
		{
			Directory.CreateDirectory(root);
			action(root);
		}
		finally
		{
			try
			{
				if (Directory.Exists(root))
					Directory.Delete(root, true);
			}
			catch
			{ }
		}
	}

	[TestMethod]
	public void TryDoHistoryPolicy_MoveDirectories()
	{
		var root = Config.GetTempPath("FileLogListener_MoveDirs");

		try
		{
			Directory.CreateDirectory(root);

			// create two dated subdirectories older than HistoryAfter (yesterday and day before)
			var yesterday = DateTime.Today.AddDays(-1);
			var dayBefore = DateTime.Today.AddDays(-2);

			var dir1 = Path.Combine(root, yesterday.ToString("yyyy_MM_dd"));
			var dir2 = Path.Combine(root, dayBefore.ToString("yyyy_MM_dd"));
			Directory.CreateDirectory(dir1);
			Directory.CreateDirectory(dir2);

			// create a simple message to trigger TryDoHistoryPolicy via WriteMessages
			var src = new DummySource("test");
			var msg = new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "m");

			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.SubDirectories,
				HistoryPolicy = FileLogHistoryPolicies.Move,
				HistoryAfter = TimeSpan.FromDays(1),
				HistoryMove = Path.Combine(root, "history")
			};

			Assert.ThrowsExactly<IOException>(() => listener.WriteMessages([msg]));
		}
		finally
		{
			try
			{
				if (Directory.Exists(root))
					Directory.Delete(root, true);
			}
			catch
			{ }
		}
	}

	[TestMethod]
	public void TryDoHistoryPolicy_MoveFiles()
	{
		var root = Config.GetTempPath("FileLogListener_MoveFiles");

		try
		{
			Directory.CreateDirectory(root);

			var yesterday = DateTime.Today.AddDays(-1);
			var dayBefore = DateTime.Today.AddDays(-2);

			var file1 = Path.Combine(root, yesterday.ToString("yyyy_MM_dd") + "_log.txt");
			var file2 = Path.Combine(root, dayBefore.ToString("yyyy_MM_dd") + "_log.txt");

			File.WriteAllText(file1, "a");
			File.WriteAllText(file2, "b");

			var src = new DummySource("test");
			var msg = new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "m");

			// Should not throw
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.FileName,
				HistoryPolicy = FileLogHistoryPolicies.Move,
				HistoryAfter = TimeSpan.FromDays(1),
				HistoryMove = Path.Combine(root, "history")
			};

			listener.WriteMessages([msg]);

			// verify files moved into history folder
			var moved1 = Path.Combine(listener.HistoryMove, Path.GetFileName(file1));
			var moved2 = Path.Combine(listener.HistoryMove, Path.GetFileName(file2));

			File.Exists(moved1).AssertTrue("First file was not moved to history.");
			File.Exists(moved2).AssertTrue("Second file was not moved to history.");
		}
		finally
		{
			try
			{
				if (Directory.Exists(root))
					Directory.Delete(root, true);
			}
			catch
			{ }
		}
	}

	[TestMethod]
	public void WritesToSingleFile_WhenSeparateByDatesNone()
	{
		WithTemp(root =>
		{
			using (var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "single",
				SeparateByDates = SeparateByDateModes.None
			})
			{
				var src = new DummySource("src");
				var msg = new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "hello123");

				listener.WriteMessages([msg]);
			}

			var file = Path.Combine(root, "single.txt");
			File.Exists(file).AssertTrue();
			var content = ReadAllText(file);
			content.AssertContains("hello123");
		});
	}

	[TestMethod]
	public void CreatesDatePrefixedFile_WhenSeparateByDatesFileName()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "log",
				SeparateByDates = SeparateByDateModes.FileName,
				DirectoryDateFormat = "yyyy_MM_dd"
			};

			var src = new DummySource("s");
			listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "m1")]);

			var todayPref = DateTime.Today.ToString("yyyy_MM_dd") + "_log" + listener.Extension;
			var path = Path.Combine(root, todayPref);
			File.Exists(path).AssertTrue();
		});
	}

	[TestMethod]
	public void CreatesSubdirectory_WhenSeparateByDatesSubDirectories()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "log",
				SeparateByDates = SeparateByDateModes.SubDirectories,
				DirectoryDateFormat = "yyyy_MM_dd"
			};

			var src = new DummySource("s");
			listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "m2")]);

			var sub = Path.Combine(root, DateTime.Today.ToString("yyyy_MM_dd"));
			Directory.Exists(sub).AssertTrue();
			var file = Path.Combine(sub, "log" + listener.Extension);
			File.Exists(file).AssertTrue();
		});
	}

	[TestMethod]
	public void RotationBySizeAndCount_CreatesRollingFiles()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "rot",
				MaxLength = 100, // small
				MaxCount = 2
			};

			var src = new DummySource("s");
			for (var i = 0; i < 200; i++)
			{
				listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, new string('x', 20))]);
			}

			var baseFile = Path.Combine(root, "rot" + listener.Extension);
			var f1 = Path.Combine(root, "rot.1" + listener.Extension);
			var f2 = Path.Combine(root, "rot.2" + listener.Extension);

			File.Exists(baseFile).AssertTrue();
			(File.Exists(f1) || File.Exists(f2)).AssertTrue();
		});
	}

	[TestMethod]
	public void AppendMode_AppendsToExistingFile()
	{
		WithTemp(root =>
		{
			var file = Path.Combine(root, "app" + ".txt");
			File.WriteAllText(file, "start\n");

			using (var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "app",
				Append = true
			})
			{
				var src = new DummySource("s");
				listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "more")]);
			}

			var content = ReadAllText(file);
			content.AssertContains("start");
			content.AssertContains("more");
		});
	}

	[TestMethod]
	public void WriteChildDataToRootFile_UsesParentName()
	{
		WithTemp(root =>
		{
			var parent = new DummySource("parent") { IsRoot = true };
			var child = new DummySource("child") { Parent = parent };

			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.None,
				WriteChildDataToRootFile = true
			};

			listener.WriteMessages([new LogMessage(child, DateTimeOffset.Now, LogLevels.Info, "cmsg")]);

			var file = Path.Combine(root, "parent" + listener.Extension);
			File.Exists(file).AssertTrue();
		});
	}

	[TestMethod]
	public void WriteSourceId_IncludesSourceIdInLog()
	{
		WithTemp(root =>
		{
			var src = new DummySource("s");
			using (var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "sid",
				WriteSourceId = true
			})
			{
				listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "mm")]);
			}

			var file = Path.Combine(root, "sid.txt");
			var content = ReadAllText(file);
			content.AssertContains(src.Id.ToString());
		});
	}

	[TestMethod]
	public void CustomExtension_IsApplied()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = "e",
				Extension = ".logx"
			};

			listener.WriteMessages([new LogMessage(new DummySource("s"), DateTimeOffset.Now, LogLevels.Info, "x")]);

			var file = Path.Combine(root, "e.logx");
			File.Exists(file).AssertTrue();
		});
	}

	[TestMethod]
	public void GetFileName_SanitizesInvalidChars()
	{
		WithTemp(root =>
		{
			var bad = "bad:name*?";
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				FileName = bad
			};

			listener.WriteMessages([new LogMessage(new DummySource("s"), DateTimeOffset.Now, LogLevels.Info, "z")]);

			// Expect file created with invalid chars replaced by '_'
			var expected = new string([.. bad.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)]) + listener.Extension;
			File.Exists(Path.Combine(root, expected)).AssertTrue();
		});
	}

	[TestMethod]
	public void HistoryPolicy_Delete_RemovesOldFiles()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.FileName,
				HistoryPolicy = FileLogHistoryPolicies.Delete,
				HistoryAfter = TimeSpan.FromDays(1)
			};

			var old = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
			File.WriteAllText(Path.Combine(root, old), "old");

			listener.WriteMessages([new LogMessage(new DummySource("s"), DateTimeOffset.Now, LogLevels.Info, "t")]);

			File.Exists(Path.Combine(root, old)).AssertFalse();
		});
	}

	[TestMethod]
	public void HistoryPolicy_Compression_CreatesZipAndDeletesOriginal()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.FileName,
				HistoryPolicy = FileLogHistoryPolicies.Compression,
				HistoryAfter = TimeSpan.FromDays(1)
			};

			var oldName = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
			var oldPath = Path.Combine(root, oldName);
			File.WriteAllText(oldPath, "old");

			listener.WriteMessages([new LogMessage(new DummySource("s"), DateTimeOffset.Now, LogLevels.Info, "t")]);

			var zipPath = Path.Combine(root, Path.GetFileNameWithoutExtension(oldPath) + ".zip");
			File.Exists(zipPath).AssertTrue();
			File.Exists(oldPath).AssertFalse();
		});
	}

	[TestMethod]
	public void HistoryPolicy_Move_FilesMovedToHistory()
	{
		WithTemp(root =>
		{
			var history = Path.Combine(root, "history");
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.FileName,
				HistoryPolicy = FileLogHistoryPolicies.Move,
				HistoryAfter = TimeSpan.FromDays(1),
				HistoryMove = history
			};

			var oldName = DateTime.Today.AddDays(-2).ToString("yyyy_MM_dd") + "_log" + listener.Extension;
			var oldPath = Path.Combine(root, oldName);
			File.WriteAllText(oldPath, "old");

			listener.WriteMessages([new LogMessage(new DummySource("s"), DateTimeOffset.Now, LogLevels.Info, "t")]);

			var moved = Path.Combine(history, Path.GetFileName(oldPath));
			File.Exists(moved).AssertTrue();
		});
	}

	[TestMethod]
	public void ParallelWrites_DoNotCrashAndCreateFiles()
	{
		WithTemp(root =>
		{
			using var listener = new FileLogListener
			{
				LogDirectory = root,
				SeparateByDates = SeparateByDateModes.FileName
			};

			var sources = Enumerable.Range(0, 10).Select(i => new DummySource("s" + i)).ToArray();

			Parallel.For(0, 100, i =>
			{
				var src = sources[i % sources.Length];
				listener.WriteMessages([new LogMessage(src, DateTimeOffset.Now, LogLevels.Info, "p" + i)]);
			});

			// at least some files exist
			Directory.EnumerateFiles(root).Any().AssertTrue();
		});
	}
}