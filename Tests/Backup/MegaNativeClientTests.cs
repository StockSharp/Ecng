namespace Ecng.Tests.Backup;

using System.IO;

using Ecng.Backup.Mega.Native;
using Ecng.Tests;

[TestClass]
[TestCategory("Integration")]
public class MegaNativeClientTests : BaseTestClass
{
	private sealed class Secrets
	{
		public MegaSecrets Mega { get; init; }
	}

	private sealed class MegaSecrets
	{
		public string Email { get; init; }
		public string Password { get; init; }
	}

	private static MegaSecrets LoadMegaSecrets()
	{
		static string Env(string name) => Environment.GetEnvironmentVariable(name);

		var email = Env("BACKUP_MEGA_EMAIL");
		var password = Env("BACKUP_MEGA_PASSWORD");

		var fromFile = TryLoadSecretsFile()?.Mega;

		email ??= fromFile?.Email;
		password ??= fromFile?.Password;

		if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
			Assert.Inconclusive("Mega secrets missing. Set BACKUP_MEGA_EMAIL and BACKUP_MEGA_PASSWORD or provide secrets.json.");

		return new MegaSecrets { Email = email, Password = password };
	}

	private static Secrets TryLoadSecretsFile()
	{
		const string secretsFileName = "secrets.json";

		var explicitPath = Environment.GetEnvironmentVariable("BACKUP_SECRETS_FILE");
		if (!explicitPath.IsEmpty() && File.Exists(explicitPath))
			return explicitPath.DeserializeSecrets<Secrets>();

		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		for (var i = 0; i < 8 && dir != null; i++)
		{
			var candidate = Path.Combine(dir.FullName, secretsFileName);
			if (File.Exists(candidate))
				return candidate.DeserializeSecrets<Secrets>();
			dir = dir.Parent;
		}

		return null;
	}

	[TestMethod]
	public async Task Login_And_GetNodes()
	{
		var s = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(s.Email, s.Password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		nodes.Any(n => n.Type == NodeType.Root).AssertTrue();
	}

	[TestMethod]
	public async Task Roundtrip_Upload_Download_Delete()
	{
		var s = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(s.Email, s.Password, CancellationToken);

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
		var s = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(s.Email, s.Password, CancellationToken);

		client.IsLoggedIn.AssertTrue();

		await client.LogoutAsync(CancellationToken);

		client.IsLoggedIn.AssertFalse();
		await ThrowsExactlyAsync<InvalidOperationException>(() => client.GetNodesAsync(CancellationToken));

		await client.LoginAsync(s.Email, s.Password, CancellationToken);
		client.IsLoggedIn.AssertTrue();
	}

	[TestMethod]
	public async Task Publish_Unpublish_File()
	{
		var s = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(s.Email, s.Password, CancellationToken);

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
			string.IsNullOrEmpty(url).AssertFalse();

			url.Contains("/file/", StringComparison.OrdinalIgnoreCase).AssertTrue(url);
			url.Contains('#', StringComparison.Ordinal).AssertTrue(url);

			var phStart = url.IndexOf("/file/", StringComparison.OrdinalIgnoreCase);
			(phStart >= 0).AssertTrue(url);
			phStart += "/file/".Length;

			var hashPos = url.IndexOf('#', phStart);
			(hashPos > phStart).AssertTrue(url);

			var publicHandle = url.Substring(phStart, hashPos - phStart);
			string.IsNullOrEmpty(publicHandle).AssertFalse();

			var dl = await client.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
			string.IsNullOrEmpty(dl.Url).AssertFalse();
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
