namespace Ecng.Tests.IO;

using System.Text;

using Ecng.IO;

[TestClass]
public class TransactionFileStreamTests : BaseTestClass
{
	#region Helper Methods

	private static string NewFilePath(string root, string name = "file.txt")
		=> Path.Combine(root, name);

	private static TransactionFileStream CreateTfs(IFileSystem fs, string path, FileMode mode)
		=> new(fs, path, mode);

	private static void WriteAllText(IFileSystem fs, string path, string content)
	{
		using var stream = fs.OpenWrite(path);
		var bytes = content.UTF8();
		stream.Write(bytes, 0, bytes.Length);
	}

	private static string ReadAllText(IFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	#endregion

	#region DataRow Tests

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CreateNew_CommitAndCleanup(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(fs, target, FileMode.CreateNew))
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
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Open_NonExisting_Throws(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		ThrowsExactly<FileNotFoundException>(() => CreateTfs(fs, target, FileMode.Open));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void OpenOrCreate_CreatesAndWrites(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(fs, target, FileMode.OpenOrCreate))
		{
			var data = "abc".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("abc");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Append_AppendsToExisting(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "start");

		using (var tfs = CreateTfs(fs, target, FileMode.Append))
		{
			var data = "+end".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("start+end");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Truncate_Existing_ReplacesContent(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "very-long-content");

		using (var tfs = CreateTfs(fs, target, FileMode.Truncate))
		{
			var data = "short".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("short");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void AfterDispose_ThrowsObjectDisposed(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var stream = CreateTfs(fs, target, FileMode.Create);

		stream.Dispose();

		ThrowsExactly<ObjectDisposedException>(() => stream.Write([1], 0, 1));
		ThrowsExactly<ObjectDisposedException>(() => { var _ = stream.Length; });
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CreateNew_ExistingFile_Throws(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		WriteAllText(fs, target, "existing");

		ThrowsExactly<IOException>(() => CreateTfs(fs, target, FileMode.CreateNew));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Create_OverwritesExisting(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "old-content");

		using (var tfs = CreateTfs(fs, target, FileMode.Create))
		{
			var data = "new".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("new");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Truncate_NonExisting_Throws(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		ThrowsExactly<FileNotFoundException>(() => CreateTfs(fs, target, FileMode.Truncate));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void OpenOrCreate_ExistingFile_AppendsContent(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "existing");

		using (var tfs = CreateTfs(fs, target, FileMode.OpenOrCreate))
		{
			var data = "-appended".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("existing-appended");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Seek_ThrowsNotSupported(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.Seek(0, SeekOrigin.Begin));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void SetLength_ThrowsNotSupported(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.SetLength(5));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void PositionSet_ThrowsNotSupported(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.Position = 0);
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CanRead_ReturnsFalse(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.CanRead.AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Read_ThrowsNotSupported(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		ThrowsExactly<NotSupportedException>(() => tfs.ReadBytes(new byte[10], 10));
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CanSeek_ReturnsFalse(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.CanSeek.AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CanWrite_ReturnsTrue(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.CanWrite.AssertTrue();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CanWrite_AfterDispose_ReturnsFalse(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.Dispose();
		tfs.CanWrite.AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Flush_Works(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);
		tfs.Flush(); // Should not throw
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MultipleDispose_NoException(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.Dispose();
		tfs.Dispose(); // Second dispose should not throw
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Append_NonExisting_CreatesFile(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(fs, target, FileMode.Append))
		{
			var data = "new-file".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("new-file");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Open_ExistingFile_AppendsContent(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "original");

		using (var tfs = CreateTfs(fs, target, FileMode.Open))
		{
			var data = "-new".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		ReadAllText(fs, target).AssertEqual("original-new");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Commit_AfterDispose_Throws(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.Dispose();
		ThrowsExactly<ObjectDisposedException>(() => tfs.Commit());
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MultipleCommits_AppendData(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(fs, target, FileMode.Create))
		{
			// First write and commit
			tfs.Write("hello".UTF8(), 0, 5);
			tfs.Commit();

			ReadAllText(fs, target).AssertEqual("hello");

			// Second write appends and commit
			tfs.Write(" world".UTF8(), 0, 6);
			tfs.Commit();

			ReadAllText(fs, target).AssertEqual("hello world");

			// Third write appends
			tfs.Write("!".UTF8(), 0, 1);
			tfs.Commit();

			ReadAllText(fs, target).AssertEqual("hello world!");
		}

		// After dispose, file should still have all committed data
		ReadAllText(fs, target).AssertEqual("hello world!");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MultipleCommits_PositionAndLengthPreserved(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);

		tfs.Write("0123456789".UTF8(), 0, 10);
		tfs.Commit();

		// After commit, position and length preserved for appending
		tfs.Position.AssertEqual(10);
		tfs.Length.AssertEqual(10);

		// New write appends
		tfs.Write("ABC".UTF8(), 0, 3);
		tfs.Commit();

		ReadAllText(fs, target).AssertEqual("0123456789ABC");
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MultipleCommits_RollbackUncommittedWrites(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		using (var tfs = CreateTfs(fs, target, FileMode.Create))
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
		ReadAllText(fs, target).AssertEqual("first-second");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void DisposeWithoutCommit_Rollback(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "original");

		using (var tfs = CreateTfs(fs, target, FileMode.Create))
		{
			tfs.Write("new".UTF8(), 0, 3);
			// No Commit() - should rollback
		}

		// Original should be preserved
		ReadAllText(fs, target).AssertEqual("original");
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void CommitRequired_ForChangesToPersist(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		var tfs = CreateTfs(fs, target, FileMode.Create);
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
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void DisposeWithoutCommit_NoTarget(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		var tfs = CreateTfs(fs, target, FileMode.Create);
		var data = "test".UTF8();
		tfs.Write(data, 0, data.Length);

		// No Commit() - rollback expected
		tfs.Dispose();

		// Target should NOT exist (rollback)
		fs.FileExists(target).AssertFalse();
		fs.FileExists(tmp).AssertFalse();
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void PositionGet_ReturnsCurrentPosition(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
		tfs.Position.AssertEqual(0);

		var data = "hello".UTF8();
		tfs.Write(data, 0, data.Length);
		tfs.Position.AssertEqual(5);
	}

	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void IsCommitted_Property(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		using var tfs = CreateTfs(fs, target, FileMode.Create);
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
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void EmptyName_Throws(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		ThrowsExactly<ArgumentNullException>(() => new TransactionFileStream(fs, "", FileMode.Create));
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

		public long MaxSize { get => inner.MaxSize; set => inner.MaxSize = value; }
		public FileSystemOverflowBehavior OverflowBehavior { get => inner.OverflowBehavior; set => inner.OverflowBehavior = value; }
		public long TotalSize => inner.TotalSize;
	}

	/// <summary>
	/// When exception is thrown inside using block (before Commit), original file should remain unchanged (rollback).
	/// </summary>
	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void ExceptionInUsing_ShouldRollback_OriginalFilePreserved(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);

		WriteAllText(fs, target, "ORIGINAL_IMPORTANT_DATA");

		try
		{
			using (var tfs = CreateTfs(fs, target, FileMode.Create))
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
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void Append_WithStaleTmpFile_ShouldStartFresh(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		// Leftover .tmp from previous crashed operation
		WriteAllText(fs, tmp, "STALE_GARBAGE_");

		// Target does NOT exist
		fs.FileExists(target).AssertFalse();

		using (var tfs = CreateTfs(fs, target, FileMode.Append))
		{
			var data = "newdata".UTF8();
			tfs.Write(data, 0, data.Length);
			tfs.Commit();
		}

		// Should be fresh file without stale garbage
		ReadAllText(fs, target).AssertEqual("newdata");
	}

	/// <summary>
	/// When MoveFile fails in Commit, written data should be preserved in .tmp for recovery.
	/// </summary>
	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MoveFileFailure_ShouldPreserveTmpForRecovery(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var faultyFs = new FaultyFileSystem(fs);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		WriteAllText(fs, target, "ORIGINAL");
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
		ReadAllText(fs, target).AssertEqual("ORIGINAL");

		// .tmp should be preserved for manual recovery (not deleted!)
		fs.FileExists(tmp).AssertTrue();
		ReadAllText(fs, tmp).AssertEqual("NEW_IMPORTANT_DATA");
	}

	/// <summary>
	/// After MoveFile failure in Commit, Dispose should not throw and should preserve .tmp.
	/// </summary>
	[TestMethod]
	[DataRow(nameof(LocalFileSystem))]
	[DataRow(nameof(MemoryFileSystem))]
	public void MoveFileFailure_DisposeAfterFailedCommit_PreservesTmp(string fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var faultyFs = new FaultyFileSystem(fs);
		var target = NewFilePath(root);
		var tmp = target + ".tmp";

		faultyFs.OnMoveFile = _ => new IOException("Error");

		var tfs = new TransactionFileStream(faultyFs, target, FileMode.Create);
		tfs.Write("test".UTF8(), 0, 4);

		try { tfs.Commit(); } catch (IOException) { }

		// Dispose should not throw after failed Commit
		tfs.Dispose();

		// .tmp should be preserved for recovery (Commit failed, so we keep it)
		fs.FileExists(tmp).AssertTrue();

		// Multiple Dispose should not throw
		tfs.Dispose();
	}

	#endregion
}
