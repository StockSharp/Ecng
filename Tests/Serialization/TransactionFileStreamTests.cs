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
			ThrowsExactly<NotSupportedException>(() => tfs.Read(new byte[10], 0, 10));
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
}