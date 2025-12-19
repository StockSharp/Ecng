namespace Ecng.Tests.Backup;

using Ecng.Backup.Mega.Native;

[TestClass]
[TestCategory("Integration")]
public class MegaNativeClientTests : BaseTestClass
{
	private (string email, string password) LoadMegaSecrets()
		=> (GetSecret("BACKUP_MEGA_EMAIL"), GetSecret("BACKUP_MEGA_PASSWORD"));

	[TestMethod]
	public async Task Login_And_GetNodes()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		nodes.Any(n => n.Type == NodeType.Root).AssertTrue();
	}

	[TestMethod]
	public async Task Roundtrip_Upload_Download_Delete()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		var root = nodes.First(n => n.Type == NodeType.Root);

		var folder = await client.CreateFolderAsync(root.Id, "ecng-mega-native-tests-" + Guid.NewGuid().ToString("N"), CancellationToken);

		var data = RandomGen.GetBytes(4096);

		var fileName = $"test-{Guid.NewGuid():N}.bin";

		Node fileNode = null;

		try
		{
			using (var uploadStream = new MemoryStream(data, writable: false))
				fileNode = await client.UploadAsync(folder.Id, fileName, uploadStream, modificationDate: null, progress: null, CancellationToken);

			using var downloadStream = new MemoryStream();
			await client.DownloadAsync(fileNode, downloadStream, progress: null, CancellationToken);
			downloadStream.ToArray().AssertEqual(data);
		}
		finally
		{
			try
			{
				if (fileNode is not null)
					await client.DeleteAsync(fileNode.Id, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await client.DeleteAsync(folder.Id, CancellationToken);
			}
			catch
			{
			}
		}
	}

	[TestMethod]
	public async Task Logout_InvalidatesSession()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		client.IsLoggedIn.AssertTrue();

		await client.LogoutAsync(CancellationToken);

		client.IsLoggedIn.AssertFalse();
		await ThrowsAsync<InvalidOperationException>(() => client.GetNodesAsync(CancellationToken));

		await client.LoginAsync(email, password, CancellationToken);
		client.IsLoggedIn.AssertTrue();
	}

	[TestMethod]
	public async Task Publish_Unpublish_File()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		var root = nodes.First(n => n.Type == NodeType.Root);

		var folder = await client.CreateFolderAsync(root.Id, "ecng-mega-native-publish-tests-" + Guid.NewGuid().ToString("N"), CancellationToken);

		var data = RandomGen.GetBytes(1024);

		var fileName = $"publish-{Guid.NewGuid():N}.bin";

		Node fileNode = null;

		try
		{
			using (var uploadStream = new MemoryStream(data, writable: false))
				fileNode = await client.UploadAsync(folder.Id, fileName, uploadStream, modificationDate: null, progress: null, CancellationToken);

			var url = await client.PublishAsync(fileNode, CancellationToken);
			url.IsEmpty().AssertFalse();

			url.Contains("/file/", StringComparison.OrdinalIgnoreCase).AssertTrue(url);
			url.Contains('#', StringComparison.Ordinal).AssertTrue(url);

			var phStart = url.IndexOf("/file/", StringComparison.OrdinalIgnoreCase);
			(phStart >= 0).AssertTrue(url);
			phStart += "/file/".Length;

			var hashPos = url.IndexOf('#', phStart);
			(hashPos > phStart).AssertTrue(url);

			var publicHandle = url.Substring(phStart, hashPos - phStart);
			publicHandle.IsEmpty().AssertFalse();

			var dl = await client.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
			dl.Url.IsEmpty().AssertFalse();
			(dl.Size > 0).AssertTrue();

			await client.UnpublishAsync(fileNode.Id, CancellationToken);

			var resolvedAfterUnpublish = false;

			for (var i = 0; i < 5; i++)
			{
				try
				{
					await client.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
					resolvedAfterUnpublish = true;
					await Task.Delay(250, CancellationToken);
				}
				catch (InvalidOperationException)
				{
					resolvedAfterUnpublish = false;
					break;
				}
			}

			resolvedAfterUnpublish.AssertFalse("Public link still resolves after unpublish.");
		}
		finally
		{
			try
			{
				if (fileNode is not null)
					await client.DeleteAsync(fileNode.Id, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await client.DeleteAsync(folder.Id, CancellationToken);
			}
			catch
			{
			}
		}
	}
}
