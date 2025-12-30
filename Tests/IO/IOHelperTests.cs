namespace Ecng.Tests.IO;

using Ecng.IO;

[TestClass]
public class IOHelperTests : BaseTestClass
{
	#region CreateDirIfNotExists

	[TestMethod]
	public void CreateDirIfNotExists_Memory_CreatesDirectory()
	{
		var fs = new MemoryFileSystem();
		var result = "/root/subdir/file.txt".CreateDirIfNotExists(fs);

		result.AssertTrue();
		fs.DirectoryExists("/root/subdir").AssertTrue();
	}

	[TestMethod]
	public void CreateDirIfNotExists_Memory_ExistingDir_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/subdir");

		var result = "/root/subdir/file.txt".CreateDirIfNotExists(fs);

		result.AssertFalse();
	}

	[TestMethod]
	public void CreateDirIfNotExists_Memory_EmptyDir_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		var result = "file.txt".CreateDirIfNotExists(fs);

		result.AssertFalse();
	}

	#endregion

	#region SafeDeleteDir

	[TestMethod]
	public void SafeDeleteDir_Memory_DeletesDirectory()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/subdir");
		using (var s = fs.Open("/root/subdir/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		"/root/subdir".SafeDeleteDir(fs);

		fs.DirectoryExists("/root/subdir").AssertFalse();
	}

	[TestMethod]
	public void SafeDeleteDir_Memory_NonExistent_NoException()
	{
		var fs = new MemoryFileSystem();
		"/nonexistent".SafeDeleteDir(fs);
	}

	#endregion

	#region CheckInstallation

	[TestMethod]
	public void CheckInstallation_Memory_WithFiles_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/install");
		using (var s = fs.Open("/install/app.exe", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.CheckInstallation("/install", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckInstallation_Memory_WithSubdirs_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/install/subdir");

		IOHelper.CheckInstallation("/install", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckInstallation_Memory_Empty_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/install");

		IOHelper.CheckInstallation("/install", fs).AssertFalse();
	}

	[TestMethod]
	public void CheckInstallation_Memory_NonExistent_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		IOHelper.CheckInstallation("/install", fs).AssertFalse();
	}

	[TestMethod]
	public void CheckInstallation_Memory_EmptyPath_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		IOHelper.CheckInstallation("", fs).AssertFalse();
	}

	#endregion

	#region CreateFile

	[TestMethod]
	public void CreateFile_Memory_CreatesFileWithContent()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var content = new byte[] { 1, 2, 3, 4, 5 };

		IOHelper.CreateFile("/root", "", "test.bin", content, fs);

		fs.FileExists("/root/test.bin").AssertTrue();
		using var stream = fs.Open("/root/test.bin", FileMode.Open, FileAccess.Read);
		var read = new byte[5];
		stream.ReadExactly(read, 0, 5);
		read.AssertEqual(content);
	}

	[TestMethod]
	public void CreateFile_Memory_WithRelativePath_CreatesDirectoryAndFile()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var content = new byte[] { 10, 20, 30 };

		IOHelper.CreateFile("/root", "sub/dir", "data.bin", content, fs);

		fs.DirectoryExists("/root/sub/dir").AssertTrue();
		fs.FileExists("/root/sub/dir/data.bin").AssertTrue();
	}

	#endregion

	#region DeleteEmptyDirs

	[TestMethod]
	public void DeleteEmptyDirs_Memory_DeletesEmptyDirectories()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/a/b/c");
		fs.CreateDirectory("/root/d");

		IOHelper.DeleteEmptyDirs("/root", fs);

		fs.DirectoryExists("/root").AssertFalse();
	}

	[TestMethod]
	public void DeleteEmptyDirs_Memory_KeepsNonEmptyDirectories()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/a/b");
		fs.CreateDirectory("/root/c");
		using (var s = fs.Open("/root/a/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.DeleteEmptyDirs("/root", fs);

		fs.DirectoryExists("/root/a").AssertTrue();
		fs.DirectoryExists("/root/a/b").AssertFalse();
		fs.DirectoryExists("/root/c").AssertFalse();
	}

	#endregion

	#region GetDirectories

	[TestMethod]
	public void GetDirectories_Memory_ReturnsDirectories()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/a");
		fs.CreateDirectory("/root/b");
		fs.CreateDirectory("/root/c");

		var dirs = IOHelper.GetDirectories("/root", fs).OrderBy(x => x).ToArray();

		dirs.Length.AssertEqual(3);
		dirs[0].ComparePaths("/root/a").AssertTrue("/root/a");
		dirs[1].ComparePaths("/root/b").AssertTrue("/root/b");
		dirs[2].ComparePaths("/root/c").AssertTrue("/root/c");
	}

	[TestMethod]
	public void GetDirectories_Memory_NonExistent_ReturnsEmpty()
	{
		var fs = new MemoryFileSystem();
		var dirs = IOHelper.GetDirectories("/nonexistent", fs);

		dirs.Any().AssertFalse();
	}

	[TestMethod]
	public void GetDirectories_Memory_WithPattern_FiltersResults()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/test1");
		fs.CreateDirectory("/root/test2");
		fs.CreateDirectory("/root/other");

		var dirs = IOHelper.GetDirectories("/root", fs, "test*").ToArray();

		dirs.Length.AssertEqual(2);
	}

	#endregion

	#region GetDirectoriesAsync

	[TestMethod]
	public async Task GetDirectoriesAsync_Memory_ReturnsDirectories()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/dir1");
		fs.CreateDirectory("/root/dir2");

		var dirs = (await IOHelper.GetDirectoriesAsync("/root", fs, cancellationToken: CancellationToken)).ToArray();

		dirs.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_Memory_NonExistent_ReturnsEmpty()
	{
		var fs = new MemoryFileSystem();
		var dirs = await IOHelper.GetDirectoriesAsync("/nonexistent", fs, cancellationToken: CancellationToken);

		dirs.Any().AssertFalse();
	}

	#endregion

	#region GetFilesAsync

	[TestMethod]
	public async Task GetFilesAsync_Memory_ReturnsFiles()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		using (var s = fs.Open("/root/a.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);
		using (var s = fs.Open("/root/b.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(2);

		var files = (await IOHelper.GetFilesAsync("/root", fs, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();

		files.Length.AssertEqual(2);
		files[0].ComparePaths("/root/a.txt").AssertTrue("/root/a.txt");
		files[1].ComparePaths("/root/b.txt").AssertTrue("/root/b.txt");
	}

	[TestMethod]
	public async Task GetFilesAsync_Memory_NonExistent_ReturnsEmpty()
	{
		var fs = new MemoryFileSystem();
		var files = await IOHelper.GetFilesAsync("/nonexistent", fs, cancellationToken: CancellationToken);

		files.Any().AssertFalse();
	}

	[TestMethod]
	public async Task GetFilesAsync_Memory_WithPattern_FiltersResults()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		using (var s = fs.Open("/root/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);
		using (var s = fs.Open("/root/file.log", FileMode.Create, FileAccess.Write))
			s.WriteByte(2);
		using (var s = fs.Open("/root/data.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(3);

		var files = (await IOHelper.GetFilesAsync("/root", fs, "*.txt", cancellationToken: CancellationToken)).ToArray();

		files.Length.AssertEqual(2);
	}

	#endregion

	#region Save

	[TestMethod]
	public void Save_Stream_Memory_SavesContent()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var data = new byte[] { 1, 2, 3, 4, 5 };
		using var source = new MemoryStream(data);

		source.Save("/root/output.bin", fs);

		fs.FileExists("/root/output.bin").AssertTrue();
		using var read = fs.Open("/root/output.bin", FileMode.Open, FileAccess.Read);
		var buffer = new byte[5];
		read.ReadExactly(buffer, 0, 5);
		buffer.AssertEqual(data);
	}

	[TestMethod]
	public void Save_Stream_Memory_RestoresPosition()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var data = new byte[] { 1, 2, 3 };
		using var source = new MemoryStream(data);
		source.Position = 1;

		source.Save("/root/output.bin", fs);

		source.Position.AssertEqual(1);
	}

	[TestMethod]
	public void Save_ByteArray_Memory_SavesContent()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var data = new byte[] { 10, 20, 30, 40 };

		var result = data.Save("/root/data.bin", fs);

		result.AssertSame(data);
		fs.FileExists("/root/data.bin").AssertTrue();
	}

	#endregion

	#region TrySave

	[TestMethod]
	public void TrySave_Memory_Success_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = data.TrySave("/root/file.bin", fs, ex => caught = ex);

		result.AssertTrue();
		caught.AssertNull();
		fs.FileExists("/root/file.bin").AssertTrue();
	}

	//[TestMethod]
	public void TrySave_Memory_Failure_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		// Directory doesn't exist, should fail with DirectoryNotFoundException
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = data.TrySave("/nonexistent/file.bin", fs, ex => caught = ex);

		result.AssertFalse();
		caught.AssertNotNull();
		(caught is DirectoryNotFoundException).AssertTrue();
	}

	[TestMethod]
	public void TrySave_LocalFileSystem_Failure_ReturnsFalse()
	{
		var fs = LocalFileSystem.Instance;
		// Directory doesn't exist, should fail with DirectoryNotFoundException
		var data = new byte[] { 1, 2, 3 };
		Exception caught = null;

		var result = data.TrySave("/nonexistent/file.bin", fs, ex => caught = ex);

		result.AssertFalse();
		caught.AssertNotNull();
		(caught is DirectoryNotFoundException).AssertTrue();
	}

	#endregion

	#region CheckDirContainFiles

	[TestMethod]
	public void CheckDirContainFiles_Memory_WithFiles_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		using (var s = fs.Open("/root/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.CheckDirContainFiles("/root", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckDirContainFiles_Memory_WithFilesInSubdir_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/sub");
		using (var s = fs.Open("/root/sub/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.CheckDirContainFiles("/root", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckDirContainFiles_Memory_EmptyDir_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/sub");

		IOHelper.CheckDirContainFiles("/root", fs).AssertFalse();
	}

	[TestMethod]
	public void CheckDirContainFiles_Memory_NonExistent_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();

		IOHelper.CheckDirContainFiles("/nonexistent", fs).AssertFalse();
	}

	#endregion

	#region CheckDirContainsAnything

	[TestMethod]
	public void CheckDirContainsAnything_Memory_WithFiles_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		using (var s = fs.Open("/root/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.CheckDirContainsAnything("/root", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckDirContainsAnything_Memory_WithSubdir_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root/sub");

		IOHelper.CheckDirContainsAnything("/root", fs).AssertTrue();
	}

	[TestMethod]
	public void CheckDirContainsAnything_Memory_Empty_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");

		IOHelper.CheckDirContainsAnything("/root", fs).AssertFalse();
	}

	[TestMethod]
	public void CheckDirContainsAnything_Memory_NonExistent_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();

		IOHelper.CheckDirContainsAnything("/nonexistent", fs).AssertFalse();
	}

	#endregion

	#region IsFileLocked

	[TestMethod]
	public void IsFileLocked_Memory_NonExistent_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();

		IOHelper.IsFileLocked("/nonexistent.txt", fs).AssertFalse();
	}

	[TestMethod]
	public void IsFileLocked_Memory_UnlockedFile_ReturnsFalse()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");
		using (var s = fs.Open("/root/file.txt", FileMode.Create, FileAccess.Write))
			s.WriteByte(1);

		IOHelper.IsFileLocked("/root/file.txt", fs).AssertFalse();
	}

	[TestMethod]
	public void IsFileLocked_Memory_LockedFile_ReturnsTrue()
	{
		var fs = new MemoryFileSystem();
		fs.CreateDirectory("/root");

		using (var s = fs.Open("/root/file.txt", FileMode.Create, FileAccess.Write, FileShare.None))
		{
			s.WriteByte(1);
			IOHelper.IsFileLocked("/root/file.txt", fs).AssertTrue();
		}
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

	[TestMethod]
	public async Task GetDirectoriesAsync_ReturnsDirectories()
	{
		var fs = LocalFileSystem.Instance;
		var root = fs.GetTempPath();

		try
		{
			fs.CreateDirectory(Path.Combine(root, "a"));
			fs.CreateDirectory(Path.Combine(root, "b"));

			var dirs = (await IOHelper.GetDirectoriesAsync(root, fs, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();
			var expected = new[] { Path.Combine(root, "a"), Path.Combine(root, "b") }.OrderBy(x => x).ToArray();

			dirs.AssertEqual(expected);
		}
		finally
		{
			try
			{
				fs.DeleteDirectory(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetFilesAsync_ReturnsFiles()
	{
		var fs = LocalFileSystem.Instance;
		var root = fs.GetTempPath();

		try
		{
			fs.WriteAllText(Path.Combine(root, "f1.txt"), "a");
			fs.WriteAllText(Path.Combine(root, "f2.txt"), "b");

			var files = (await IOHelper.GetFilesAsync(root, fs, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();
			var expected = new[] { Path.Combine(root, "f1.txt"), Path.Combine(root, "f2.txt") }.OrderBy(x => x).ToArray();

			files.AssertEqual(expected);
		}
		finally
		{
			try
			{
				fs.DeleteDirectory(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_Nonexistent_ReturnsEmpty()
	{
		var fs = LocalFileSystem.Instance;
		var path = fs.GetTempPath("NonExistent");
		var res = await IOHelper.GetDirectoriesAsync(path, fs, cancellationToken: CancellationToken);
		res.Any().AssertFalse();
	}

	[TestMethod]
	public async Task GetFilesAsync_Cancellation_ThrowsOperationCanceled()
	{
		var fs = LocalFileSystem.Instance;
		var root = fs.GetTempPath();

		try
		{
			fs.WriteAllText(Path.Combine(root, "f.txt"), "x");

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			var thrown = false;
			try
			{
				await IOHelper.GetFilesAsync(root, fs, cancellationToken: cts.Token);
			}
			catch (OperationCanceledException)
			{
				thrown = true;
			}

			thrown.AssertTrue();
		}
		finally
		{
			try
			{
				fs.DeleteDirectory(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_Materialized_AfterDeleteStillAvailable()
	{
		var fs = LocalFileSystem.Instance;
		var root = fs.GetTempPath();

		try
		{
			fs.CreateDirectory(Path.Combine(root, "d1"));
			fs.CreateDirectory(Path.Combine(root, "d2"));

			var result = await IOHelper.GetDirectoriesAsync(root, fs, cancellationToken: CancellationToken);
			var arr = result.ToArray();

			// remove original directory
			fs.DeleteDirectory(root, true);

			// materialized result should still contain entries
			arr.Length.AssertEqual(2);
		}
		finally
		{
			try
			{
				if (fs.DirectoryExists(root))
					fs.DeleteDirectory(root, true);
			}
			catch { }
		}
	}
}