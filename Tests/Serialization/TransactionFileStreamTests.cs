namespace Ecng.Tests.Serialization;

using System.Text;

using Ecng.Serialization;

[TestClass]
public class TransactionFileStreamTests : BaseTestClass
{
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
}