namespace Ecng.Tests.Common;

using System.Text;

[TestClass]
public class FileSystemTests : BaseTestClass
{
	private static void WithLocalFs(Action<IFileSystem, string> action)
	{
		var root = Config.GetTempPath("Fs");

		try
		{
			Directory.CreateDirectory(root);
			action(new LocalFileSystem(), root);
		}
		finally
		{
			try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
		}
	}

	private static void WithMemoryFs(Action<IFileSystem, string> action)
	{
		var fs = new MemoryFileSystem();
		var root = "memroot";
		fs.CreateDirectory(root);
		action(fs, root);
	}

	private static void WriteAll(IFileSystem fs, string path, string content)
	{
		using var s = fs.OpenWrite(path);
		var bytes = content.UTF8();
		s.Write(bytes,0, bytes.Length);
	}

	private static string ReadAll(IFileSystem fs, string path)
	{
		using var s = fs.OpenRead(path);
		using var ms = new MemoryStream();
		s.CopyTo(ms);
		return ms.ToArray().UTF8();
	}

	private static void RunContract(IFileSystem fs, string root)
	{
		var dir1 = Path.Combine(root, "dir1");
		fs.CreateDirectory(dir1);
		fs.DirectoryExists(dir1).AssertTrue();

		var file1 = Path.Combine(dir1, "file.txt");
		WriteAll(fs, file1, "hello");
		fs.FileExists(file1).AssertTrue();
		ReadAll(fs, file1).AssertEqual("hello");

		using (var s = fs.OpenWrite(file1, append: true))
		{
			var bytes = "!".UTF8();
			s.Write(bytes,0, bytes.Length);
		}
		ReadAll(fs, file1).AssertEqual("hello!");

		var copy = Path.Combine(dir1, "copy.txt");
		fs.CopyFile(file1, copy, overwrite: true);
		fs.FileExists(copy).AssertTrue();
		ReadAll(fs, copy).AssertEqual("hello!");

		var moved = Path.Combine(dir1, "moved.txt");
		fs.MoveFile(copy, moved, overwrite: true);
		fs.FileExists(copy).AssertFalse();
		fs.FileExists(moved).AssertTrue();
		ReadAll(fs, moved).AssertEqual("hello!");

		// timestamps
		var now = DateTime.UtcNow.AddMinutes(-5);
		(fs.GetCreationTimeUtc(file1) >= now).AssertTrue();
		(fs.GetLastWriteTimeUtc(file1) >= now).AssertTrue();

		// enumerate
		var rootFileTxt = Path.Combine(root, "a.txt");
		var rootFileLog = Path.Combine(root, "b.log");
		WriteAll(fs, rootFileTxt, "t");
		WriteAll(fs, rootFileLog, "l");

		var dirA = Path.Combine(root, "dirA");
		var dirB = Path.Combine(root, "dirB");
		fs.CreateDirectory(dirA);
		fs.CreateDirectory(dirB);
		WriteAll(fs, Path.Combine(dirA, "c.txt"), "c");
		WriteAll(fs, Path.Combine(dirB, "d.txt"), "d");

		fs.EnumerateFiles(root, "*.txt", SearchOption.TopDirectoryOnly)
			.Select(Path.GetFileName)
			.AssertEqual(["a.txt"]);

		fs.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.AssertEqual(new string[] { "a.txt", "c.txt", "d.txt", "file.txt", "moved.txt" });

		fs.EnumerateDirectories(root)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.AssertEqual(new string[] { "dir1", "dirA", "dirB" });

		// MoveDirectory test
		var dirToMove = Path.Combine(root, "dirToMove");
		fs.CreateDirectory(dirToMove);
		WriteAll(fs, Path.Combine(dirToMove, "inner.txt"), "inner content");
		var subDir = Path.Combine(dirToMove, "sub");
		fs.CreateDirectory(subDir);
		WriteAll(fs, Path.Combine(subDir, "nested.txt"), "nested content");

		var movedDir = Path.Combine(root, "movedDir");
		fs.MoveDirectory(dirToMove, movedDir);

		fs.DirectoryExists(dirToMove).AssertFalse();
		fs.DirectoryExists(movedDir).AssertTrue();
		fs.FileExists(Path.Combine(movedDir, "inner.txt")).AssertTrue();
		ReadAll(fs, Path.Combine(movedDir, "inner.txt")).AssertEqual("inner content");
		fs.DirectoryExists(Path.Combine(movedDir, "sub")).AssertTrue();
		fs.FileExists(Path.Combine(movedDir, "sub", "nested.txt")).AssertTrue();
		ReadAll(fs, Path.Combine(movedDir, "sub", "nested.txt")).AssertEqual("nested content");

		// recursive delete
		fs.DeleteDirectory(dirA, recursive: true);
		fs.DirectoryExists(dirA).AssertFalse();

		// delete file
		fs.DeleteFile(moved);
		fs.FileExists(moved).AssertFalse();
	}

	private static void PrepareScenario(IFileSystem fs, string root)
	{
		var dir1 = Path.Combine(root, "dir1");
		fs.CreateDirectory(dir1);

		var file1 = Path.Combine(dir1, "file.txt");
		WriteAll(fs, file1, "hello");
		using (var s = fs.OpenWrite(file1, append: true))
		{
			var bytes = "!".UTF8();
			s.Write(bytes,0, bytes.Length);
		}

		var copy = Path.Combine(dir1, "copy.txt");
		fs.CopyFile(file1, copy, overwrite: true);
		var moved = Path.Combine(dir1, "moved.txt");
		fs.MoveFile(copy, moved, overwrite: true);

		var rootFileTxt = Path.Combine(root, "a.txt");
		var rootFileLog = Path.Combine(root, "b.log");
		WriteAll(fs, rootFileTxt, "t");
		WriteAll(fs, rootFileLog, "l");

		var dirA = Path.Combine(root, "dirA");
		var dirB = Path.Combine(root, "dirB");
		fs.CreateDirectory(dirA);
		fs.CreateDirectory(dirB);
		WriteAll(fs, Path.Combine(dirA, "c.txt"), "c");
		WriteAll(fs, Path.Combine(dirB, "d.txt"), "d");

		fs.DeleteDirectory(dirA, recursive: true);
		fs.DeleteFile(moved);
	}

	private class Snapshot
	{
		public string[] Files { get; set; }
		public string[] Dirs { get; set; }
		public Dictionary<string, byte[]> Content { get; set; }
	}

	private static Snapshot TakeSnapshot(IFileSystem fs, string root)
	{
		var allFiles = fs.EnumerateFiles(root, "*", SearchOption.AllDirectories)
			.Select(p => Path.GetRelativePath(root, p))
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var allDirs = fs.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
			.Select(p => Path.GetRelativePath(root, p))
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.ToArray();

		var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		foreach (var rel in allFiles)
		{
			var full = Path.Combine(root, rel);
			using var s = fs.OpenRead(full);
			using var ms = new MemoryStream();
			s.CopyTo(ms);
			map[rel] = ms.ToArray();
		}

		return new Snapshot { Files = allFiles, Dirs = allDirs, Content = map };
	}

	[TestMethod]
	public void Local()
	{
		WithLocalFs(RunContract);
	}

	[TestMethod]
	public void Memory()
	{
		WithMemoryFs(RunContract);
	}

	[TestMethod]
	public void Compare()
	{
		WithLocalFs((lfs, lroot) =>
		{
			WithMemoryFs((mfs, mroot) =>
			{
				PrepareScenario(lfs, lroot);
				PrepareScenario(mfs, mroot);

				var s1 = TakeSnapshot(lfs, lroot);
				var s2 = TakeSnapshot(mfs, mroot);

				s1.Dirs.AssertEqual(s2.Dirs);
				s1.Files.AssertEqual(s2.Files);

				foreach (var rel in s1.Files)
				{
					s1.Content[rel].AssertEqual(s2.Content[rel]);
				}
			});
		});
	}

	[TestMethod]
	public void MoveDirectory_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "source");
			fs.CreateDirectory(sourceDir);
			WriteAll(fs, Path.Combine(sourceDir, "file1.txt"), "content1");
			
			var subDir = Path.Combine(sourceDir, "subdir");
			fs.CreateDirectory(subDir);
			WriteAll(fs, Path.Combine(subDir, "file2.txt"), "content2");

			var destDir = Path.Combine(root, "dest");
			fs.MoveDirectory(sourceDir, destDir);

			fs.DirectoryExists(sourceDir).AssertFalse();
			fs.DirectoryExists(destDir).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "file1.txt")).AssertTrue();
			ReadAll(fs, Path.Combine(destDir, "file1.txt")).AssertEqual("content1");
			fs.DirectoryExists(Path.Combine(destDir, "subdir")).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "subdir", "file2.txt")).AssertTrue();
			ReadAll(fs, Path.Combine(destDir, "subdir", "file2.txt")).AssertEqual("content2");
		});
	}

	[TestMethod]
	public void MoveDirectory_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "source");
			fs.CreateDirectory(sourceDir);
			WriteAll(fs, Path.Combine(sourceDir, "file1.txt"), "content1");
			
			var subDir = Path.Combine(sourceDir, "subdir");
			fs.CreateDirectory(subDir);
			WriteAll(fs, Path.Combine(subDir, "file2.txt"), "content2");

			var destDir = Path.Combine(root, "dest");
			fs.MoveDirectory(sourceDir, destDir);

			fs.DirectoryExists(sourceDir).AssertFalse();
			fs.DirectoryExists(destDir).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "file1.txt")).AssertTrue();
			ReadAll(fs, Path.Combine(destDir, "file1.txt")).AssertEqual("content1");
			fs.DirectoryExists(Path.Combine(destDir, "subdir")).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "subdir", "file2.txt")).AssertTrue();
			ReadAll(fs, Path.Combine(destDir, "subdir", "file2.txt")).AssertEqual("content2");
		});
	}

	[TestMethod]
	public void MoveDirectory_ToNestedPath_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "source");
			fs.CreateDirectory(sourceDir);
			WriteAll(fs, Path.Combine(sourceDir, "test.txt"), "test");

			// Move to a path where parent doesn't exist yet
			var destDir = Path.Combine(root, "level1", "level2", "dest");
			fs.MoveDirectory(sourceDir, destDir);

			fs.DirectoryExists(sourceDir).AssertFalse();
			fs.DirectoryExists(destDir).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "test.txt")).AssertTrue();
		});
	}

	[TestMethod]
	public void MoveDirectory_ToNestedPath_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "source");
			fs.CreateDirectory(sourceDir);
			WriteAll(fs, Path.Combine(sourceDir, "test.txt"), "test");

			// Move to a path where parent doesn't exist yet
			var destDir = Path.Combine(root, "level1", "level2", "dest");
			fs.MoveDirectory(sourceDir, destDir);

			fs.DirectoryExists(sourceDir).AssertFalse();
			fs.DirectoryExists(destDir).AssertTrue();
			fs.FileExists(Path.Combine(destDir, "test.txt")).AssertTrue();
		});
	}

	[TestMethod]
	public void MoveDirectory_NonExistentSource_Throws()
	{
		WithMemoryFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "nonexistent");
			var destDir = Path.Combine(root, "dest");

			ThrowsExactly<DirectoryNotFoundException>(() => fs.MoveDirectory(sourceDir, destDir));
		});
	}

	[TestMethod]
	public void MoveDirectory_DestinationExists_Throws()
	{
		WithMemoryFs((fs, root) =>
		{
			var sourceDir = Path.Combine(root, "source");
			fs.CreateDirectory(sourceDir);

			var destDir = Path.Combine(root, "dest");
			fs.CreateDirectory(destDir);

			ThrowsExactly<IOException>(() => fs.MoveDirectory(sourceDir, destDir));
		});
	}

	[TestMethod]
	public void CopyFile_OverwriteExisting_Local()
	{
		// Test that CopyFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		WithLocalFs((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			// Create source with "NEW"
			WriteAll(fs, source, "NEW");

			// Create destination with "OLD_CONTENT" (longer than source)
			WriteAll(fs, dest, "OLD_CONTENT");

			// Copy with overwrite - should REPLACE, not append
			fs.CopyFile(source, dest, overwrite: true);

			// Destination should contain only "NEW", not "OLD_CONTENTNEW"
			ReadAll(fs, dest).AssertEqual("NEW");
		});
	}

	[TestMethod]
	public void CopyFile_OverwriteExisting_Memory()
	{
		// Test that CopyFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		//
		WithMemoryFs((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			// Create source with "NEW"
			WriteAll(fs, source, "NEW");

			// Create destination with "OLD_CONTENT" (longer than source)
			WriteAll(fs, dest, "OLD_CONTENT");

			// Copy with overwrite - should REPLACE, not append
			fs.CopyFile(source, dest, overwrite: true);

			// Destination should contain only "NEW", not "OLD_CONTENTNEW"
			ReadAll(fs, dest).AssertEqual("NEW");
		});
	}

	[TestMethod]
	public void MoveFile_OverwriteExisting_Local()
	{
		// Test that MoveFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		WithLocalFs((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			// Create source with "NEW"
			WriteAll(fs, source, "NEW");

			// Create destination with "OLD_CONTENT" (longer than source)
			WriteAll(fs, dest, "OLD_CONTENT");

			// Move with overwrite - should REPLACE, not append
			fs.MoveFile(source, dest, overwrite: true);

			// Source should be deleted
			fs.FileExists(source).AssertFalse();

			// Destination should contain only "NEW", not "OLD_CONTENTNEW"
			ReadAll(fs, dest).AssertEqual("NEW");
		});
	}

	[TestMethod]
	public void MoveFile_OverwriteExisting_Memory()
	{
		// Test that MoveFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		//
		WithMemoryFs((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			// Create source with "NEW"
			WriteAll(fs, source, "NEW");

			// Create destination with "OLD_CONTENT" (longer than source)
			WriteAll(fs, dest, "OLD_CONTENT");

			// Move with overwrite - should REPLACE, not append
			fs.MoveFile(source, dest, overwrite: true);

			// Source should be deleted
			fs.FileExists(source).AssertFalse();

			// Destination should contain only "NEW", not "OLD_CONTENTNEW"
			ReadAll(fs, dest).AssertEqual("NEW");
		});
	}

	[TestMethod]
	public void Extensions_ReadWriteAllText()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "text.txt");

			fs.WriteAllText(file, "Hello World");
			fs.ReadAllText(file).AssertEqual("Hello World");
		});
	}

	[TestMethod]
	public async Task Extensions_ReadWriteAllTextAsync()
	{
		var fs = new MemoryFileSystem();
		var file = "async_text.txt";

		await fs.WriteAllTextAsync(file, "Async Content", cancellationToken: CancellationToken);
		var content = await fs.ReadAllTextAsync(file, cancellationToken: CancellationToken);
		content.AssertEqual("Async Content");
	}

	[TestMethod]
	public void Extensions_AppendAllText()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "append.txt");

			fs.WriteAllText(file, "Hello");
			fs.AppendAllText(file, " World");
			fs.ReadAllText(file).AssertEqual("Hello World");
		});
	}

	[TestMethod]
	public async Task Extensions_AppendAllTextAsync()
	{
		var fs = new MemoryFileSystem();
		var file = "append_async.txt";

		await fs.WriteAllTextAsync(file, "Hello", cancellationToken: CancellationToken);
		await fs.AppendAllTextAsync(file, " World", cancellationToken: CancellationToken);
		var content = await fs.ReadAllTextAsync(file, cancellationToken: CancellationToken);
		content.AssertEqual("Hello World");
	}

	[TestMethod]
	public void Extensions_ReadWriteAllBytes()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "bytes.bin");
			var data = new byte[] { 1, 2, 3, 4, 5 };

			fs.WriteAllBytes(file, data);
			fs.ReadAllBytes(file).AssertEqual(data);
		});
	}

	[TestMethod]
	public async Task Extensions_ReadWriteAllBytesAsync()
	{
		var fs = new MemoryFileSystem();
		var file = "bytes_async.bin";
		var data = new byte[] { 10, 20, 30, 40, 50 };

		await fs.WriteAllBytesAsync(file, data, CancellationToken);
		var result = await fs.ReadAllBytesAsync(file, CancellationToken);
		result.AssertEqual(data);
	}

	[TestMethod]
	public void Extensions_ReadWriteAllLines()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "lines.txt");
			var lines = new[] { "Line 1", "Line 2", "Line 3" };

			fs.WriteAllLines(file, lines);
			fs.ReadAllLines(file).AssertEqual(lines);
		});
	}

	[TestMethod]
	public async Task Extensions_ReadWriteAllLinesAsync()
	{
		var fs = new MemoryFileSystem();
		var file = "lines_async.txt";
		var lines = new[] { "Async Line 1", "Async Line 2" };

		await fs.WriteAllLinesAsync(file, lines, cancellationToken: CancellationToken);
		var result = await fs.ReadAllLinesAsync(file, cancellationToken: CancellationToken);
		result.AssertEqual(lines);
	}

	[TestMethod]
	public void Extensions_ReadWriteAllText_WithEncoding()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "unicode.txt");
			var text = "Привет мир! 你好世界";

			fs.WriteAllText(file, text, Encoding.UTF8);
			fs.ReadAllText(file, Encoding.UTF8).AssertEqual(text);
		});
	}

	[TestMethod]
	public void Open_ReadAccess_CannotWrite_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read);
			stream.CanRead.AssertTrue();
			stream.CanWrite.AssertFalse();

			// Verify read works
			var buffer = new byte[7];
			stream.Read(buffer, 0, buffer.Length).AssertEqual(7);

			// Verify write throws
			Throws<NotSupportedException>(() => stream.Write([1, 2, 3], 0, 3));
		});
	}

	[TestMethod]
	public void Open_ReadAccess_CannotWrite_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read);
			stream.CanRead.AssertTrue();
			stream.CanWrite.AssertFalse();

			// Verify read works
			var buffer = new byte[7];
			stream.Read(buffer, 0, buffer.Length).AssertEqual(7);

			// Verify write throws
			Throws<NotSupportedException>(() => stream.Write([1, 2, 3], 0, 3));
		});
	}

	[TestMethod]
	public void Open_WriteAccess_CannotRead_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();

			// Verify write works
			stream.Write("test"u8.ToArray(), 0, 4);

			// Verify read throws
			Throws<NotSupportedException>(() => stream.Read(new byte[4], 0, 4));
		});
	}

	[TestMethod]
	public void Open_WriteAccess_CannotRead_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();

			// Verify write works
			stream.Write("test"u8.ToArray(), 0, 4);

			// Verify read throws
			Throws<NotSupportedException>(() => stream.Read(new byte[4], 0, 4));
		});
	}

	[TestMethod]
	public void Open_ReadWriteAccess_CanDoAll_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.ReadWrite);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertTrue();

			// Verify write works
			stream.Write("test"u8.ToArray(), 0, 4);

			// Verify read works
			stream.Position = 0;
			var buffer = new byte[4];
			stream.Read(buffer, 0, 4).AssertEqual(4);
			buffer.AssertEqual("test"u8.ToArray());
		});
	}

	[TestMethod]
	public void Open_ReadWriteAccess_CanDoAll_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.ReadWrite);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertTrue();

			// Verify write works
			stream.Write("test"u8.ToArray(), 0, 4);

			// Verify read works
			stream.Position = 0;
			var buffer = new byte[4];
			stream.Read(buffer, 0, 4).AssertEqual(4);
			buffer.AssertEqual("test"u8.ToArray());
		});
	}

	[TestMethod]
	public void Open_Append_CannotRead_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();
		});
	}

	[TestMethod]
	public void Open_Append_CannotRead_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();
		});
	}

	[TestMethod]
	public void Open_Append_SeekBeforeEnd_Throws_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			// Seeking to beginning should throw
			Throws<IOException>(() => stream.Seek(0, SeekOrigin.Begin));
		});
	}

	[TestMethod]
	public void Open_Append_SeekBeforeEnd_Throws_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			// Seeking to beginning should throw
			Throws<IOException>(() => stream.Seek(0, SeekOrigin.Begin));
		});
	}

	[TestMethod]
	public void Open_Append_ReadAccess_Throws_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			// Append with Read access should throw
			Throws<ArgumentException>(() => fs.Open(file, FileMode.Append, FileAccess.Read));
		});
	}

	[TestMethod]
	public void Open_Append_ReadAccess_Throws_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			// Append with Read access should throw
			Throws<ArgumentException>(() => fs.Open(file, FileMode.Append, FileAccess.Read));
		});
	}

	[TestMethod]
	public void Open_ShareNone_BlocksSecondOpen_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream1 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
			// Second open should throw because first has exclusive access
			Throws<IOException>(() => fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None));
		});
	}

	[TestMethod]
	public void Open_ShareRead_AllowsMultipleReaders_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream1 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var stream2 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			// Both streams should be readable
			stream1.CanRead.AssertTrue();
			stream2.CanRead.AssertTrue();
		});
	}

	[TestMethod]
	public void Open_ShareReadWrite_AllowsReadAndWrite_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var reader = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var writer = fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
			reader.CanRead.AssertTrue();
			writer.CanWrite.AssertTrue();
		});
	}
}