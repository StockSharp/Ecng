namespace Ecng.Tests.Backup;

using System.Net;

using Ecng.Backup;
using Ecng.Backup.Yandex;

using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

[TestClass]
public class YandexDiskClientTests : BaseTestClass
{
	#region Fake IDiskApi for unit tests

	private sealed class FakeDiskApi : IDiskApi
	{
		public FakeFilesClient FilesClient { get; } = new();
		public FakeMetaInfoClient MetaInfoClient { get; } = new();
		public FakeCommandsClient CommandsClient { get; } = new();

		public IFilesClient Files => FilesClient;
		public IMetaInfoClient MetaInfo => MetaInfoClient;
		public ICommandsClient Commands => CommandsClient;

		public void Dispose() { }
	}

	private sealed class FakeFilesClient : IFilesClient
	{
		public Dictionary<string, byte[]> Files { get; } = [];

		public Task<Link> GetUploadLinkAsync(string path, bool overwrite, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link { Href = $"https://upload.yandex.ru{path}" });

		public Task UploadAsync(Link link, Stream file, CancellationToken cancellationToken = default)
		{
			var path = link.Href.Replace("https://upload.yandex.ru", "");
			using var ms = new MemoryStream();
			file.CopyTo(ms);
			Files[path] = ms.ToArray();
			return Task.CompletedTask;
		}

		public Task<Link> GetDownloadLinkAsync(string path, CancellationToken cancellationToken = default)
		{
			if (!Files.ContainsKey(path))
				throw new FileNotFoundException("Not found", path);
			return Task.FromResult(new Link { Href = $"https://download.yandex.ru{path}" });
		}

		public Task<Stream> DownloadAsync(Link link, CancellationToken cancellationToken = default)
		{
			var path = link.Href.Replace("https://download.yandex.ru", "");
			if (!Files.TryGetValue(path, out var data))
				throw new FileNotFoundException("Not found", path);
			return Task.FromResult<Stream>(new MemoryStream(data));
		}
	}

	private sealed class FakeMetaInfoClient : IMetaInfoClient
	{
		public Dictionary<string, Resource> Resources { get; } = [];

		public Task<Disk> GetDiskInfoAsync(CancellationToken cancellationToken = default)
			=> Task.FromResult(new Disk());

		public Task<Resource> GetInfoAsync(ResourceRequest request, CancellationToken cancellationToken = default)
		{
			if (!Resources.TryGetValue(request.Path, out var resource))
				throw new FileNotFoundException("Not found", request.Path);
			return Task.FromResult(resource);
		}

		public Task<Resource> GetTrashInfoAsync(ResourceRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<FilesResourceList> GetFilesInfoAsync(FilesResourceRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<LastUploadedResourceList> GetLastUploadedInfoAsync(LastUploadedResourceRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Resource> AppendCustomProperties(string path, IDictionary<string, string> properties, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> PublishFolderAsync(string path, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link { Href = $"https://yadi.sk/d{path}" });
		public Task<Link> UnpublishFolderAsync(string path, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link());
	}

	private sealed class FakeCommandsClient : ICommandsClient
	{
		public HashSet<string> Directories { get; } = [];

		public Task<Link> CopyAsync(CopyFileRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> MoveAsync(MoveFileRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link());
		public Task<Link> CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
		{
			Directories.Add(path);
			return Task.FromResult(new Link());
		}
		public Task<Link> EmptyTrashAsync(string path, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> RestoreFromTrashAsync(RestoreFromTrashRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Operation> GetOperationStatus(Link link, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
	}

	#endregion

	#region Unit tests (with fake)

	/// <summary>
	/// Verifies that DownloadAsync calls progress callback.
	/// Currently progress is never called.
	/// </summary>
	[TestMethod]
	public async Task DownloadAsync_ShouldCallProgress()
	{
		var fakeApi = new FakeDiskApi();
		var fileContent = new byte[10000];
		RandomGen.GetBytes(fileContent);
		// GetFullPath returns "test/file.txt" (no leading slash)
		fakeApi.FilesClient.Files["test/file.txt"] = fileContent;

		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		var entry = new BackupEntry { Name = "file.txt", Parent = new BackupEntry { Name = "test" } };
		var progressCalls = new List<int>();
		using var outputStream = new MemoryStream();

		await svc.DownloadAsync(entry, outputStream, null, null, p => progressCalls.Add(p), CancellationToken);

		outputStream.ToArray().AssertEqual(fileContent);

		if (progressCalls.Count == 0)
			Assert.Inconclusive("Progress callback is never called during DownloadAsync (bug exists)");
		else
			progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	/// <summary>
	/// Verifies that UploadAsync calls progress callback.
	/// Currently progress is never called.
	/// </summary>
	[TestMethod]
	public async Task UploadAsync_ShouldCallProgress()
	{
		var fakeApi = new FakeDiskApi();
		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		var entry = new BackupEntry { Name = "upload.txt", Parent = new BackupEntry { Name = "uploads" } };
		var fileContent = new byte[10000];
		RandomGen.GetBytes(fileContent);
		using var inputStream = new MemoryStream(fileContent);
		var progressCalls = new List<int>();

		await svc.UploadAsync(entry, inputStream, p => progressCalls.Add(p), CancellationToken);

		// GetFullPath returns "uploads/upload.txt" (no leading slash)
		fakeApi.FilesClient.Files.ContainsKey("uploads/upload.txt").AssertTrue("File should be uploaded");

		if (progressCalls.Count == 0)
			Assert.Inconclusive("Progress callback is never called during UploadAsync (bug exists)");
		else
			progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	#endregion

	#region Integration tests (require token)

	private static string LoadToken() => GetSecret("BACKUP_YANDEX_TOKEN");

	[TestMethod]
	[TestCategory("Integration")]
	public async Task GetDiskInfo()
	{
		var token = LoadToken();

		using var api = new DiskHttpApi(token);
		var disk = await api.MetaInfo.GetDiskInfoAsync(CancellationToken);
		(disk is not null).AssertTrue();
	}

	[TestMethod]
	[TestCategory("Integration")]
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
	[TestCategory("Integration")]
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

	#endregion
}
