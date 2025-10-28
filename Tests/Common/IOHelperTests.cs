namespace Ecng.Tests.Common;

[TestClass]
public class IOHelperTests : BaseTestClass
{
	private static string CreateTempRoot()
	{
		var path = Config.GetTempPath("Ecng_IOHelperTests");
		Directory.CreateDirectory(path);
		return path;
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_ReturnsDirectories()
	{
		var root = CreateTempRoot();

		try
		{
			Directory.CreateDirectory(Path.Combine(root, "a"));
			Directory.CreateDirectory(Path.Combine(root, "b"));

			var dirs = (await IOHelper.GetDirectoriesAsync(root, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();
			var expected = new[] { Path.Combine(root, "a"), Path.Combine(root, "b") }.OrderBy(x => x).ToArray();

			dirs.AssertEqual(expected);
		}
		finally
		{
			try
			{
				Directory.Delete(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetFilesAsync_ReturnsFiles()
	{
		var root = CreateTempRoot();

		try
		{
			File.WriteAllText(Path.Combine(root, "f1.txt"), "a");
			File.WriteAllText(Path.Combine(root, "f2.txt"), "b");

			var files = (await IOHelper.GetFilesAsync(root, cancellationToken: CancellationToken)).OrderBy(x => x).ToArray();
			var expected = new[] { Path.Combine(root, "f1.txt"), Path.Combine(root, "f2.txt") }.OrderBy(x => x).ToArray();

			files.AssertEqual(expected);
		}
		finally
		{
			try
			{
				Directory.Delete(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_Nonexistent_ReturnsEmpty()
	{
		var path = Config.GetTempPath("NonExistent");
		var res = await IOHelper.GetDirectoriesAsync(path, cancellationToken: CancellationToken);
		res.Any().AssertFalse();
	}

	[TestMethod]
	public async Task GetFilesAsync_Cancellation_ThrowsOperationCanceled()
	{
		var root = CreateTempRoot();

		try
		{
			File.WriteAllText(Path.Combine(root, "f.txt"), "x");

			using var cts = new CancellationTokenSource();
			cts.Cancel();

			var thrown = false;
			try
			{
				await IOHelper.GetFilesAsync(root, cancellationToken: cts.Token);
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
				Directory.Delete(root, true);
			}
			catch { }
		}
	}

	[TestMethod]
	public async Task GetDirectoriesAsync_Materialized_AfterDeleteStillAvailable()
	{
		var root = CreateTempRoot();

		try
		{
			Directory.CreateDirectory(Path.Combine(root, "d1"));
			Directory.CreateDirectory(Path.Combine(root, "d2"));

			var result = await IOHelper.GetDirectoriesAsync(root, cancellationToken: CancellationToken);
			var arr = result.ToArray();

			// remove original directory
			Directory.Delete(root, true);

			// materialized result should still contain entries
			arr.Length.AssertEqual(2);
		}
		finally
		{
			try
			{
				if (Directory.Exists(root))
					Directory.Delete(root, true);
			}
			catch { }
		}
	}
}