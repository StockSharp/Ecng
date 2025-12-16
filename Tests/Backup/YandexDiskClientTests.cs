namespace Ecng.Tests.Backup;

using System.IO;

using Ecng.Common;

using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

[TestClass]
[TestCategory("Integration")]
public class YandexDiskClientTests : BaseTestClass
{
	private sealed class Secrets
	{
		public YandexSecrets Yandex { get; init; }
	}

	private sealed class YandexSecrets
	{
		public string Token { get; init; }
	}

	private string LoadToken()
	{
		static string Env(string name) => Environment.GetEnvironmentVariable(name);

		var token = Env("BACKUP_YANDEX_TOKEN");
		var fromFile = TryLoadSecretsFile()?.Yandex?.Token;
		token ??= fromFile;

		if (token.IsEmpty())
			Assert.Inconclusive("Yandex secrets missing. Set BACKUP_YANDEX_TOKEN or provide secrets.json.");

		return token;
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
	public async Task GetDiskInfo()
	{
		var token = LoadToken();

		using var api = new DiskHttpApi(token);
		var disk = await api.MetaInfo.GetDiskInfoAsync(CancellationToken);
		(disk is not null).AssertTrue();
	}

	[TestMethod]
	public async Task Roundtrip_CreateFolder_Upload_Download_Delete()
	{
		var token = LoadToken();

		using var api = new DiskHttpApi(token);

		var folder = "/ecng-yandex-native-tests-" + Guid.NewGuid().ToString("N");
		await api.Commands.CreateDictionaryAsync(folder, CancellationToken);

		var filePath = folder + "/test-" + Guid.NewGuid().ToString("N") + ".bin";
		var data = RandomGen.GetBytes(4096);

		try
		{
			var link = await api.Files.GetUploadLinkAsync(filePath, overwrite: true, cancellationToken: CancellationToken);
			using (var uploadStream = new MemoryStream(data, writable: false))
				await api.Files.UploadAsync(link, uploadStream, CancellationToken);

			var info = await api.MetaInfo.GetInfoAsync(new() { Path = filePath }, CancellationToken);
			info.Size.AssertEqual(data.Length);

			using var downloaded = await api.Files.DownloadFileAsync(filePath, CancellationToken);
			using var ms = new MemoryStream();
			await downloaded.CopyToAsync(ms, CancellationToken);
			ms.ToArray().AssertEqual(data);
		}
		finally
		{
			try
			{
				await api.Commands.DeleteAsync(new() { Path = filePath, Permanently = true }, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await api.Commands.DeleteAsync(new() { Path = folder, Permanently = true }, CancellationToken);
			}
			catch
			{
			}
		}
	}

	[TestMethod]
	public async Task Publish_Unpublish_File()
	{
		var token = LoadToken();

		using var api = new DiskHttpApi(token);

		var folder = "/ecng-yandex-native-publish-tests-" + Guid.NewGuid().ToString("N");
		await api.Commands.CreateDictionaryAsync(folder, CancellationToken);

		var filePath = folder + "/publish-" + Guid.NewGuid().ToString("N") + ".txt";
		var data = "hello " + Guid.NewGuid().ToString("N");

		try
		{
			var link = await api.Files.GetUploadLinkAsync(filePath, overwrite: true, cancellationToken: CancellationToken);
			using (var uploadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data), writable: false))
				await api.Files.UploadAsync(link, uploadStream, CancellationToken);

			var published = await api.MetaInfo.PublishFolderAsync(filePath, CancellationToken);
			published?.Href.IsEmpty().AssertFalse();

			await api.MetaInfo.UnpublishFolderAsync(filePath, CancellationToken);
		}
		finally
		{
			try
			{
				await api.Commands.DeleteAsync(new() { Path = filePath, Permanently = true }, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await api.Commands.DeleteAsync(new() { Path = folder, Permanently = true }, CancellationToken);
			}
			catch
			{
			}
		}
	}
}
