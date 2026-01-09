namespace Ecng.Tests.IO;

using System.IO;

using Ecng.IO;

/// <summary>
/// Tests for file system size limiting functionality (TotalSize, MaxSize, OverflowBehavior).
/// </summary>
[TestClass]
public class FileSystemSizeLimitTests : BaseTestClass
{
	// Note: we create new LocalFileSystem instances (not the singleton) because each test needs its own TotalSize tracking.
	// Cleanup is handled by Config.AssemblyCleanup which deletes _tempRoot.
	private static (IFileSystem fs, string root) CreateFs(string fsType)
	{
		if (fsType == nameof(MemoryFileSystem))
		{
			var fs = new MemoryFileSystem();
			var root = "/data";
			fs.CreateDirectory(root);
			return (fs, root);
		}
		else
		{
			var fs = new LocalFileSystem();
			var root = fs.GetTempPath();
			return (fs, root);
		}
	}

	private static void WriteBytes(IFileSystem fs, string path, int count)
	{
		using var stream = fs.Open(path, FileMode.Create, FileAccess.Write);
		stream.Write(new byte[count], 0, count);
	}

	private static void SetMaxSize(IFileSystem fs, long maxSize, FileSystemOverflowBehavior behavior)
	{
		fs.MaxSize = maxSize;
		fs.OverflowBehavior = behavior;
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void TotalSize_TracksFileSize(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 1000, FileSystemOverflowBehavior.ThrowException);

		fs.TotalSize.AssertEqual(0L);

		WriteBytes(fs, Path.Combine(root, "file1.txt"), 5);
		fs.TotalSize.AssertEqual(5L);

		WriteBytes(fs, Path.Combine(root, "file2.txt"), 6);
		fs.TotalSize.AssertEqual(11L);

		fs.DeleteFile(Path.Combine(root, "file1.txt"));
		fs.TotalSize.AssertEqual(6L);

		fs.DeleteFile(Path.Combine(root, "file2.txt"));
		fs.TotalSize.AssertEqual(0L);
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void MaxSize_ThrowException_WhenExceeded(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 10, FileSystemOverflowBehavior.ThrowException);

		WriteBytes(fs, Path.Combine(root, "file1.txt"), 5);
		fs.TotalSize.AssertEqual(5L);

		// This should throw - would exceed limit
		ThrowsExactly<IOException>(() => WriteBytes(fs, Path.Combine(root, "file2.txt"), 10));

		// Original tracking should remain
		fs.TotalSize.AssertEqual(5L);
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void MaxSize_IgnoreWrites_WhenExceeded(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 10, FileSystemOverflowBehavior.IgnoreWrites);

		WriteBytes(fs, Path.Combine(root, "file1.txt"), 5);
		fs.TotalSize.AssertEqual(5L);

		// This should be silently ignored
		WriteBytes(fs, Path.Combine(root, "file2.txt"), 10);

		// Total size should remain unchanged
		fs.TotalSize.AssertEqual(5L);
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void MaxSize_NoLimit_WhenZero(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 0, FileSystemOverflowBehavior.ThrowException);

		// Should be able to write unlimited data (no tracking when MaxSize=0)
		WriteBytes(fs, Path.Combine(root, "file1.txt"), 1000);
		WriteBytes(fs, Path.Combine(root, "file2.txt"), 1000);

		if (fs is MemoryFileSystem)
			fs.TotalSize.AssertEqual(2000L);
		else
			fs.TotalSize.AssertEqual(0L); // LocalFileSystem doesn't track when MaxSize=0
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void MaxSize_ExactLimit(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 10, FileSystemOverflowBehavior.ThrowException);

		WriteBytes(fs, Path.Combine(root, "file.txt"), 10);
		fs.TotalSize.AssertEqual(10L);

		// Should throw on any addition
		ThrowsExactly<IOException>(() => WriteBytes(fs, Path.Combine(root, "file2.txt"), 1));
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void TotalSize_TracksDeleteDirectory(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 1000, FileSystemOverflowBehavior.ThrowException);

		var dir = Path.Combine(root, "dir");
		fs.CreateDirectory(dir);
		WriteBytes(fs, Path.Combine(dir, "file1.txt"), 4);
		WriteBytes(fs, Path.Combine(dir, "file2.txt"), 4);
		var sub = Path.Combine(dir, "sub");
		fs.CreateDirectory(sub);
		WriteBytes(fs, Path.Combine(sub, "file3.txt"), 2);

		fs.TotalSize.AssertEqual(10L);

		fs.DeleteDirectory(dir, recursive: true);
		fs.TotalSize.AssertEqual(0L);
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void TotalSize_TracksCopyFile(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 1000, FileSystemOverflowBehavior.ThrowException);

		var src = Path.Combine(root, "src.txt");
		var dst = Path.Combine(root, "dst.txt");

		WriteBytes(fs, src, 10);
		fs.TotalSize.AssertEqual(10L);

		fs.CopyFile(src, dst);
		fs.TotalSize.AssertEqual(20L);

		// Overwrite with same size
		fs.CopyFile(src, dst, overwrite: true);
		fs.TotalSize.AssertEqual(20L);
	}

	[TestMethod]
	[DataRow(nameof(MemoryFileSystem))]
	[DataRow(nameof(LocalFileSystem))]
	public void TotalSize_TracksMoveFileOverwrite(string fsType)
	{
		var (fs, root) = CreateFs(fsType);
		SetMaxSize(fs, 1000, FileSystemOverflowBehavior.ThrowException);

		var src = Path.Combine(root, "src.txt");
		var dst = Path.Combine(root, "dst.txt");

		WriteBytes(fs, src, 10);
		WriteBytes(fs, dst, 5);
		fs.TotalSize.AssertEqual(15L);

		// Move with overwrite - should subtract old dst size
		fs.MoveFile(src, dst, overwrite: true);
		fs.TotalSize.AssertEqual(10L);
	}

	#region MemoryFileSystem-specific tests

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
	public void MemoryFs_MaxSize_EvictOldest_WhenExceeded()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 15,
			OverflowBehavior = FileSystemOverflowBehavior.EvictOldest
		};

		WriteBytes(fs, "old1.txt", 4);
		WriteBytes(fs, "old2.txt", 4);
		WriteBytes(fs, "old3.txt", 4);

		fs.TotalSize.AssertEqual(12L);

		WriteBytes(fs, "new.txt", 10);

		fs.TotalSize.AssertEqual(14L);
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
			OverflowBehavior = FileSystemOverflowBehavior.EvictOldest
		};

		WriteBytes(fs, "file1.txt", 5);

		using var stream = fs.OpenRead("file1.txt");

		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 10));
	}

	[TestMethod]
	public void MemoryFs_MaxSize_EvictOldest_DoesNotEvictReadOnlyFiles()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = FileSystemOverflowBehavior.EvictOldest
		};

		WriteBytes(fs, "readonly.txt", 5);
		fs.SetReadOnly("readonly.txt", true);

		ThrowsExactly<IOException>(() => WriteBytes(fs, "file2.txt", 10));
	}

	[TestMethod]
	public void MemoryFs_MaxSize_AllowsShrinking()
	{
		var fs = new MemoryFileSystem
		{
			MaxSize = 10,
			OverflowBehavior = FileSystemOverflowBehavior.ThrowException
		};

		WriteBytes(fs, "file.txt", 10);
		fs.TotalSize.AssertEqual(10L);

		WriteBytes(fs, "file.txt", 5);
		fs.TotalSize.AssertEqual(5L);
	}

	#endregion
}
