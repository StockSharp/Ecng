namespace Ecng.Tests.IO;

using Ecng.IO;

[TestClass]
public class IOHelperTests : BaseTestClass
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

	private string NewPath(params string[] parts)
		=> Path.Combine([_root, .. parts]);

	private void WriteFile(string path, byte data = 1)
	{
		using var s = _fs.Open(path, FileMode.Create, FileAccess.Write);
		s.WriteByte(data);
	}

	#endregion

	#region CreateDirIfNotExists

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateDirIfNotExists_CreatesDirectory(FileSystemType fsType)
	{
		InitFs(fsType);
		var path = NewPath("subdir", "file.txt");

		var result = _fs.CreateDirIfNotExists(path);

		result.AssertTrue();
		_fs.DirectoryExists(NewPath("subdir")).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateDirIfNotExists_ExistingDir_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("subdir"));

		var result = _fs.CreateDirIfNotExists(NewPath("subdir", "file.txt"));

		result.AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateDirIfNotExists_EmptyDir_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		var result = _fs.CreateDirIfNotExists("file.txt");

		result.AssertFalse();
	}

	#endregion

	#region SafeDeleteDir

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void SafeDeleteDir_DeletesDirectory(FileSystemType fsType)
	{
		InitFs(fsType);
		var subdir = NewPath("subdir");
		_fs.CreateDirectory(subdir);
		WriteFile(NewPath("subdir", "file.txt"));

		_fs.SafeDeleteDir(subdir);

		_fs.DirectoryExists(subdir).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void SafeDeleteDir_NonExistent_NoException(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.SafeDeleteDir(NewPath("nonexistent"));
	}

	#endregion

	#region CheckInstallation

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckInstallation_WithFiles_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		var install = NewPath("install");
		_fs.CreateDirectory(install);
		WriteFile(NewPath("install", "app.exe"));

		_fs.CheckInstallation(install).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckInstallation_WithSubdirs_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("install", "subdir"));

		_fs.CheckInstallation(NewPath("install")).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckInstallation_Empty_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		var install = NewPath("install");
		_fs.CreateDirectory(install);

		_fs.CheckInstallation(install).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckInstallation_NonExistent_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CheckInstallation(NewPath("install")).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckInstallation_EmptyPath_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CheckInstallation("").AssertFalse();
	}

	#endregion

	#region CreateFile

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateFile_CreatesFileWithContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var content = new byte[] { 1, 2, 3, 4, 5 };

		_fs.CreateFile(_root, "", "test.bin", content);

		_fs.FileExists(NewPath("test.bin")).AssertTrue();
		using var stream = _fs.Open(NewPath("test.bin"), FileMode.Open, FileAccess.Read);
		var read = new byte[5];
		stream.ReadExactly(read, 0, 5);
		read.AssertEqual(content);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CreateFile_WithRelativePath_CreatesDirectoryAndFile(FileSystemType fsType)
	{
		InitFs(fsType);
		var content = new byte[] { 10, 20, 30 };

		_fs.CreateFile(_root, "sub/dir", "data.bin", content);

		_fs.DirectoryExists(NewPath("sub", "dir")).AssertTrue();
		_fs.FileExists(NewPath("sub", "dir", "data.bin")).AssertTrue();
	}

	#endregion

	#region DeleteEmptyDirs

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteEmptyDirs_DeletesEmptyDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("a", "b", "c"));
		_fs.CreateDirectory(NewPath("d"));

		_fs.DeleteEmptyDirs(_root);

		_fs.DirectoryExists(_root).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void DeleteEmptyDirs_KeepsNonEmptyDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("a", "b"));
		_fs.CreateDirectory(NewPath("c"));
		WriteFile(NewPath("a", "file.txt"));

		_fs.DeleteEmptyDirs(_root);

		_fs.DirectoryExists(NewPath("a")).AssertTrue();
		_fs.DirectoryExists(NewPath("a", "b")).AssertFalse();
		_fs.DirectoryExists(NewPath("c")).AssertFalse();
	}

	#endregion

	#region GetDirectories

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void GetDirectories_ReturnsDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("a"));
		_fs.CreateDirectory(NewPath("b"));
		_fs.CreateDirectory(NewPath("c"));

		var dirs = _fs.GetDirectories(_root).OrderBy(x => x).ToArray();

		dirs.Length.AssertEqual(3);
		dirs[0].ComparePaths(NewPath("a")).AssertTrue(NewPath("a"));
		dirs[1].ComparePaths(NewPath("b")).AssertTrue(NewPath("b"));
		dirs[2].ComparePaths(NewPath("c")).AssertTrue(NewPath("c"));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void GetDirectories_NonExistent_ReturnsEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var dirs = _fs.GetDirectories(NewPath("nonexistent"));

		dirs.Any().AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void GetDirectories_WithPattern_FiltersResults(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("test1"));
		_fs.CreateDirectory(NewPath("test2"));
		_fs.CreateDirectory(NewPath("other"));

		var dirs = _fs.GetDirectories(_root, "test*").ToArray();

		dirs.Length.AssertEqual(2);
	}

	#endregion

	#region GetDirectoriesAsync

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetDirectoriesAsync_ReturnsDirectories(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("dir1"));
		_fs.CreateDirectory(NewPath("dir2"));

		var dirs = (await _fs.GetDirectoriesAsync(_root, cancellationToken: CancellationToken)).ToArray();

		dirs.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetDirectoriesAsync_NonExistent_ReturnsEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var dirs = await _fs.GetDirectoriesAsync(NewPath("nonexistent"), cancellationToken: CancellationToken);

		dirs.Any().AssertFalse();
	}

	#endregion

	#region GetFilesAsync

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetFilesAsync_ReturnsFiles(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("a.txt"), 1);
		WriteFile(NewPath("b.txt"), 2);

		var files = (await _fs.GetFilesAsync(_root, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();

		files.Length.AssertEqual(2);
		files[0].ComparePaths(NewPath("a.txt")).AssertTrue(NewPath("a.txt"));
		files[1].ComparePaths(NewPath("b.txt")).AssertTrue(NewPath("b.txt"));
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetFilesAsync_NonExistent_ReturnsEmpty(FileSystemType fsType)
	{
		InitFs(fsType);
		var files = await _fs.GetFilesAsync(NewPath("nonexistent"), cancellationToken: CancellationToken);

		files.Any().AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetFilesAsync_WithPattern_FiltersResults(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("file.txt"), 1);
		WriteFile(NewPath("file.log"), 2);
		WriteFile(NewPath("data.txt"), 3);

		var files = (await _fs.GetFilesAsync(_root, "*.txt", cancellationToken: CancellationToken)).ToArray();

		files.Length.AssertEqual(2);
	}

	#endregion

	#region Save

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Save_Stream_SavesContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var data = new byte[] { 1, 2, 3, 4, 5 };
		using var source = new MemoryStream(data);

		_fs.Save(source, NewPath("output.bin"));

		_fs.FileExists(NewPath("output.bin")).AssertTrue();
		using var read = _fs.Open(NewPath("output.bin"), FileMode.Open, FileAccess.Read);
		var buffer = new byte[5];
		read.ReadExactly(buffer, 0, 5);
		buffer.AssertEqual(data);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Save_Stream_RestoresPosition(FileSystemType fsType)
	{
		InitFs(fsType);
		var data = new byte[] { 1, 2, 3 };
		using var source = new MemoryStream(data);
		source.Position = 1;

		_fs.Save(source, NewPath("output.bin"));

		source.Position.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void Save_ByteArray_SavesContent(FileSystemType fsType)
	{
		InitFs(fsType);
		var data = new byte[] { 10, 20, 30, 40 };

		var result = _fs.Save(data, NewPath("data.bin"));

		result.AssertSame(data);
		_fs.FileExists(NewPath("data.bin")).AssertTrue();
	}

	#endregion

	#region TrySave

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void TrySave_Success_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = _fs.TrySave(data, NewPath("file.bin"), ex => caught = ex);

		result.AssertTrue();
		caught.AssertNull();
		_fs.FileExists(NewPath("file.bin")).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void TrySave_Failure_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		// Directory doesn't exist, should fail with DirectoryNotFoundException
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = _fs.TrySave(data, NewPath("nonexistent", "file.bin"), ex => caught = ex);

		result.AssertFalse();
		caught.AssertNotNull();
		(caught is DirectoryNotFoundException).AssertTrue();
	}

	#endregion

	#region CheckDirContainFiles

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainFiles_WithFiles_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("file.txt"));

		_fs.CheckDirContainFiles(_root).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainFiles_WithFilesInSubdir_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("sub"));
		WriteFile(NewPath("sub", "file.txt"));

		_fs.CheckDirContainFiles(_root).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainFiles_EmptyDir_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("sub"));

		_fs.CheckDirContainFiles(_root).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainFiles_NonExistent_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CheckDirContainFiles(NewPath("nonexistent")).AssertFalse();
	}

	#endregion

	#region CheckDirContainsAnything

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainsAnything_WithFiles_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("file.txt"));

		_fs.CheckDirContainsAnything(_root).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainsAnything_WithSubdir_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("sub"));

		_fs.CheckDirContainsAnything(_root).AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainsAnything_Empty_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		// _root exists and is empty
		_fs.CheckDirContainsAnything(_root).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void CheckDirContainsAnything_NonExistent_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CheckDirContainsAnything(NewPath("nonexistent")).AssertFalse();
	}

	#endregion

	#region IsFileLocked

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void IsFileLocked_NonExistent_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.IsFileLocked(NewPath("nonexistent.txt")).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void IsFileLocked_UnlockedFile_ReturnsFalse(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("file.txt"));

		_fs.IsFileLocked(NewPath("file.txt")).AssertFalse();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public void IsFileLocked_LockedFile_ReturnsTrue(FileSystemType fsType)
	{
		InitFs(fsType);
		using var s = _fs.Open(NewPath("file.txt"), FileMode.Create, FileAccess.Write, FileShare.None);
		s.WriteByte(1);
		_fs.IsFileLocked(NewPath("file.txt")).AssertTrue();
	}

	#endregion

	#region LocalFileSystem.Instance

	[TestMethod]
	public void LocalFileSystem_Instance_NotNull()
	{
		LocalFileSystem.Instance.AssertNotNull();
	}

	[TestMethod]
	public void LocalFileSystem_Instance_IsSingleton()
	{
		var a = LocalFileSystem.Instance;
		var b = LocalFileSystem.Instance;

		a.AssertSame(b);
	}

	#endregion

	#region Async Cancellation and Materialization Tests

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetFilesAsync_Cancellation_ThrowsOperationCanceled(FileSystemType fsType)
	{
		InitFs(fsType);
		WriteFile(NewPath("f.txt"));

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var thrown = false;
		try
		{
			await _fs.GetFilesAsync(_root, cancellationToken: cts.Token);
		}
		catch (OperationCanceledException)
		{
			thrown = true;
		}

		thrown.AssertTrue();
	}

	[TestMethod]
	[DataRow(FileSystemType.Local)]
	[DataRow(FileSystemType.Memory)]
	public async Task GetDirectoriesAsync_Materialized_AfterDeleteStillAvailable(FileSystemType fsType)
	{
		InitFs(fsType);
		_fs.CreateDirectory(NewPath("d1"));
		_fs.CreateDirectory(NewPath("d2"));

		var result = await _fs.GetDirectoriesAsync(_root, cancellationToken: CancellationToken);
		var arr = result.ToArray();

		// remove original directory
		_fs.DeleteDirectory(_root, true);

		// materialized result should still contain entries
		arr.Length.AssertEqual(2);
	}

	#endregion
}