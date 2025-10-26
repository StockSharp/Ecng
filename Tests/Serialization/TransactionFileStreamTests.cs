namespace Ecng.Tests.Serialization;

using System.Text;

using Ecng.Serialization;

[TestClass]
public class TransactionFileStreamTests
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
				var data = Encoding.UTF8.GetBytes("hello");
				tfs.Write(data,0, data.Length);
			}

			Assert.IsTrue(File.Exists(target));
			ReadAllText(target).AssertEqual("hello");
			Assert.IsFalse(File.Exists(tmp));
		}
		finally
		{
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(tmp)) File.Delete(tmp);
		}
	}

	[TestMethod]
	public void Open_NonExisting_Throws()
	{
		var target = NewTempFilePath();
		try
		{
			Assert.ThrowsExactly<FileNotFoundException>(() => new TransactionFileStream(target, FileMode.Open));
		}
		finally
		{
			var tmp = target + ".tmp";
			if (File.Exists(tmp)) File.Delete(tmp);
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
				var data = Encoding.UTF8.GetBytes("abc");
				tfs.Write(data,0, data.Length);
			}

			ReadAllText(target).AssertEqual("abc");
			Assert.IsFalse(File.Exists(tmp));
		}
		finally
		{
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(tmp)) File.Delete(tmp);
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
				var data = Encoding.UTF8.GetBytes("+end");
				tfs.Write(data,0, data.Length);
			}

			ReadAllText(target).AssertEqual("start+end");
			Assert.IsFalse(File.Exists(tmp));
		}
		finally
		{
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(tmp)) File.Delete(tmp);
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
				var data = Encoding.UTF8.GetBytes("short");
				tfs.Write(data,0, data.Length);
			}

			ReadAllText(target).AssertEqual("short");
			Assert.IsFalse(File.Exists(tmp));
		}
		finally
		{
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(tmp)) File.Delete(tmp);
		}
	}

	[TestMethod]
	public void AfterDispose_ThrowsObjectDisposed()
	{
		var target = NewTempFilePath();
		var stream = new TransactionFileStream(target, FileMode.Create);

		stream.Dispose();

		Assert.ThrowsExactly<ObjectDisposedException>(() => stream.Write(new byte[] {1 },0,1));
		Assert.ThrowsExactly<ObjectDisposedException>(() => { var _ = stream.Length; });
		Assert.ThrowsExactly<ObjectDisposedException>(() => { stream.Position =0; });
	}
}
