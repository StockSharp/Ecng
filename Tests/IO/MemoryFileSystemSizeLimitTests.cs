namespace Ecng.Tests.IO;

using Ecng.IO;

/// <summary>
/// Tests for MemoryFileSystem size limiting functionality (TotalSize, MaxSize, OverflowBehavior).
/// These tests are specific to MemoryFileSystem and cannot be parameterized with LocalFileSystem.
/// </summary>
[TestClass]
public class MemoryFileSystemSizeLimitTests : BaseTestClass
{
	private static void WriteBytes(MemoryFileSystem fs, string path, int count)
	{
		using var stream = fs.Open(path, FileMode.Create, FileAccess.Write);
		stream.Write(new byte[count], 0, count);
	}

	[TestMethod]
	public void TotalSize_TracksFileSize()
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
	public void TotalSize_TracksOverwrite()
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
	public void TotalSize_TracksDeleteDirectory()
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
	public void MaxSize_ThrowException_WhenExceeded()
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
	public void MaxSize_IgnoreWrites_WhenExceeded()
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
	public void MaxSize_EvictOldest_WhenExceeded()
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
	public void MaxSize_EvictOldest_DoesNotEvictOpenFiles()
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
	public void MaxSize_EvictOldest_DoesNotEvictReadOnlyFiles()
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
	public void MaxSize_NoLimit_WhenZero()
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
	public void MaxSize_AllowsShrinking()
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
	public void MaxSize_ExactLimit()
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
}
