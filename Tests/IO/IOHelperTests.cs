namespace Ecng.Tests.IO;

using Ecng.IO;

[TestClass]
public class IOHelperTests : BaseTestClass
{
	#region Helper Methods

	private static string NewPath(string root, params string[] parts)
		=> Path.Combine([root, .. parts]);

	private static void WriteFile(IFileSystem fs, string path, byte data = 1)
	{
		using var s = fs.Open(path, FileMode.Create, FileAccess.Write);
		s.WriteByte(data);
	}

	#endregion

	#region CreateDirIfNotExists

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CreateDirIfNotExists_CreatesDirectory(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var path = NewPath(root,"subdir", "file.txt");

		var result = fs.CreateDirIfNotExists(path);

		result.AssertTrue();
		fs.DirectoryExists(NewPath(root, "subdir")).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CreateDirIfNotExists_ExistingDir_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "subdir"));

		var result = fs.CreateDirIfNotExists(NewPath(root, "subdir", "file.txt"));

		result.AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CreateDirIfNotExists_EmptyDir_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var result = fs.CreateDirIfNotExists("file.txt");

		result.AssertFalse();
	}

	#endregion

	#region SafeDeleteDir

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void SafeDeleteDir_DeletesDirectory(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var subdir = NewPath(root, "subdir");
		fs.CreateDirectory(subdir);
		WriteFile(fs, NewPath(root,"subdir", "file.txt"));

		fs.SafeDeleteDir(subdir);

		fs.DirectoryExists(subdir).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void SafeDeleteDir_NonExistent_NoException(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.SafeDeleteDir(NewPath(root, "nonexistent"));
	}

	#endregion

	#region CheckInstallation

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckInstallation_WithFiles_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var install = NewPath(root, "install");
		fs.CreateDirectory(install);
		WriteFile(fs, NewPath(root,"install", "app.exe"));

		fs.CheckInstallation(install).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckInstallation_WithSubdirs_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "install", "subdir"));

		fs.CheckInstallation(NewPath(root, "install")).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckInstallation_Empty_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var install = NewPath(root, "install");
		fs.CreateDirectory(install);

		fs.CheckInstallation(install).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckInstallation_NonExistent_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CheckInstallation(NewPath(root, "install")).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckInstallation_EmptyPath_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CheckInstallation("").AssertFalse();
	}

	#endregion

	#region CreateFile

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CreateFile_CreatesFileWithContent(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var content = new byte[] { 1, 2, 3, 4, 5 };

		fs.CreateFile(root, "", "test.bin", content);

		fs.FileExists(NewPath(root, "test.bin")).AssertTrue();
		using var stream = fs.Open(NewPath(root, "test.bin"), FileMode.Open, FileAccess.Read);
		var read = new byte[5];
		stream.ReadExactly(read, 0, 5);
		read.AssertEqual(content);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CreateFile_WithRelativePath_CreatesDirectoryAndFile(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var content = new byte[] { 10, 20, 30 };

		fs.CreateFile(root, "sub/dir", "data.bin", content);

		fs.DirectoryExists(NewPath(root, "sub", "dir")).AssertTrue();
		fs.FileExists(NewPath(root, "sub", "dir", "data.bin")).AssertTrue();
	}

	#endregion

	#region DeleteEmptyDirs

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void DeleteEmptyDirs_DeletesEmptyDirectories(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "a", "b", "c"));
		fs.CreateDirectory(NewPath(root, "d"));

		fs.DeleteEmptyDirs(root);

		fs.DirectoryExists(root).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void DeleteEmptyDirs_KeepsNonEmptyDirectories(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "a", "b"));
		fs.CreateDirectory(NewPath(root, "c"));
		WriteFile(fs, NewPath(root,"a", "file.txt"));

		fs.DeleteEmptyDirs(root);

		fs.DirectoryExists(NewPath(root, "a")).AssertTrue();
		fs.DirectoryExists(NewPath(root, "a", "b")).AssertFalse();
		fs.DirectoryExists(NewPath(root, "c")).AssertFalse();
	}

	#endregion

	#region GetDirectories

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void GetDirectories_ReturnsDirectories(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "a"));
		fs.CreateDirectory(NewPath(root, "b"));
		fs.CreateDirectory(NewPath(root, "c"));

		var dirs = fs.GetDirectories(root).OrderBy(x => x).ToArray();

		dirs.Length.AssertEqual(3);
		dirs[0].ComparePaths(NewPath(root, "a")).AssertTrue(NewPath(root, "a"));
		dirs[1].ComparePaths(NewPath(root, "b")).AssertTrue(NewPath(root, "b"));
		dirs[2].ComparePaths(NewPath(root, "c")).AssertTrue(NewPath(root, "c"));
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void GetDirectories_NonExistent_ReturnsEmpty(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var dirs = fs.GetDirectories(NewPath(root, "nonexistent"));

		dirs.Any().AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void GetDirectories_WithPattern_FiltersResults(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "test1"));
		fs.CreateDirectory(NewPath(root, "test2"));
		fs.CreateDirectory(NewPath(root, "other"));

		var dirs = fs.GetDirectories(root, "test*").ToArray();

		dirs.Length.AssertEqual(2);
	}

	#endregion

	#region GetDirectoriesAsync

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetDirectoriesAsync_ReturnsDirectories(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "dir1"));
		fs.CreateDirectory(NewPath(root, "dir2"));

		var dirs = (await fs.GetDirectoriesAsync(root, cancellationToken: CancellationToken)).ToArray();

		dirs.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetDirectoriesAsync_NonExistent_ReturnsEmpty(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var dirs = await fs.GetDirectoriesAsync(NewPath(root, "nonexistent"), cancellationToken: CancellationToken);

		dirs.Any().AssertFalse();
	}

	#endregion

	#region GetFilesAsync

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetFilesAsync_ReturnsFiles(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"a.txt"), 1);
		WriteFile(fs, NewPath(root,"b.txt"), 2);

		var files = (await fs.GetFilesAsync(root, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();

		files.Length.AssertEqual(2);
		files[0].ComparePaths(NewPath(root, "a.txt")).AssertTrue(NewPath(root, "a.txt"));
		files[1].ComparePaths(NewPath(root, "b.txt")).AssertTrue(NewPath(root, "b.txt"));
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetFilesAsync_NonExistent_ReturnsEmpty(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var files = await fs.GetFilesAsync(NewPath(root, "nonexistent"), cancellationToken: CancellationToken);

		files.Any().AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetFilesAsync_WithPattern_FiltersResults(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"file.txt"), 1);
		WriteFile(fs, NewPath(root,"file.log"), 2);
		WriteFile(fs, NewPath(root,"data.txt"), 3);

		var files = (await fs.GetFilesAsync(root, "*.txt", cancellationToken: CancellationToken)).ToArray();

		files.Length.AssertEqual(2);
	}

	#endregion

	#region Save

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void Save_Stream_SavesContent(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var data = new byte[] { 1, 2, 3, 4, 5 };
		using var source = new MemoryStream(data);

		fs.Save(source, NewPath(root, "output.bin"));

		fs.FileExists(NewPath(root, "output.bin")).AssertTrue();
		using var read = fs.Open(NewPath(root, "output.bin"), FileMode.Open, FileAccess.Read);
		var buffer = new byte[5];
		read.ReadExactly(buffer, 0, 5);
		buffer.AssertEqual(data);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void Save_Stream_RestoresPosition(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var data = new byte[] { 1, 2, 3 };
		using var source = new MemoryStream(data);
		source.Position = 1;

		fs.Save(source, NewPath(root, "output.bin"));

		source.Position.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void Save_ByteArray_SavesContent(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var data = new byte[] { 10, 20, 30, 40 };

		var result = fs.Save(data, NewPath(root, "data.bin"));

		result.AssertSame(data);
		fs.FileExists(NewPath(root, "data.bin")).AssertTrue();
	}

	#endregion

	#region TrySave

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void TrySave_Success_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = fs.TrySave(data, NewPath(root, "file.bin"), ex => caught = ex);

		result.AssertTrue();
		caught.AssertNull();
		fs.FileExists(NewPath(root, "file.bin")).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void TrySave_Failure_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// Directory doesn't exist, should fail with DirectoryNotFoundException
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = fs.TrySave(data, NewPath(root, "nonexistent", "file.bin"), ex => caught = ex);

		result.AssertFalse();
		caught.AssertNotNull();
		(caught is DirectoryNotFoundException).AssertTrue();
	}

	#endregion

	#region CheckDirContainFiles

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainFiles_WithFiles_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"file.txt"));

		fs.CheckDirContainFiles(root).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainFiles_WithFilesInSubdir_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "sub"));
		WriteFile(fs, NewPath(root,"sub", "file.txt"));

		fs.CheckDirContainFiles(root).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainFiles_EmptyDir_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "sub"));

		fs.CheckDirContainFiles(root).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainFiles_NonExistent_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CheckDirContainFiles(NewPath(root, "nonexistent")).AssertFalse();
	}

	#endregion

	#region CheckDirContainsAnything

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainsAnything_WithFiles_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"file.txt"));

		fs.CheckDirContainsAnything(root).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainsAnything_WithSubdir_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "sub"));

		fs.CheckDirContainsAnything(root).AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainsAnything_Empty_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		// root exists and is empty
		fs.CheckDirContainsAnything(root).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void CheckDirContainsAnything_NonExistent_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CheckDirContainsAnything(NewPath(root, "nonexistent")).AssertFalse();
	}

	#endregion

	#region IsFileLocked

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void IsFileLocked_NonExistent_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.IsFileLocked(NewPath(root, "nonexistent.txt")).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void IsFileLocked_UnlockedFile_ReturnsFalse(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"file.txt"));

		fs.IsFileLocked(NewPath(root, "file.txt")).AssertFalse();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public void IsFileLocked_LockedFile_ReturnsTrue(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		using var s = fs.Open(NewPath(root, "file.txt"), FileMode.Create, FileAccess.Write, FileShare.None);
		s.WriteByte(1);
		fs.IsFileLocked(NewPath(root, "file.txt")).AssertTrue();
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
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetFilesAsync_Cancellation_ThrowsOperationCanceled(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		WriteFile(fs, NewPath(root,"f.txt"));

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var thrown = false;
		try
		{
			await fs.GetFilesAsync(root, cancellationToken: cts.Token);
		}
		catch (OperationCanceledException)
		{
			thrown = true;
		}

		thrown.AssertTrue();
	}

	[TestMethod]
	[DataRow(typeof(LocalFileSystem))]
	[DataRow(typeof(MemoryFileSystem))]
	public async Task GetDirectoriesAsync_Materialized_AfterDeleteStillAvailable(Type fsType)
	{
		var (fs, root) = Config.CreateFs(fsType);
		fs.CreateDirectory(NewPath(root, "d1"));
		fs.CreateDirectory(NewPath(root, "d2"));

		var result = await fs.GetDirectoriesAsync(root, cancellationToken: CancellationToken);
		var arr = result.ToArray();

		// remove original directory
		fs.DeleteDirectory(root, true);

		// materialized result should still contain entries
		arr.Length.AssertEqual(2);
	}

	#endregion
}