namespace Ecng.Tests.Common;

using System.Text;

[TestClass]
public class FileSystemTests
{
	private static void WithLocalFs(Action<IFileSystem, string> action)
	{
		var root = Config.GetTempPath("Fs");

		try
		{
			Directory.CreateDirectory(root);
			action(new LocalFileSystem(), root);
		}
		finally
		{
			try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
		}
	}

	private static void WithMemoryFs(Action<IFileSystem, string> action)
	{
		var fs = new MemoryFileSystem();
		var root = "memroot";
		fs.CreateDirectory(root);
		action(fs, root);
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
			.SequenceEqual(["a.txt"])
			.AssertTrue();

		fs.EnumerateFiles(root, "*.txt", SearchOption.AllDirectories)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.SequenceEqual(["a.txt", "c.txt", "d.txt", "file.txt", "moved.txt"])
			.AssertTrue();

		fs.EnumerateDirectories(root)
			.Select(Path.GetFileName)
			.OrderBy(s => s)
			.SequenceEqual(["dir1", "dirA", "dirB"])
			.AssertTrue();

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

				s1.Dirs.SequenceEqual(s2.Dirs).AssertTrue();
				s1.Files.SequenceEqual(s2.Files).AssertTrue();

				foreach (var rel in s1.Files)
				{
					s1.Content[rel].SequenceEqual(s2.Content[rel]).AssertTrue();
				}
			});
		});
	}
}