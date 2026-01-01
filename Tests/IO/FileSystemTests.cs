namespace Ecng.Tests.IO;

using System.Text;
using System.IO;

using Ecng.IO;

public enum FileSystemType
{
	Local,
	Memory
}

[TestClass]
public class FileSystemTests : BaseTestClass
{
	private static void WithLocalFs(Action<IFileSystem, string> action)
	{
		var fs = LocalFileSystem.Instance;
		var root = fs.GetTempPath();

		try
		{
			fs.CreateDirectory(root);
			action(fs, root);
		}
		finally
		{
			try { if (fs.DirectoryExists(root)) fs.DeleteDirectory(root, true); } catch { }
		}
	}

	private static void WithMemoryFs(Action<IFileSystem, string> action)
	{
		var fs = new MemoryFileSystem();
		var root = "memroot";
		fs.CreateDirectory(root);
		action(fs, root);
	}

	/// <summary>
	/// Runs the given action with both LocalFileSystem and MemoryFileSystem.
	/// </summary>
	private static void ForEach(Action<IFileSystem, string, FileSystemType> action)
	{
		WithLocalFs((fs, root) => action(fs, root, FileSystemType.Local));
		WithMemoryFs((fs, root) => action(fs, root, FileSystemType.Memory));
	}

	/// <summary>
	/// Runs the given action with both LocalFileSystem and MemoryFileSystem.
	/// Simplified overload without FileSystemType parameter.
	/// </summary>
	private static void ForEach(Action<IFileSystem, string> action)
	{
		WithLocalFs(action);
		WithMemoryFs(action);
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
	public void MoveDirectory()
	{
		ForEach((fs, root) =>
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
	public void MoveDirectory_ToNestedPath()
	{
		ForEach((fs, root) =>
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
	public void CopyFile_OverwriteExisting()
	{
		// Test that CopyFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		ForEach((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			WriteAll(fs, source, "NEW");
			WriteAll(fs, dest, "OLD_CONTENT");

			fs.CopyFile(source, dest, overwrite: true);

			ReadAll(fs, dest).AssertEqual("NEW");
		});
	}

	[TestMethod]
	public void MoveFile_OverwriteExisting()
	{
		// Test that MoveFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		ForEach((fs, root) =>
		{
			var source = Path.Combine(root, "source.txt");
			var dest = Path.Combine(root, "dest.txt");

			WriteAll(fs, source, "NEW");
			WriteAll(fs, dest, "OLD_CONTENT");

			fs.MoveFile(source, dest, overwrite: true);

			fs.FileExists(source).AssertFalse();
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
	public void Open_ReadAccess_CannotWrite()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read);
			stream.CanRead.AssertTrue();
			stream.CanWrite.AssertFalse();

			var buffer = new byte[7];
			stream.ReadExactly(buffer, 0, buffer.Length);

			Throws<NotSupportedException>(() => stream.Write([1, 2, 3], 0, 3));
		});
	}

	[TestMethod]
	public void Open_WriteAccess_CannotRead()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();

			stream.Write("test"u8.ToArray(), 0, 4);

			Throws<NotSupportedException>(() => stream.ReadExactly(new byte[4]));
		});
	}

	[TestMethod]
	public void Open_ExistingFile_PositionAtStart()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
			stream.Position.AssertEqual(0L);
		});
	}

	[TestMethod]
	public void Open_ReadWriteAccess_CanDoAll()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");

			using var stream = fs.Open(file, FileMode.Create, FileAccess.ReadWrite);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertTrue();

			stream.Write("test"u8.ToArray(), 0, 4);

			stream.Position = 0;
			var buffer = new byte[4];
			stream.ReadExactly(buffer, 0, 4);
			buffer.AssertEqual("test"u8.ToArray());
		});
	}

	[TestMethod]
	public void Open_Append_CannotRead()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			stream.CanWrite.AssertTrue();
			stream.CanRead.AssertFalse();
		});
	}

	[TestMethod]
	public void Open_Append_SeekBeforeEnd_Throws()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			Throws<IOException>(() => stream.Seek(0, SeekOrigin.Begin));
		});
	}

	[TestMethod]
	public void Open_Append_ReadAccess_Throws()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			Throws<ArgumentException>(() => fs.Open(file, FileMode.Append, FileAccess.Read));
		});
	}

	[TestMethod]
	public void Open_ShareNone_BlocksSecondOpen()
	{
		ForEach((fs, root, fsType) =>
		{
			// FileShare locking is Windows-specific for local FS
			if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
				return;

			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream1 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
			Throws<IOException>(() => fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None));
		});
	}

	[TestMethod]
	public void Open_ShareRead_AllowsMultipleReaders()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream1 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var stream2 = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			stream1.CanRead.AssertTrue();
			stream2.CanRead.AssertTrue();
		});
	}

	[TestMethod]
	public void Open_ShareReadWrite_AllowsReadAndWrite()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var reader = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var writer = fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
			reader.CanRead.AssertTrue();
			writer.CanWrite.AssertTrue();
		});
	}

	[TestMethod]
	public void Open_ShareRead_BlocksWriter()
	{
		ForEach((fs, root, fsType) =>
		{
			if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
				return;

			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var reader = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			Throws<IOException>(() => fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Read));
		});
	}

	[TestMethod]
	public void Open_ShareWrite_BlocksReader()
	{
		ForEach((fs, root, fsType) =>
		{
			if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
				return;

			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var writer = fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Write);
			Throws<IOException>(() => fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Write));
		});
	}

	[TestMethod]
	public void DeleteFile_WhileOpen_Throws()
	{
		ForEach((fs, root, fsType) =>
		{
			if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
				return;

			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
			Throws<IOException>(() => fs.DeleteFile(file));
		});
	}

	[TestMethod]
	public void MoveFile_WhileOpen_Throws()
	{
		ForEach((fs, root, fsType) =>
		{
			if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
				return;

			var file = Path.Combine(root, "test.txt");
			var dest = Path.Combine(root, "moved.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
			Throws<IOException>(() => fs.MoveFile(file, dest));
		});
	}

	[TestMethod]
	public void DeleteFile_WithShareDelete_Succeeds()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
			fs.DeleteFile(file);
			fs.FileExists(file).AssertFalse();
		});
	}

	[TestMethod]
	public void DeleteFile_WithShareDelete_FileNotAccessibleAfterDelete()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
			fs.DeleteFile(file);

			fs.FileExists(file).AssertFalse();
			Throws<FileNotFoundException>(() => fs.Open(file, FileMode.Open, FileAccess.Read));

			var buffer = new byte[7];
			stream.ReadExactly(buffer, 0, 7);
			Encoding.UTF8.GetString(buffer).AssertEqual("content");
		});
	}

	[TestMethod]
	public void DeleteFile_WithShareDelete_CanCreateNewFileWithSameName()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "original");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
			fs.DeleteFile(file);

			WriteAll(fs, file, "new content");
			fs.FileExists(file).AssertTrue();

			var buffer = new byte[8];
			stream.ReadExactly(buffer, 0, 8);
			Encoding.UTF8.GetString(buffer).AssertEqual("original");

			fs.ReadAllText(file).AssertEqual("new content");
		});
	}

	[TestMethod]
	public void DeleteFile_WithShareDelete_WriteStillWorks()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "original");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);
			fs.DeleteFile(file);

			fs.FileExists(file).AssertFalse();

			stream.Seek(0, SeekOrigin.End);
			var extra = " extra"u8.ToArray();
			stream.Write(extra, 0, extra.Length);

			stream.Seek(0, SeekOrigin.Begin);
			var buffer = new byte[14];
			stream.ReadExactly(buffer, 0, 14);
			Encoding.UTF8.GetString(buffer).AssertEqual("original extra");

			fs.FileExists(file).AssertFalse();
		});
	}

	[TestMethod]
	public void DeleteFile_WithShareDelete_DataLostAfterClose()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "test.txt");
			WriteAll(fs, file, "original");

			using (var stream = fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Delete))
			{
				fs.DeleteFile(file);
				stream.Write("modified"u8.ToArray(), 0, 8);
			}

			fs.FileExists(file).AssertFalse();
		});
	}

	[TestMethod]
	public void SetReadOnly_GetAttributes_Memory()
	{
		WithMemoryFs((fs, root) =>
		{
			var file = Path.Combine(root, "rofile.txt");
			WriteAll(fs, file, "content");

			// initially not read-only
			(fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertFalse();

			fs.SetReadOnly(file, true);
			(fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertTrue();

			// Memory FS should prevent deletion of read-only file
			ThrowsExactly<UnauthorizedAccessException>(() => fs.DeleteFile(file));

			// clear and delete
			fs.SetReadOnly(file, false);
			(fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertFalse();
			fs.DeleteFile(file);
			fs.FileExists(file).AssertFalse();
		});
	}

	[TestMethod]
	public void SetReadOnly_GetAttributes_Local()
	{
		WithLocalFs((fs, root) =>
		{
			var file = Path.Combine(root, "ro_local.txt");
			WriteAll(fs, file, "content");

			fs.SetReadOnly(file, true);
			var attrs = fs.GetAttributes(file);
			(attrs.HasFlag(FileAttributes.ReadOnly)).AssertTrue();

			// Deletion behavior may differ across platforms. Accept either UnauthorizedAccessException or successful deletion.
			try
			{
				fs.DeleteFile(file);
				fs.FileExists(file).AssertFalse();
			}
			catch (UnauthorizedAccessException)
			{
				// expected on some platforms
				// try clearing and deleting
				fs.SetReadOnly(file, false);
				fs.DeleteFile(file);
				fs.FileExists(file).AssertFalse();
			}
		});
	}

	#region FileMode comprehensive tests

	[TestMethod]
	public void FileMode_CreateNew_FailsIfExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "content");

			Throws<IOException>(() => fs.Open(file, FileMode.CreateNew, FileAccess.Write));
		});
	}

	[TestMethod]
	public void FileMode_CreateNew_CreatesIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "new.txt");

			using (var stream = fs.Open(file, FileMode.CreateNew, FileAccess.Write))
			{
				stream.Write("hello"u8.ToArray(), 0, 5);
			}

			fs.FileExists(file).AssertTrue();
			ReadAll(fs, file).AssertEqual("hello");
		});
	}

	[TestMethod]
	public void FileMode_Create_TruncatesExisting()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "old content that is long");

			using (var stream = fs.Open(file, FileMode.Create, FileAccess.Write))
			{
				stream.Write("new"u8.ToArray(), 0, 3);
			}

			ReadAll(fs, file).AssertEqual("new");
		});
	}

	[TestMethod]
	public void FileMode_Create_CreatesIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "new.txt");

			using (var stream = fs.Open(file, FileMode.Create, FileAccess.Write))
			{
				stream.Write("created"u8.ToArray(), 0, 7);
			}

			fs.FileExists(file).AssertTrue();
			ReadAll(fs, file).AssertEqual("created");
		});
	}

	[TestMethod]
	public void FileMode_Open_FailsIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "nonexistent.txt");

			Throws<FileNotFoundException>(() => fs.Open(file, FileMode.Open, FileAccess.Read));
		});
	}

	[TestMethod]
	public void FileMode_Open_OpensExisting()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "content");

			using var stream = fs.Open(file, FileMode.Open, FileAccess.Read);
			var buffer = new byte[7];
			stream.ReadExactly(buffer, 0, 7);
			Encoding.UTF8.GetString(buffer).AssertEqual("content");
		});
	}

	[TestMethod]
	public void FileMode_OpenOrCreate_CreatesIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "new.txt");

			using (var stream = fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write))
			{
				stream.Write("created"u8.ToArray(), 0, 7);
			}

			fs.FileExists(file).AssertTrue();
			ReadAll(fs, file).AssertEqual("created");
		});
	}

	[TestMethod]
	public void FileMode_OpenOrCreate_OpensExisting()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "original");

			using (var stream = fs.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				// File should be opened at beginning, not truncated
				var buffer = new byte[8];
				stream.ReadExactly(buffer, 0, 8);
				Encoding.UTF8.GetString(buffer).AssertEqual("original");
			}
		});
	}

	[TestMethod]
	public void FileMode_OpenOrCreate_DoesNotTruncate()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "original content");

			using (var stream = fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write))
			{
				stream.Write("new"u8.ToArray(), 0, 3);
			}

			// Only first 3 bytes should be overwritten
			ReadAll(fs, file).AssertEqual("newginal content");
		});
	}

	[TestMethod]
	public void FileMode_Truncate_FailsIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "nonexistent.txt");

			Throws<FileNotFoundException>(() => fs.Open(file, FileMode.Truncate, FileAccess.Write));
		});
	}

	[TestMethod]
	public void FileMode_Truncate_TruncatesExisting()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "old content that is very long");

			using (var stream = fs.Open(file, FileMode.Truncate, FileAccess.Write))
			{
				stream.Write("short"u8.ToArray(), 0, 5);
			}

			ReadAll(fs, file).AssertEqual("short");
		});
	}

	[TestMethod]
	public void FileMode_Append_CreatesIfNotExists()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "new.txt");

			using (var stream = fs.Open(file, FileMode.Append, FileAccess.Write))
			{
				stream.Write("appended"u8.ToArray(), 0, 8);
			}

			fs.FileExists(file).AssertTrue();
			ReadAll(fs, file).AssertEqual("appended");
		});
	}

	[TestMethod]
	public void FileMode_Append_AppendsToExisting()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "original");

			using (var stream = fs.Open(file, FileMode.Append, FileAccess.Write))
			{
				stream.Write(" appended"u8.ToArray(), 0, 9);
			}

			ReadAll(fs, file).AssertEqual("original appended");
		});
	}

	[TestMethod]
	public void FileMode_Append_PositionAtEnd()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "existing.txt");
			WriteAll(fs, file, "12345");

			using var stream = fs.Open(file, FileMode.Append, FileAccess.Write);
			stream.Position.AssertEqual(5L);
		});
	}

	#endregion

	#region Directory enumeration tests

	[TestMethod]
	public void EnumerateFiles_NonExistingDirectory_Throws()
	{
		ForEach((fs, root) =>
		{
			var nonExistent = Path.Combine(root, "nonexistent");

			Throws<DirectoryNotFoundException>(() => fs.EnumerateFiles(nonExistent).ToList());
		});
	}

	[TestMethod]
	public void EnumerateDirectories_NonExistingDirectory_Throws()
	{
		ForEach((fs, root) =>
		{
			var nonExistent = Path.Combine(root, "nonexistent");

			Throws<DirectoryNotFoundException>(() => fs.EnumerateDirectories(nonExistent).ToList());
		});
	}

	[TestMethod]
	public void EnumerateFiles_EmptyDirectory_ReturnsEmpty()
	{
		ForEach((fs, root) =>
		{
			var emptyDir = Path.Combine(root, "empty");
			fs.CreateDirectory(emptyDir);

			fs.EnumerateFiles(emptyDir).ToArray().Length.AssertEqual(0);
		});
	}

	[TestMethod]
	public void EnumerateDirectories_EmptyDirectory_ReturnsEmpty()
	{
		ForEach((fs, root) =>
		{
			var emptyDir = Path.Combine(root, "empty");
			fs.CreateDirectory(emptyDir);

			fs.EnumerateDirectories(emptyDir).ToArray().Length.AssertEqual(0);
		});
	}

	[TestMethod]
	public void EnumerateFiles_WithPattern()
	{
		ForEach((fs, root) =>
		{
			WriteAll(fs, Path.Combine(root, "file1.txt"), "a");
			WriteAll(fs, Path.Combine(root, "file2.txt"), "b");
			WriteAll(fs, Path.Combine(root, "data.csv"), "c");

			var txtFiles = fs.EnumerateFiles(root, "*.txt").Select(Path.GetFileName).OrderBy(s => s).ToArray();
			txtFiles.AssertEqual(["file1.txt", "file2.txt"]);

			var csvFiles = fs.EnumerateFiles(root, "*.csv").Select(Path.GetFileName).ToArray();
			csvFiles.AssertEqual(["data.csv"]);
		});
	}

	[TestMethod]
	public void EnumerateDirectories_WithPattern()
	{
		ForEach((fs, root) =>
		{
			fs.CreateDirectory(Path.Combine(root, "test_dir"));
			fs.CreateDirectory(Path.Combine(root, "test_folder"));
			fs.CreateDirectory(Path.Combine(root, "other"));

			var testDirs = fs.EnumerateDirectories(root, "test_*").Select(Path.GetFileName).OrderBy(s => s).ToArray();
			testDirs.AssertEqual(["test_dir", "test_folder"]);
		});
	}

	[TestMethod]
	public void EnumerateFiles_AllDirectories()
	{
		ForEach((fs, root) =>
		{
			WriteAll(fs, Path.Combine(root, "root.txt"), "r");
			var subDir = Path.Combine(root, "sub");
			fs.CreateDirectory(subDir);
			WriteAll(fs, Path.Combine(subDir, "sub.txt"), "s");
			var deepDir = Path.Combine(subDir, "deep");
			fs.CreateDirectory(deepDir);
			WriteAll(fs, Path.Combine(deepDir, "deep.txt"), "d");

			var allFiles = fs.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
				.Select(Path.GetFileName)
				.OrderBy(s => s)
				.ToArray();
			allFiles.AssertEqual(["deep.txt", "root.txt", "sub.txt"]);
		});
	}

	[TestMethod]
	public void EnumerateDirectories_AllDirectories()
	{
		ForEach((fs, root) =>
		{
			var dir1 = Path.Combine(root, "dir1");
			fs.CreateDirectory(dir1);
			var dir2 = Path.Combine(dir1, "dir2");
			fs.CreateDirectory(dir2);
			var dir3 = Path.Combine(dir2, "dir3");
			fs.CreateDirectory(dir3);

			var allDirs = fs.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
				.Select(Path.GetFileName)
				.OrderBy(s => s)
				.ToArray();
			allDirs.AssertEqual(["dir1", "dir2", "dir3"]);
		});
	}

	#endregion

	#region Directory operations tests

	[TestMethod]
	public void CreateDirectory_NestedPath()
	{
		ForEach((fs, root) =>
		{
			var nested = Path.Combine(root, "a", "b", "c");
			fs.CreateDirectory(nested);

			fs.DirectoryExists(nested).AssertTrue();
			fs.DirectoryExists(Path.Combine(root, "a", "b")).AssertTrue();
			fs.DirectoryExists(Path.Combine(root, "a")).AssertTrue();
		});
	}

	[TestMethod]
	public void CreateDirectory_AlreadyExists_NoError()
	{
		ForEach((fs, root) =>
		{
			var dir = Path.Combine(root, "existing");
			fs.CreateDirectory(dir);
			fs.CreateDirectory(dir); // Should not throw

			fs.DirectoryExists(dir).AssertTrue();
		});
	}

	[TestMethod]
	public void DeleteDirectory_NonRecursive_FailsIfNotEmpty()
	{
		ForEach((fs, root) =>
		{
			var dir = Path.Combine(root, "nonempty");
			fs.CreateDirectory(dir);
			WriteAll(fs, Path.Combine(dir, "file.txt"), "content");

			Throws<IOException>(() => fs.DeleteDirectory(dir, recursive: false));
		});
	}

	[TestMethod]
	public void DeleteDirectory_Recursive_DeletesContents()
	{
		ForEach((fs, root) =>
		{
			var dir = Path.Combine(root, "nonempty");
			fs.CreateDirectory(dir);
			WriteAll(fs, Path.Combine(dir, "file.txt"), "content");
			var subDir = Path.Combine(dir, "sub");
			fs.CreateDirectory(subDir);
			WriteAll(fs, Path.Combine(subDir, "nested.txt"), "nested");

			fs.DeleteDirectory(dir, recursive: true);

			fs.DirectoryExists(dir).AssertFalse();
		});
	}

	[TestMethod]
	public void DeleteDirectory_NonExistent_Throws()
	{
		ForEach((fs, root) =>
		{
			var dir = Path.Combine(root, "nonexistent");

			Throws<DirectoryNotFoundException>(() => fs.DeleteDirectory(dir));
		});
	}

	#endregion

	#region File deletion tests

	[TestMethod]
	public void DeleteFile_NonExistent_NoError()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "nonexistent.txt");
			fs.DeleteFile(file); // Should not throw

			fs.FileExists(file).AssertFalse();
		});
	}

	[TestMethod]
	public void DeleteFile_ExistingFile()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "todelete.txt");
			WriteAll(fs, file, "content");

			fs.DeleteFile(file);

			fs.FileExists(file).AssertFalse();
		});
	}

	#endregion

	#region Open file in non-existing directory tests

	[TestMethod]
	public void Open_InNonExistingDirectory_Throws()
	{
		// Both LocalFileSystem and MemoryFileSystem should throw DirectoryNotFoundException
		// when trying to open a file in a non-existing directory
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "nonexistent_dir", "file.txt");

			Throws<DirectoryNotFoundException>(() => fs.Open(file, FileMode.Create, FileAccess.Write));
		});
	}

	[TestMethod]
	public void Open_InNonExistingNestedDirectory_Throws()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "a", "b", "c", "file.txt");

			Throws<DirectoryNotFoundException>(() => fs.Open(file, FileMode.CreateNew, FileAccess.Write));
		});
	}

	[TestMethod]
	public void Open_OpenOrCreate_InNonExistingDirectory_Throws()
	{
		ForEach((fs, root) =>
		{
			var file = Path.Combine(root, "nonexistent", "file.txt");

			Throws<DirectoryNotFoundException>(() => fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write));
		});
	}

	#endregion

	#region MemoryFileSystem size limit tests

	private static void WriteBytes(MemoryFileSystem fs, string path, int count)
	{
		using var stream = fs.Open(path, FileMode.Create, FileAccess.Write);
		stream.Write(new byte[count], 0, count);
	}

	[TestMethod]
	public void MemoryFs_TotalSize_TracksFileSize()
	{
		var fs = new MemoryFileSystem();
		fs.TotalSize.AssertEqual(0L);

		WriteBytes(fs, "file1.txt", 5);
		fs.TotalSize.AssertEqual(5L);

		WriteBytes(fs, "file2.txt", 6);
		fs.TotalSize.AssertEqual(11L);

		fs.DeleteFile("file1.txt");
		fs.TotalSize.AssertEqual(6L);

		fs.DeleteFile("file2.txt");
		fs.TotalSize.AssertEqual(0L);
	}

	[TestMethod]
	public void MemoryFs_TotalSize_TracksOverwrite()
	{
		var fs = new MemoryFileSystem();

		WriteBytes(fs, "file.txt", 5);
		fs.TotalSize.AssertEqual(5L);

		WriteBytes(fs, "file.txt", 14);
		fs.TotalSize.AssertEqual(14L);

		WriteBytes(fs, "file.txt", 1);
		fs.TotalSize.AssertEqual(1L);
	}

	[TestMethod]
	public void MemoryFs_TotalSize_TracksDeleteDirectory()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("dir");
		WriteBytes(fs, "dir/file1.txt", 4);
		WriteBytes(fs, "dir/file2.txt", 4);
		fs.CreateDirectory("dir/sub");
		WriteBytes(fs, "dir/sub/file3.txt", 2);

		fs.TotalSize.AssertEqual(10L);

		fs.DeleteDirectory("dir", recursive: true);
		fs.TotalSize.AssertEqual(0L);
	}

	[TestMethod]
	public void MemoryFs_MaxSize_ThrowException_WhenExceeded()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.ThrowException
		};

		WriteBytes(fs, "file1.txt", 5);
		fs.TotalSize.AssertEqual(5L);

		// This should throw - would exceed limit (5 + 10 = 15 > 10)
		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 10));

		// Original file should remain
		fs.TotalSize.AssertEqual(5L);
		fs.FileExists("file1.txt").AssertTrue();
	}

	[TestMethod]
	public void MemoryFs_MaxSize_IgnoreWrites_WhenExceeded()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.IgnoreWrites
		};

		WriteBytes(fs, "file1.txt", 5);
		fs.TotalSize.AssertEqual(5L);

		// This should be silently ignored
		WriteBytes(fs, "file2.txt", 10);

		// Total size should remain unchanged
		fs.TotalSize.AssertEqual(5L);
	}

	[TestMethod]
	public void MemoryFs_MaxSize_EvictOldest_WhenExceeded()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 15,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.EvictOldest
		};

		// Create files
		WriteBytes(fs, "old1.txt", 4);
		WriteBytes(fs, "old2.txt", 4);
		WriteBytes(fs, "old3.txt", 4);

		fs.TotalSize.AssertEqual(12L);

		// Now write a large file that requires eviction
		// Need: 12 + 10 = 22 > 15, need to evict 7+ bytes
		WriteBytes(fs, "new.txt", 10);

		// Should have evicted old1 and old2 (8 bytes) to make room
		fs.TotalSize.AssertEqual(14L); // old3 (4) + new (10)
		fs.FileExists("old1.txt").AssertFalse();
		fs.FileExists("old2.txt").AssertFalse();
		fs.FileExists("old3.txt").AssertTrue();
		fs.FileExists("new.txt").AssertTrue();
	}

	[TestMethod]
	public void MemoryFs_MaxSize_EvictOldest_DoesNotEvictOpenFiles()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.EvictOldest
		};

		WriteBytes(fs, "file1.txt", 5);

		// Open file1 to prevent eviction
		using var stream = fs.OpenRead("file1.txt");

		// Try to write more than limit allows - should fail because file1 can't be evicted
		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 10));
	}

	[TestMethod]
	public void MemoryFs_MaxSize_EvictOldest_DoesNotEvictReadOnlyFiles()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.EvictOldest
		};

		WriteBytes(fs, "readonly.txt", 5);
		fs.SetReadOnly("readonly.txt", true);

		// Try to write more than limit allows - should fail because readonly can't be evicted
		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 10));
	}

	[TestMethod]
	public void MemoryFs_MaxSize_NoLimit_WhenZero()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 0, // No limit
			OverflowBehavior = MemoryFileSystemOverflowBehavior.ThrowException
		};

		// Should be able to write unlimited data
		WriteBytes(fs, "file1.txt", 1000);
		WriteBytes(fs, "file2.txt", 1000);

		fs.TotalSize.AssertEqual(2000L);
	}

	[TestMethod]
	public void MemoryFs_MaxSize_AllowsShrinking()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.ThrowException
		};

		WriteBytes(fs, "file.txt", 10); // exactly at limit
		fs.TotalSize.AssertEqual(10L);

		// Shrinking should always be allowed
		WriteBytes(fs, "file.txt", 5);
		fs.TotalSize.AssertEqual(5L);
	}

	[TestMethod]
	public void MemoryFs_MaxSize_ExactLimit()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = MemoryFileSystemOverflowBehavior.ThrowException
		};

		WriteBytes(fs, "file.txt", 10); // exactly at limit
		fs.TotalSize.AssertEqual(10L);

		// Should throw on any addition
		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 1));
	}

	#endregion

}