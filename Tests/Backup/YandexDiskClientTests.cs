namespace Ecng.Tests.Backup;

using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

[TestClass]
[TestCategory("Integration")]
public class YandexDiskClientTests : BaseTestClass
{
	private static string LoadToken() => GetSecret("BACKUP_YANDEX_TOKEN");

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
		await api.Commands.CreateDirectoryAsync(folder, CancellationToken);

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
		await api.Commands.CreateDirectoryAsync(folder, CancellationToken);

		var filePath = folder + "/publish-" + Guid.NewGuid().ToString("N") + ".txt";
		var data = "hello " + Guid.NewGuid().ToString("N");

		try
		{
			var link = await api.Files.GetUploadLinkAsync(filePath, overwrite: true, cancellationToken: CancellationToken);
			using (var uploadStream = new MemoryStream(data.UTF8(), writable: false))
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
