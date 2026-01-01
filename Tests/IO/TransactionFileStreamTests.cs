namespace Ecng.Tests.IO;

using System.Text;

using Ecng.IO;

[TestClass]
public class TransactionFileStreamTests : BaseTestClass
{
	#region Helper Methods

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
			_root = "/data";
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

	private string NewFilePath(string name = "file.txt")
		=> Path.Combine(_root, name);

	private TransactionFileStream CreateTfs(string path, FileMode mode)
		=> new(_fs, path, mode);

	private void WriteAllText(string path, string content)
	{
		using var stream = _fs.OpenWrite(path);
		var bytes = content.UTF8();
		stream.Write(bytes, 0, bytes.Length);
	}

	private string ReadAllText(string path)
	{
		using var stream = _fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	#endregion

	#region DataRow Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateNew_CommitAndCleanup(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(target, FileMode.CreateNew))
		{
			var data = "hello".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		_fs.FileExists(target).AssertTrue();
		ReadAllText(target).AssertEqual("hello");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_NonExisting_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		ThrowsExactly<FileNotFoundException>(() => CreateTfs(target, FileMode.Open));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void OpenOrCreate_CreatesAndWrites(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(target, FileMode.OpenOrCreate))
		{
			var data = "abc".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("abc");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Append_AppendsToExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "start");

		using (var tfs = CreateTfs(target, FileMode.Append))
		{
			var data = "+end".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("start+end");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Truncate_Existing_ReplacesContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "very-long-content");

		using (var tfs = CreateTfs(target, FileMode.Truncate))
		{
			var data = "short".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("short");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void AfterDispose_ThrowsObjectDisposed(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var stream = CreateTfs(target, FileMode.Create);

		stream.Dispose();

		ThrowsExactly<ObjectDisposedException>(() => stream.Write([1], 0, 1));
		ThrowsExactly<ObjectDisposedException>(() => { var _ = stream.Length; });
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateNew_ExistingFile_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		WriteAllText(target, "existing");

		ThrowsExactly<IOException>(() => CreateTfs(target, FileMode.CreateNew));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Create_OverwritesExisting(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "old-content");

		using (var tfs = CreateTfs(target, FileMode.Create))
		{
			var data = "new".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("new");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Truncate_NonExisting_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		ThrowsExactly<FileNotFoundException>(() => CreateTfs(target, FileMode.Truncate));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void OpenOrCreate_ExistingFile_AppendsContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "existing");

		using (var tfs = CreateTfs(target, FileMode.OpenOrCreate))
		{
			var data = "-appended".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("existing-appended");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Seek_ThrowsNotSupported(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.Seek(0, SeekOrigin.Begin));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void SetLength_ThrowsNotSupported(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.SetLength(5));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void PositionSet_ThrowsNotSupported(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.Position = 0);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CanRead_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		tfs.CanRead.AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Read_ThrowsNotSupported(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.ReadBytes(new byte[10], 10));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CanSeek_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		tfs.CanSeek.AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CanWrite_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		tfs.CanWrite.AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CanWrite_AfterDispose_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		var tfs = CreateTfs(target, FileMode.Create);
		tfs.Dispose();
		tfs.CanWrite.AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Flush_Works(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);
		tfs.Flush(); // Should not throw
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MultipleDispose_NoException(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		var tfs = CreateTfs(target, FileMode.Create);
		tfs.Dispose();
		tfs.Dispose(); // Second dispose should not throw
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Append_NonExisting_CreatesFile(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(target, FileMode.Append))
		{
			var data = "new-file".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("new-file");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Open_ExistingFile_AppendsContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "original");

		using (var tfs = CreateTfs(target, FileMode.Open))
		{
			var data = "-new".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(target).AssertEqual("original-new");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Commit_AfterDispose_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		var tfs = CreateTfs(target, FileMode.Create);
		tfs.Dispose();
		ThrowsExactly<ObjectDisposedException>(() => tfs.Commit());
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MultipleCommits_AppendData(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(target, FileMode.Create))
		{
			// First write and commit
			tfs.Write("hello".UTF8(), 0, 5);
			tfs.Commit();

			ReadAllText(target).AssertEqual("hello");

			// Second write appends and commit
			tfs.Write(" world".UTF8(), 0, 6);
			tfs.Commit();

			ReadAllText(target).AssertEqual("hello world");

			// Third write appends
			tfs.Write("!".UTF8(), 0, 1);
			tfs.Commit();

			ReadAllText(target).AssertEqual("hello world!");
		}

		// After dispose, file should still have all committed data
		ReadAllText(target).AssertEqual("hello world!");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MultipleCommits_PositionAndLengthPreserved(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);

		tfs.Write("0123456789".UTF8(), 0, 10);
		tfs.Commit();

		// After commit, position and length preserved for appending
		tfs.Position.AssertEqual(10);
		tfs.Length.AssertEqual(10);

		// New write appends
		tfs.Write("ABC".UTF8(), 0, 3);
		tfs.Commit();

		ReadAllText(target).AssertEqual("0123456789ABC");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MultipleCommits_RollbackUncommittedWrites(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(target, FileMode.Create))
		{
			// First commit
			tfs.Write("first".UTF8(), 0, 5);
			tfs.Commit();

			// Second commit
			tfs.Write("-second".UTF8(), 0, 7);
			tfs.Commit();

			// Third write - NOT committed
			tfs.Write("-garbage".UTF8(), 0, 8);
			// No commit - this should be lost
		}

		// Should have only first two committed writes, no garbage
		ReadAllText(target).AssertEqual("first-second");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DisposeWithoutCommit_Rollback(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "original");

		using (var tfs = CreateTfs(target, FileMode.Create))
		{
			tfs.Write("new".UTF8(), 0, 3);
			// No Commit() - should rollback
		}

		// Original should be preserved
		ReadAllText(target).AssertEqual("original");
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CommitRequired_ForChangesToPersist(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		var tfs = CreateTfs(target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);

		// Before dispose, target should not exist (only tmp)
		_fs.FileExists(target).AssertFalse();

		// Commit marks transaction for persistence
		tfs.Commit();
		tfs.Dispose();

		// After dispose with commit, target should exist
		_fs.FileExists(target).AssertTrue();
		ReadAllText(target).AssertEqual("test");
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DisposeWithoutCommit_NoTarget(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		var tfs = CreateTfs(target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);

		// No Commit() - rollback expected
		tfs.Dispose();

		// Target should NOT exist (rollback)
		_fs.FileExists(target).AssertFalse();
		_fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void PositionGet_ReturnsCurrentPosition(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		tfs.Position.AssertEqual(0);

		var data = "hello".UTF8();
		tfs.Write(data, 0, data.Length);
		tfs.Position.AssertEqual(5);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void IsCommitted_Property(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		using var tfs = CreateTfs(target, FileMode.Create);
		tfs.Write("test".UTF8(), 0, 4);

		tfs.IsCommitted.AssertFalse();
		tfs.Commit();
		tfs.IsCommitted.AssertTrue();
	}

	#endregion

	#region Parameter Validation Tests

	[TestMethod]
	public void NullFileSystem_Throws()
	{
		ThrowsExactly<ArgumentNullException>(() => new TransactionFileStream(null, "/file.txt", FileMode.Create));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void EmptyName_Throws(FileSystemType fsType)
	{
		InitFs(fsType);
		ThrowsExactly<ArgumentNullException>(() => new TransactionFileStream(_fs, "", FileMode.Create));
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
		public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None) => inner.Open(path, mode, access, share);
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

		public void SetReadOnly(string path, bool isReadOnly) => inner.SetReadOnly(path, isReadOnly);
		public FileAttributes GetAttributes(string path) => inner.GetAttributes(path);
	}

	/// <summary>
	/// When exception is thrown inside using block (before Commit), original file should remain unchanged (rollback).
	/// </summary>
	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void ExceptionInUsing_ShouldRollback_OriginalFilePreserved(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();

		WriteAllText(target, "ORIGINAL_IMPORTANT_DATA");

		try
		{
			using (var tfs = CreateTfs(target, FileMode.Create))
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
		ReadAllText(target).AssertEqual("ORIGINAL_IMPORTANT_DATA");
	}

	/// <summary>
	/// Stale .tmp file from previous crash should not affect new Append operation.
	/// </summary>
	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Append_WithStaleTmpFile_ShouldStartFresh(FileSystemType fsType)
	{
		InitFs(fsType);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		// Leftover .tmp from previous crashed operation
		WriteAllText(tmp, "STALE_GARBAGE_");

		// Target does NOT exist
		_fs.FileExists(target).AssertFalse();

		using (var tfs = CreateTfs(target, FileMode.Append))
		{
			var data = "newdata".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		// Should be fresh file without stale garbage
		ReadAllText(target).AssertEqual("newdata");
	}

	/// <summary>
	/// When MoveFile fails in Commit, written data should be preserved in .tmp for recovery.
	/// </summary>
	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveFileFailure_ShouldPreserveTmpForRecovery(FileSystemType fsType)
	{
		InitFs(fsType);
		var faultyFs = new FaultyFileSystem(_fs);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		WriteAllText(target, "ORIGINAL");
		faultyFs.OnMoveFile = _ => new IOException("Disk full");

		using var tfs = new TransactionFileStream(faultyFs, target, FileMode.Create);
		var data = "NEW_IMPORTANT_DATA".UTF8();
		tfs.Write(data, 0, data.Length);

		try
		{
			tfs.Commit();
			Fail("Should have thrown IOException");
		}
		catch (IOException)
		{
		}

		// Original should be untouched
		ReadAllText(target).AssertEqual("ORIGINAL");

		// .tmp should be preserved for manual recovery (not deleted!)
		_fs.FileExists(tmp).AssertTrue();
		ReadAllText(tmp).AssertEqual("NEW_IMPORTANT_DATA");
	}

	/// <summary>
	/// After MoveFile failure in Commit, Dispose should not throw and should preserve .tmp.
	/// </summary>
	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void MoveFileFailure_DisposeAfterFailedCommit_PreservesTmp(FileSystemType fsType)
	{
		InitFs(fsType);
		var faultyFs = new FaultyFileSystem(_fs);
		var target = NewFilePath();
		var tmp = target + ".tmp";

		faultyFs.OnMoveFile = _ => new IOException("Error");

		var tfs = new TransactionFileStream(faultyFs, target, FileMode.Create);
		tfs.Write("test".UTF8(), 0, 4);

		try { tfs.Commit(); } catch (IOException) { }

		// Dispose should not throw after failed Commit
		tfs.Dispose();

		// .tmp should be preserved for recovery (Commit failed, so we keep it)
		_fs.FileExists(tmp).AssertTrue();

		// Multiple Dispose should not throw
		tfs.Dispose();
	}

	#endregion
}
