namespace Ecng.Tests.Serialization;

using System.Text;

using Ecng.Serialization;

[TestClass]
public class TransactionFileStreamTests : BaseTestClass
{
	#region Helper Methods
	private static string NewTempFilePath()
	{
		var dir = Config.GetTempPath(nameof(TransactionFileStreamTests));
		Directory.CreateDirectory(dir);
		return Path.Combine(dir, Guid.NewGuid().ToString("N"));
	}

	private static void WriteAllText(string path, string content)
	{
		File.WriteAllText(path, content, Encoding.UTF8);
	}

	private static string ReadAllText(string path)
	{
		return File.ReadAllText(path, Encoding.UTF8);
	}

	#endregion

	#region LocalFileSystem Tests

	[TestMethod]
	public void CreateNew_CommitAndCleanup()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";

		try
		{
			using (var tfs = new TransactionFileStream(target, FileMode.CreateNew))
			{
				var data = "hello".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			File.Exists(target).AssertTrue();
			ReadAllText(target).AssertEqual("hello");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Open_NonExisting_Throws()
	{
		var target = NewTempFilePath();
		try
		{
			ThrowsExactly<FileNotFoundException>(() => new TransactionFileStream(target, FileMode.Open));
		}
		finally
		{
			var tmp = target + ".tmp";
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void OpenOrCreate_CreatesAndWrites()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using (var tfs = new TransactionFileStream(target, FileMode.OpenOrCreate))
			{
				var data = "abc".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("abc");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Append_AppendsToExisting()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "start");

			using (var tfs = new TransactionFileStream(target, FileMode.Append))
			{
				var data = "+end".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("start+end");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Truncate_Existing_ReplacesContent()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "very-long-content");

			using (var tfs = new TransactionFileStream(target, FileMode.Truncate))
			{
				var data = "short".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("short");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void AfterDispose_ThrowsObjectDisposed()
	{
		var target = NewTempFilePath();
		var stream = new TransactionFileStream(target, FileMode.Create);

		stream.Dispose();

		ThrowsExactly<ObjectDisposedException>(() => stream.Write([1], 0, 1));
		ThrowsExactly<ObjectDisposedException>(() => { var _ = stream.Length; });
		ThrowsExactly<ObjectDisposedException>(() => { stream.Position = 0; });
	}

	[TestMethod]
	public void CreateNew_ExistingFile_Throws()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "existing");

			ThrowsExactly<IOException>(() => new TransactionFileStream(target, FileMode.CreateNew));
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Create_OverwritesExisting()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "old-content");

			using (var tfs = new TransactionFileStream(target, FileMode.Create))
			{
				var data = "new".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("new");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Truncate_NonExisting_Throws()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			ThrowsExactly<FileNotFoundException>(() => new TransactionFileStream(target, FileMode.Truncate));
		}
		finally
		{
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void OpenOrCreate_ExistingFile_PreservesContent()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "existing");

			using (var tfs = new TransactionFileStream(target, FileMode.OpenOrCreate))
			{
				var data = "XX".UTF8();
				tfs.Write(data, 0, data.Length);

				// Truncate to match new size
				tfs.SetLength(data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("XX");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Seek_UpdatesPosition()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using (var tfs = new TransactionFileStream(target, FileMode.Create))
			{
				var data = "hello world".UTF8();
				tfs.Write(data, 0, data.Length);

				tfs.Position.AssertEqual(11);

				tfs.Seek(0, SeekOrigin.Begin);
				tfs.Position.AssertEqual(0);

				tfs.Seek(5, SeekOrigin.Begin);
				tfs.Position.AssertEqual(5);
			}
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void SetLength_ChangesStreamLength()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using (var tfs = new TransactionFileStream(target, FileMode.Create))
			{
				var data = "hello world".UTF8();
				tfs.Write(data, 0, data.Length);

				tfs.SetLength(5);
				tfs.Length.AssertEqual(5);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("hello");
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void CanRead_ReturnsFalse()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.CanRead.AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Read_ThrowsNotSupported()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			ThrowsExactly<NotSupportedException>(() => tfs.ReadBytes(new byte[10], 10));
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void CanSeek_ReturnsTrue()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.CanSeek.AssertTrue();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void CanWrite_ReturnsTrue()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.CanWrite.AssertTrue();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void CanSeek_AfterDispose_ReturnsFalse()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.Dispose();
			tfs.CanSeek.AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void CanWrite_AfterDispose_ReturnsFalse()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.Dispose();
			tfs.CanWrite.AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Flush_Works()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			var data = "test".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Flush(); // Should not throw
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void MultipleDispose_NoException()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.Dispose();
			tfs.Dispose(); // Second dispose should not throw
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Append_NonExisting_CreatesFile()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using (var tfs = new TransactionFileStream(target, FileMode.Append))
			{
				var data = "new-file".UTF8();
				tfs.Write(data, 0, data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("new-file");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Open_ExistingFile_PreservesContent()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "original");

			using (var tfs = new TransactionFileStream(target, FileMode.Open))
			{
				var data = "new".UTF8();
				tfs.Write(data, 0, data.Length);

				// Truncate to match new size
				tfs.SetLength(data.Length);
				tfs.Commit();
			}

			ReadAllText(target).AssertEqual("new");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Commit_AfterDispose_Throws()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.Dispose();
			ThrowsExactly<ObjectDisposedException>(() => tfs.Commit());
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void DoubleCommit_Throws()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			using var tfs = new TransactionFileStream(target, FileMode.Create);
			tfs.Write("test".UTF8(), 0, 4);
			tfs.Commit();
			ThrowsExactly<InvalidOperationException>(() => tfs.Commit());
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	[TestMethod]
	public void DisposeWithoutCommit_Rollback()
	{
		var target = NewTempFilePath();
		var tmp = target + ".tmp";
		try
		{
			WriteAllText(target, "original");

			using (var tfs = new TransactionFileStream(target, FileMode.Create))
			{
				tfs.Write("new".UTF8(), 0, 3);
				// No Commit() - should rollback
			}

			// Original should be preserved
			ReadAllText(target).AssertEqual("original");
			File.Exists(tmp).AssertFalse();
		}
		finally
		{
			if (File.Exists(target))
				File.Delete(target);
			if (File.Exists(tmp))
				File.Delete(tmp);
		}
	}

	#endregion

	#region MemoryFileSystem Tests

	private static string ReadAllText(MemoryFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	private static void WriteAllText(MemoryFileSystem fs, string path, string content)
	{
		using var stream = fs.OpenWrite(path);
		var bytes = content.UTF8();
		stream.Write(bytes, 0, bytes.Length);
	}

	[TestMethod]
	public void MemoryFs_CreateNew_CommitAndCleanup()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		using (var tfs = new TransactionFileStream(fs, target, FileMode.CreateNew))
		{
			var data = "hello".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		fs.FileExists(target).AssertTrue();
		ReadAllText(fs, target).AssertEqual("hello");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_CreateNew_ExistingFile_Throws()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		WriteAllText(fs, target, "existing");

		ThrowsExactly<IOException>(() => new TransactionFileStream(fs, target, FileMode.CreateNew));
	}

	[TestMethod]
	public void MemoryFs_Create_OverwritesExisting()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "old-content");

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Create))
		{
			var data = "new".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("new");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_Open_NonExisting_Throws()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		ThrowsExactly<FileNotFoundException>(() => new TransactionFileStream(fs, target, FileMode.Open));
	}

	[TestMethod]
	public void MemoryFs_Open_ExistingFile_PreservesAndOverwrites()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "original");

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Open))
		{
			var data = "NEW".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.SetLength(data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("NEW");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_OpenOrCreate_CreatesNew()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		using (var tfs = new TransactionFileStream(fs, target, FileMode.OpenOrCreate))
		{
			var data = "created".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("created");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_OpenOrCreate_ExistingFile_PreservesContent()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "existing");

		using (var tfs = new TransactionFileStream(fs, target, FileMode.OpenOrCreate))
		{
			// Write at beginning, then truncate
			var data = "XX".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.SetLength(data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("XX");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_Truncate_NonExisting_Throws()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		ThrowsExactly<FileNotFoundException>(() => new TransactionFileStream(fs, target, FileMode.Truncate));
	}

	[TestMethod]
	public void MemoryFs_Truncate_Existing_ReplacesContent()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "very-long-content");

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Truncate))
		{
			var data = "short".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("short");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_Append_NonExisting_CreatesFile()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Append))
		{
			var data = "new-file".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("new-file");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_Append_ExistingFile_AppendsContent()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "start");

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Append))
		{
			var data = "+end".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("start+end");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_CommitRequired_ForChangesToPersist()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);

		// Before dispose, target should not exist (only tmp)
		fs.FileExists(target).AssertFalse();

		// Commit marks transaction for persistence
		tfs.Commit();
		tfs.Dispose();

		// After dispose with commit, target should exist
		fs.FileExists(target).AssertTrue();
		ReadAllText(fs, target).AssertEqual("test");
	}

	[TestMethod]
	public void MemoryFs_DisposeWithoutCommit_NoTarget()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);

		// No Commit() - rollback expected
		tfs.Dispose();

		// Target should NOT exist (rollback)
		fs.FileExists(target).AssertFalse();
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	public void MemoryFs_NullFileSystem_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new TransactionFileStream(null, "/file.txt", FileMode.Create));
	}

	[TestMethod]
	public void MemoryFs_EmptyName_Throws()
	{
		var fs = new MemoryFileSystem();
		ThrowsExactly<ArgumentNullException>(() => new TransactionFileStream(fs, "", FileMode.Create));
	}

	[TestMethod]
	public void MemoryFs_SeekAndPosition()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		using var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		var data = "hello world".UTF8();
		tfs.Write(data, 0, data.Length);

		tfs.Position.AssertEqual(11);

		tfs.Seek(0, SeekOrigin.Begin);
		tfs.Position.AssertEqual(0);

		tfs.Seek(5, SeekOrigin.Begin);
		tfs.Position.AssertEqual(5);
	}

	[TestMethod]
	public void MemoryFs_SetLength()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Create))
		{
			var data = "hello world".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.SetLength(5);
			tfs.Length.AssertEqual(5);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("hello");
	}

	[TestMethod]
	public void MemoryFs_MultipleDispose_NoException()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		tfs.Dispose();
		tfs.Dispose(); // Should not throw
	}

	[TestMethod]
	public void MemoryFs_AfterDispose_ThrowsObjectDisposed()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		tfs.Dispose();

		ThrowsExactly<ObjectDisposedException>(() => tfs.Write([1], 0, 1));
		ThrowsExactly<ObjectDisposedException>(() => { var _ = tfs.Length; });
		ThrowsExactly<ObjectDisposedException>(() => { tfs.Position = 0; });
	}

	[TestMethod]
	public void MemoryFs_IsCommitted_Property()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		using var tfs = new TransactionFileStream(fs, target, FileMode.Create);
		tfs.Write("test".UTF8(), 0, 4);

		tfs.IsCommitted.AssertFalse();
		tfs.Commit();
		tfs.IsCommitted.AssertTrue();
	}

	#endregion

	#region Rollback and Recovery Tests

	/// <summary>
	/// Decorator over IFileSystem that allows injecting faults for testing.
	/// </summary>
	private class FaultyFileSystem(IFileSystem inner) : IFileSystem
	{
		public Func<string, Exception> OnMoveFile { get; set; }

		public bool FileExists(string path) => inner.FileExists(path);
		public bool DirectoryExists(string path) => inner.DirectoryExists(path);
		public Stream OpenRead(string path) => inner.OpenRead(path);
		public Stream OpenWrite(string path, bool append = false) => inner.OpenWrite(path, append);
		public void CreateDirectory(string path) => inner.CreateDirectory(path);
		public void DeleteDirectory(string path, bool recursive = false) => inner.DeleteDirectory(path, recursive);
		public void DeleteFile(string path) => inner.DeleteFile(path);

		public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
		{
			var ex = OnMoveFile?.Invoke(sourceFileName);
			if (ex != null) throw ex;
			inner.MoveFile(sourceFileName, destFileName, overwrite);
		}

		public void MoveDirectory(string sourceDirName, string destDirName) => inner.MoveDirectory(sourceDirName, destDirName);
		public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) => inner.CopyFile(sourceFileName, destFileName, overwrite);
		public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => inner.EnumerateFiles(path, searchPattern, searchOption);
		public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => inner.EnumerateDirectories(path, searchPattern, searchOption);
		public DateTime GetCreationTimeUtc(string path) => inner.GetCreationTimeUtc(path);
		public DateTime GetLastWriteTimeUtc(string path) => inner.GetLastWriteTimeUtc(path);
		public long GetFileLength(string path) => inner.GetFileLength(path);
	}

	/// <summary>
	/// When exception is thrown inside using block (before Commit), original file should remain unchanged (rollback).
	/// </summary>
	[TestMethod]
	public void ExceptionInUsing_ShouldRollback_OriginalFilePreserved()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";

		WriteAllText(fs, target, "ORIGINAL_IMPORTANT_DATA");

		try
		{
			using (var tfs = new TransactionFileStream(fs, target, FileMode.Create))
			{
				var data = "PARTIAL".UTF8();
				tfs.Write(data, 0, data.Length);
				throw new InvalidOperationException("Simulated crash");
				// No Commit() reached
			}
		}
		catch (InvalidOperationException)
		{
		}

		// Original file should be preserved on exception (rollback behavior)
		ReadAllText(fs, target).AssertEqual("ORIGINAL_IMPORTANT_DATA");
	}

	/// <summary>
	/// Stale .tmp file from previous crash should not affect new Append operation.
	/// </summary>
	[TestMethod]
	public void Append_WithStaleTmpFile_ShouldStartFresh()
	{
		var fs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		// Leftover .tmp from previous crashed operation
		WriteAllText(fs, tmp, "STALE_GARBAGE_");

		// Target does NOT exist
		fs.FileExists(target).AssertFalse();

		using (var tfs = new TransactionFileStream(fs, target, FileMode.Append))
		{
			var data = "newdata".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		// Should be fresh file without stale garbage
		ReadAllText(fs, target).AssertEqual("newdata");
	}

	/// <summary>
	/// When MoveFile fails, written data should be preserved in .tmp for recovery.
	/// </summary>
	[TestMethod]
	public void MoveFileFailure_ShouldPreserveTmpForRecovery()
	{
		var innerFs = new MemoryFileSystem();
		var faultyFs = new FaultyFileSystem(innerFs);
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		WriteAllText(innerFs, target, "ORIGINAL");
		faultyFs.OnMoveFile = _ => new IOException("Disk full");

		var tfs = new TransactionFileStream(faultyFs, target, FileMode.Create);
		var data = "NEW_IMPORTANT_DATA".UTF8();
		tfs.Write(data, 0, data.Length);
		tfs.Commit();

		try
		{
			tfs.Dispose();
			Fail("Should have thrown IOException");
		}
		catch (IOException)
		{
		}

		// Original should be untouched
		ReadAllText(innerFs, target).AssertEqual("ORIGINAL");

		// .tmp should be preserved for manual recovery (not deleted!)
		innerFs.FileExists(tmp).AssertTrue();
		ReadAllText(innerFs, tmp).AssertEqual("NEW_IMPORTANT_DATA");
	}

	/// <summary>
	/// After MoveFile failure, _disposed flag should be set so subsequent Dispose calls
	/// don't try to do anything (not even check FileExists).
	/// </summary>
	[TestMethod]
	public void MoveFileFailure_DisposedFlagShouldBeSet()
	{
		var innerFs = new MemoryFileSystem();
		var target = "/data/file.txt";
		var tmp = target + ".tmp";

		// Track all FileExists calls on tmp
		var fileExistsCallsOnTmp = 0;
		var faultyFs = new TrackingFileSystem(innerFs, path =>
		{
			if (path == tmp) fileExistsCallsOnTmp++;
		});

		faultyFs.OnMoveFile = _ => new IOException("Error");

		var tfs = new TransactionFileStream(faultyFs, target, FileMode.Create);
		tfs.Write("test".UTF8(), 0, 4);
		tfs.Commit();

		var callsBeforeFirstDispose = fileExistsCallsOnTmp;

		try { tfs.Dispose(); } catch (IOException) { }

		var callsAfterFirstDispose = fileExistsCallsOnTmp;

		// Second Dispose - if _disposed was set correctly, no FileExists should be called
		tfs.Dispose();

		var callsAfterSecondDispose = fileExistsCallsOnTmp;

		// _disposed should prevent any work in second Dispose
		// If this fails, _disposed wasn't set on MoveFile failure
		callsAfterSecondDispose.AssertEqual(callsAfterFirstDispose,
			"Second Dispose should not check FileExists if _disposed was set correctly");
	}

	/// <summary>
	/// Extended FaultyFileSystem that tracks FileExists calls.
	/// </summary>
	private class TrackingFileSystem(IFileSystem inner, Action<string> onFileExists) : IFileSystem
	{
		public Func<string, Exception> OnMoveFile { get; set; }

		public bool FileExists(string path)
		{
			onFileExists?.Invoke(path);
			return inner.FileExists(path);
		}

		public bool DirectoryExists(string path) => inner.DirectoryExists(path);
		public Stream OpenRead(string path) => inner.OpenRead(path);
		public Stream OpenWrite(string path, bool append = false) => inner.OpenWrite(path, append);
		public void CreateDirectory(string path) => inner.CreateDirectory(path);
		public void DeleteDirectory(string path, bool recursive = false) => inner.DeleteDirectory(path, recursive);
		public void DeleteFile(string path) => inner.DeleteFile(path);

		public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
		{
			var ex = OnMoveFile?.Invoke(sourceFileName);
			if (ex != null) throw ex;
			inner.MoveFile(sourceFileName, destFileName, overwrite);
		}

		public void MoveDirectory(string sourceDirName, string destDirName) => inner.MoveDirectory(sourceDirName, destDirName);
		public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false) => inner.CopyFile(sourceFileName, destFileName, overwrite);
		public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => inner.EnumerateFiles(path, searchPattern, searchOption);
		public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly) => inner.EnumerateDirectories(path, searchPattern, searchOption);
		public DateTime GetCreationTimeUtc(string path) => inner.GetCreationTimeUtc(path);
		public DateTime GetLastWriteTimeUtc(string path) => inner.GetLastWriteTimeUtc(path);
		public long GetFileLength(string path) => inner.GetFileLength(path);
	}

	#endregion
}
