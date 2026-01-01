namespace Ecng.Tests.IO;

using System.Text;
using System.IO;

using Ecng.IO;

[TestClass]
public class FileSystemTests : BaseTestClass
{
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
			_root = "memroot";
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
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Contract(FileSystemType fsType)
	{
		InitFs(fsType);
		RunContract(_fs, _root);
	}

	[TestMethod]
	public void Compare()
	{
		var lfs = LocalFileSystem.Instance;
		var lroot = lfs.GetTempPath();
		lfs.CreateDirectory(lroot);

		var mfs = new MemoryFileSystem();
		var mroot = "memroot";
		mfs.CreateDirectory(mroot);

		try
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
		}
		finally
		{
			try { if (lfs.DirectoryExists(lroot)) lfs.DeleteDirectory(lroot, true); } catch { }
		}
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveDirectory(FileSystemType fsType)
	{
		InitFs(fsType);

		var sourceDir = Path.Combine(_root, "source");
		_fs.CreateDirectory(sourceDir);
		WriteAll(_fs, Path.Combine(sourceDir, "file1.txt"), "content1");

		var subDir = Path.Combine(sourceDir, "subdir");
		_fs.CreateDirectory(subDir);
		WriteAll(_fs, Path.Combine(subDir, "file2.txt"), "content2");

		var destDir = Path.Combine(_root, "dest");
		_fs.MoveDirectory(sourceDir, destDir);

		_fs.DirectoryExists(sourceDir).AssertFalse();
		_fs.DirectoryExists(destDir).AssertTrue();
		_fs.FileExists(Path.Combine(destDir, "file1.txt")).AssertTrue();
		ReadAll(_fs, Path.Combine(destDir, "file1.txt")).AssertEqual("content1");
		_fs.DirectoryExists(Path.Combine(destDir, "subdir")).AssertTrue();
		_fs.FileExists(Path.Combine(destDir, "subdir", "file2.txt")).AssertTrue();
		ReadAll(_fs, Path.Combine(destDir, "subdir", "file2.txt")).AssertEqual("content2");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveDirectory_ToNestedPath(FileSystemType fsType)
	{
		InitFs(fsType);

		var sourceDir = Path.Combine(_root, "source");
		_fs.CreateDirectory(sourceDir);
		WriteAll(_fs, Path.Combine(sourceDir, "test.txt"), "test");

		// Move to a path where parent doesn't exist yet
		var destDir = Path.Combine(_root, "level1", "level2", "dest");
		_fs.MoveDirectory(sourceDir, destDir);

		_fs.DirectoryExists(sourceDir).AssertFalse();
		_fs.DirectoryExists(destDir).AssertTrue();
		_fs.FileExists(Path.Combine(destDir, "test.txt")).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveDirectory_NonExistentSource_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var sourceDir = Path.Combine(_root, "nonexistent");
		var destDir = Path.Combine(_root, "dest");

		ThrowsExactly<DirectoryNotFoundException>(() => _fs.MoveDirectory(sourceDir, destDir));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveDirectory_DestinationExists_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var sourceDir = Path.Combine(_root, "source");
		_fs.CreateDirectory(sourceDir);

		var destDir = Path.Combine(_root, "dest");
		_fs.CreateDirectory(destDir);

		ThrowsExactly<IOException>(() => _fs.MoveDirectory(sourceDir, destDir));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CopyFile_OverwriteExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		// Test that CopyFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		var source = Path.Combine(_root, "source.txt");
		var dest = Path.Combine(_root, "dest.txt");

		WriteAll(_fs, source, "NEW");
		WriteAll(_fs, dest, "OLD_CONTENT");

		_fs.CopyFile(source, dest, overwrite: true);

		ReadAll(_fs, dest).AssertEqual("NEW");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveFile_OverwriteExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		// Test that MoveFile with overwrite=true REPLACES the destination content,
		// not appends to it.
		var source = Path.Combine(_root, "source.txt");
		var dest = Path.Combine(_root, "dest.txt");

		WriteAll(_fs, source, "NEW");
		WriteAll(_fs, dest, "OLD_CONTENT");

		_fs.MoveFile(source, dest, overwrite: true);

		_fs.FileExists(source).AssertFalse();
		ReadAll(_fs, dest).AssertEqual("NEW");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Extensions_ReadWriteAllText(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "text.txt");

		_fs.WriteAllText(file, "Hello World");
		_fs.ReadAllText(file).AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Extensions_ReadWriteAllTextAsync(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "async_text.txt");

		await _fs.WriteAllTextAsync(file, "Async Content", cancellationToken: CancellationToken);
		var content = await _fs.ReadAllTextAsync(file, cancellationToken: CancellationToken);
		content.AssertEqual("Async Content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Extensions_AppendAllText(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "append.txt");

		_fs.WriteAllText(file, "Hello");
		_fs.AppendAllText(file, " World");
		_fs.ReadAllText(file).AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Extensions_AppendAllTextAsync(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "append_async.txt");

		await _fs.WriteAllTextAsync(file, "Hello", cancellationToken: CancellationToken);
		await _fs.AppendAllTextAsync(file, " World", cancellationToken: CancellationToken);
		var content = await _fs.ReadAllTextAsync(file, cancellationToken: CancellationToken);
		content.AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Extensions_ReadWriteAllBytes(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "bytes.bin");
		var data = new byte[] { 1, 2, 3, 4, 5 };

		_fs.WriteAllBytes(file, data);
		_fs.ReadAllBytes(file).AssertEqual(data);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Extensions_ReadWriteAllBytesAsync(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "bytes_async.bin");
		var data = new byte[] { 10, 20, 30, 40, 50 };

		await _fs.WriteAllBytesAsync(file, data, CancellationToken);
		var result = await _fs.ReadAllBytesAsync(file, CancellationToken);
		result.AssertEqual(data);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Extensions_ReadWriteAllLines(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "lines.txt");
		var lines = new[] { "Line 1", "Line 2", "Line 3" };

		_fs.WriteAllLines(file, lines);
		_fs.ReadAllLines(file).AssertEqual(lines);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task Extensions_ReadWriteAllLinesAsync(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "lines_async.txt");
		var lines = new[] { "Async Line 1", "Async Line 2" };

		await _fs.WriteAllLinesAsync(file, lines, cancellationToken: CancellationToken);
		var result = await _fs.ReadAllLinesAsync(file, cancellationToken: CancellationToken);
		result.AssertEqual(lines);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Extensions_ReadWriteAllText_WithEncoding(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "unicode.txt");
		var text = "Привет мир! 你好世界";

		_fs.WriteAllText(file, text, Encoding.UTF8);
		_fs.ReadAllText(file, Encoding.UTF8).AssertEqual(text);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ReadAccess_CannotWrite(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read);
		stream.CanRead.AssertTrue();
		stream.CanWrite.AssertFalse();

		var buffer = new byte[7];
		stream.ReadExactly(buffer, 0, buffer.Length);

		Throws<NotSupportedException>(() => stream.Write([1, 2, 3], 0, 3));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_WriteAccess_CannotRead(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");

		using var stream = _fs.Open(file, FileMode.Create, FileAccess.Write);
		stream.CanWrite.AssertTrue();
		stream.CanRead.AssertFalse();

		stream.Write("test"u8.ToArray(), 0, 4);

		Throws<NotSupportedException>(() => stream.ReadExactly(new byte[4]));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ExistingFile_PositionAtStart(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
		stream.Position.AssertEqual(0L);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ReadWriteAccess_CanDoAll(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");

		using var stream = _fs.Open(file, FileMode.Create, FileAccess.ReadWrite);
		stream.CanWrite.AssertTrue();
		stream.CanRead.AssertTrue();

		stream.Write("test"u8.ToArray(), 0, 4);

		stream.Position = 0;
		var buffer = new byte[4];
		stream.ReadExactly(buffer, 0, 4);
		buffer.AssertEqual("test"u8.ToArray());
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_Append_CannotRead(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Append, FileAccess.Write);
		stream.CanWrite.AssertTrue();
		stream.CanRead.AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_Append_SeekBeforeEnd_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Append, FileAccess.Write);
		Throws<IOException>(() => stream.Seek(0, SeekOrigin.Begin));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_Append_ReadAccess_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		Throws<ArgumentException>(() => _fs.Open(file, FileMode.Append, FileAccess.Read));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ShareNone_BlocksSecondOpen(FileSystemType fsType)
	{
		// FileShare locking is Windows-specific for local FS
		if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
			return;

		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream1 = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
		Throws<IOException>(() => _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ShareRead_AllowsMultipleReaders(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream1 = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var stream2 = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
		stream1.CanRead.AssertTrue();
		stream2.CanRead.AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ShareReadWrite_AllowsReadAndWrite(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var reader = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var writer = _fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
		reader.CanRead.AssertTrue();
		writer.CanWrite.AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ShareRead_BlocksWriter(FileSystemType fsType)
	{
		if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
			return;

		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var reader = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
		Throws<IOException>(() => _fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Read));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ShareWrite_BlocksReader(FileSystemType fsType)
	{
		if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
			return;

		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var writer = _fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Write);
		Throws<IOException>(() => _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Write));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WhileOpen_Throws(FileSystemType fsType)
	{
		if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
			return;

		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
		Throws<IOException>(() => _fs.DeleteFile(file));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveFile_WhileOpen_Throws(FileSystemType fsType)
	{
		if (fsType == FileSystemType.Local && !OperatingSystemEx.IsWindows())
			return;

		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		var dest = Path.Combine(_root, "moved.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
		Throws<IOException>(() => _fs.MoveFile(file, dest));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WithShareDelete_Succeeds(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
		_fs.DeleteFile(file);
		_fs.FileExists(file).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WithShareDelete_FileNotAccessibleAfterDelete(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
		_fs.DeleteFile(file);

		_fs.FileExists(file).AssertFalse();
		Throws<FileNotFoundException>(() => _fs.Open(file, FileMode.Open, FileAccess.Read));

		var buffer = new byte[7];
		stream.ReadExactly(buffer, 0, 7);
		Encoding.UTF8.GetString(buffer).AssertEqual("content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WithShareDelete_CanCreateNewFileWithSameName(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "original");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read, FileShare.Delete);
		_fs.DeleteFile(file);

		WriteAll(_fs, file, "new content");
		_fs.FileExists(file).AssertTrue();

		var buffer = new byte[8];
		stream.ReadExactly(buffer, 0, 8);
		Encoding.UTF8.GetString(buffer).AssertEqual("original");

		_fs.ReadAllText(file).AssertEqual("new content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WithShareDelete_WriteStillWorks(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "original");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete);
		_fs.DeleteFile(file);

		_fs.FileExists(file).AssertFalse();

		stream.Seek(0, SeekOrigin.End);
		var extra = " extra"u8.ToArray();
		stream.Write(extra, 0, extra.Length);

		stream.Seek(0, SeekOrigin.Begin);
		var buffer = new byte[14];
		stream.ReadExactly(buffer, 0, 14);
		Encoding.UTF8.GetString(buffer).AssertEqual("original extra");

		_fs.FileExists(file).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_WithShareDelete_DataLostAfterClose(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "test.txt");
		WriteAll(_fs, file, "original");

		using (var stream = _fs.Open(file, FileMode.Open, FileAccess.Write, FileShare.Delete))
		{
			_fs.DeleteFile(file);
			stream.Write("modified"u8.ToArray(), 0, 8);
		}

		_fs.FileExists(file).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void SetReadOnly_GetAttributes(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "rofile.txt");
		WriteAll(_fs, file, "content");

		// initially not read-only
		(_fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertFalse();

		_fs.SetReadOnly(file, true);
		(_fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertTrue();

		// Deletion behavior may differ across platforms. Accept either UnauthorizedAccessException or successful deletion.
		try
		{
			_fs.DeleteFile(file);
			_fs.FileExists(file).AssertFalse();
		}
		catch (UnauthorizedAccessException)
		{
			// expected on Memory FS and some Local FS platforms
			// clear and delete
			_fs.SetReadOnly(file, false);
			(_fs.GetAttributes(file).HasFlag(FileAttributes.ReadOnly)).AssertFalse();
			_fs.DeleteFile(file);
			_fs.FileExists(file).AssertFalse();
		}
	}

	#region FileMode comprehensive tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_CreateNew_FailsIfExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "content");

		Throws<IOException>(() => _fs.Open(file, FileMode.CreateNew, FileAccess.Write));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_CreateNew_CreatesIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "new.txt");

		using (var stream = _fs.Open(file, FileMode.CreateNew, FileAccess.Write))
		{
			stream.Write("hello"u8.ToArray(), 0, 5);
		}

		_fs.FileExists(file).AssertTrue();
		ReadAll(_fs, file).AssertEqual("hello");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Create_TruncatesExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "old content that is long");

		using (var stream = _fs.Open(file, FileMode.Create, FileAccess.Write))
		{
			stream.Write("new"u8.ToArray(), 0, 3);
		}

		ReadAll(_fs, file).AssertEqual("new");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Create_CreatesIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "new.txt");

		using (var stream = _fs.Open(file, FileMode.Create, FileAccess.Write))
		{
			stream.Write("created"u8.ToArray(), 0, 7);
		}

		_fs.FileExists(file).AssertTrue();
		ReadAll(_fs, file).AssertEqual("created");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Open_FailsIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "nonexistent.txt");

		Throws<FileNotFoundException>(() => _fs.Open(file, FileMode.Open, FileAccess.Read));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Open_OpensExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "content");

		using var stream = _fs.Open(file, FileMode.Open, FileAccess.Read);
		var buffer = new byte[7];
		stream.ReadExactly(buffer, 0, 7);
		Encoding.UTF8.GetString(buffer).AssertEqual("content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_OpenOrCreate_CreatesIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "new.txt");

		using (var stream = _fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write))
		{
			stream.Write("created"u8.ToArray(), 0, 7);
		}

		_fs.FileExists(file).AssertTrue();
		ReadAll(_fs, file).AssertEqual("created");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_OpenOrCreate_OpensExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "original");

		using (var stream = _fs.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite))
		{
			// File should be opened at beginning, not truncated
			var buffer = new byte[8];
			stream.ReadExactly(buffer, 0, 8);
			Encoding.UTF8.GetString(buffer).AssertEqual("original");
		}
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_OpenOrCreate_DoesNotTruncate(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "original content");

		using (var stream = _fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write))
		{
			stream.Write("new"u8.ToArray(), 0, 3);
		}

		// Only first 3 bytes should be overwritten
		ReadAll(_fs, file).AssertEqual("newginal content");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Truncate_FailsIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "nonexistent.txt");

		Throws<FileNotFoundException>(() => _fs.Open(file, FileMode.Truncate, FileAccess.Write));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Truncate_TruncatesExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "old content that is very long");

		using (var stream = _fs.Open(file, FileMode.Truncate, FileAccess.Write))
		{
			stream.Write("short"u8.ToArray(), 0, 5);
		}

		ReadAll(_fs, file).AssertEqual("short");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Append_CreatesIfNotExists(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "new.txt");

		using (var stream = _fs.Open(file, FileMode.Append, FileAccess.Write))
		{
			stream.Write("appended"u8.ToArray(), 0, 8);
		}

		_fs.FileExists(file).AssertTrue();
		ReadAll(_fs, file).AssertEqual("appended");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Append_AppendsToExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "original");

		using (var stream = _fs.Open(file, FileMode.Append, FileAccess.Write))
		{
			stream.Write(" appended"u8.ToArray(), 0, 9);
		}

		ReadAll(_fs, file).AssertEqual("original appended");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void FileMode_Append_PositionAtEnd(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "existing.txt");
		WriteAll(_fs, file, "12345");

		using var stream = _fs.Open(file, FileMode.Append, FileAccess.Write);
		stream.Position.AssertEqual(5L);
	}

	#endregion

	#region Directory enumeration tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateFiles_NonExistingDirectory_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var nonExistent = Path.Combine(_root, "nonexistent");

		Throws<DirectoryNotFoundException>(() => _fs.EnumerateFiles(nonExistent).ToList());
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateDirectories_NonExistingDirectory_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var nonExistent = Path.Combine(_root, "nonexistent");

		Throws<DirectoryNotFoundException>(() => _fs.EnumerateDirectories(nonExistent).ToList());
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateFiles_EmptyDirectory_ReturnsEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var emptyDir = Path.Combine(_root, "empty");
		_fs.CreateDirectory(emptyDir);

		_fs.EnumerateFiles(emptyDir).ToArray().Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateDirectories_EmptyDirectory_ReturnsEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var emptyDir = Path.Combine(_root, "empty");
		_fs.CreateDirectory(emptyDir);

		_fs.EnumerateDirectories(emptyDir).ToArray().Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateFiles_WithPattern(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteAll(_fs, Path.Combine(_root, "file1.txt"), "a");
		WriteAll(_fs, Path.Combine(_root, "file2.txt"), "b");
		WriteAll(_fs, Path.Combine(_root, "data.csv"), "c");

		var txtFiles = _fs.EnumerateFiles(_root, "*.txt").Select(Path.GetFileName).OrderBy(s => s).ToArray();
		txtFiles.AssertEqual(["file1.txt", "file2.txt"]);

		var csvFiles = _fs.EnumerateFiles(_root, "*.csv").Select(Path.GetFileName).ToArray();
		csvFiles.AssertEqual(["data.csv"]);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateDirectories_WithPattern(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(Path.Combine(_root, "test_dir"));
		_fs.CreateDirectory(Path.Combine(_root, "test_folder"));
		_fs.CreateDirectory(Path.Combine(_root, "other"));

		var testDirs = _fs.EnumerateDirectories(_root, "test_*").Select(Path.GetFileName).OrderBy(s => s).ToArray();
		testDirs.AssertEqual(["test_dir", "test_folder"]);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateFiles_AllDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteAll(_fs, Path.Combine(_root, "root.txt"), "r");
		var subDir = Path.Combine(_root, "sub");
		_fs.CreateDirectory(subDir);
		WriteAll(_fs, Path.Combine(subDir, "sub.txt"), "s");
		var deepDir = Path.Combine(subDir, "deep");
		_fs.CreateDirectory(deepDir);
		WriteAll(_fs, Path.Combine(deepDir, "deep.txt"), "d");

		var allFiles = _fs.EnumerateFiles(_root, "*.txt", SearchOption.AllDirectories)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.ToArray();
		allFiles.AssertEqual(["deep.txt", "root.txt", "sub.txt"]);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EnumerateDirectories_AllDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		var dir1 = Path.Combine(_root, "dir1");
		_fs.CreateDirectory(dir1);
		var dir2 = Path.Combine(dir1, "dir2");
		_fs.CreateDirectory(dir2);
		var dir3 = Path.Combine(dir2, "dir3");
		_fs.CreateDirectory(dir3);

		var allDirs = _fs.EnumerateDirectories(_root, "*", SearchOption.AllDirectories)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.ToArray();
		allDirs.AssertEqual(["dir1", "dir2", "dir3"]);
	}

	#endregion

	#region Directory operations tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateDirectory_NestedPath(FileSystemType fsType)
	{
		InitFs(fsType);
		var nested = Path.Combine(_root, "a", "b", "c");
		_fs.CreateDirectory(nested);

		_fs.DirectoryExists(nested).AssertTrue();
		_fs.DirectoryExists(Path.Combine(_root, "a", "b")).AssertTrue();
		_fs.DirectoryExists(Path.Combine(_root, "a")).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateDirectory_AlreadyExists_NoError(FileSystemType fsType)
	{
		InitFs(fsType);
		var dir = Path.Combine(_root, "existing");
		_fs.CreateDirectory(dir);
		_fs.CreateDirectory(dir); // Should not throw

		_fs.DirectoryExists(dir).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteDirectory_NonRecursive_FailsIfNotEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var dir = Path.Combine(_root, "nonempty");
		_fs.CreateDirectory(dir);
		WriteAll(_fs, Path.Combine(dir, "file.txt"), "content");

		Throws<IOException>(() => _fs.DeleteDirectory(dir, recursive: false));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteDirectory_Recursive_DeletesContents(FileSystemType fsType)
	{
		InitFs(fsType);
		var dir = Path.Combine(_root, "nonempty");
		_fs.CreateDirectory(dir);
		WriteAll(_fs, Path.Combine(dir, "file.txt"), "content");
		var subDir = Path.Combine(dir, "sub");
		_fs.CreateDirectory(subDir);
		WriteAll(_fs, Path.Combine(subDir, "nested.txt"), "nested");

		_fs.DeleteDirectory(dir, recursive: true);

		_fs.DirectoryExists(dir).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteDirectory_NonExistent_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var dir = Path.Combine(_root, "nonexistent");

		Throws<DirectoryNotFoundException>(() => _fs.DeleteDirectory(dir));
	}

	#endregion

	#region File deletion tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_NonExistent_NoError(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "nonexistent.txt");
		_fs.DeleteFile(file); // Should not throw

		_fs.FileExists(file).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteFile_ExistingFile(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "todelete.txt");
		WriteAll(_fs, file, "content");

		_fs.DeleteFile(file);

		_fs.FileExists(file).AssertFalse();
	}

	#endregion

	#region Open file in non-existing directory tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_InNonExistingDirectory_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		// Both LocalFileSystem and MemoryFileSystem should throw DirectoryNotFoundException
		// when trying to open a file in a non-existing directory
		var file = Path.Combine(_root, "nonexistent_dir", "file.txt");

		Throws<DirectoryNotFoundException>(() => _fs.Open(file, FileMode.Create, FileAccess.Write));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_InNonExistingNestedDirectory_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "a", "b", "c", "file.txt");

		Throws<DirectoryNotFoundException>(() => _fs.Open(file, FileMode.CreateNew, FileAccess.Write));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_OpenOrCreate_InNonExistingDirectory_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var file = Path.Combine(_root, "nonexistent", "file.txt");

		Throws<DirectoryNotFoundException>(() => _fs.Open(file, FileMode.OpenOrCreate, FileAccess.Write));
	}

	#endregion

}