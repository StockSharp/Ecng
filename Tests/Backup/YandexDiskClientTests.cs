namespace Ecng.Tests.Backup;

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

		public int DisposeCount { get; private set; }

		public void Dispose() => DisposeCount++;
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

		// When set, PublishFolderAsync returns this Href (mimics the cloud-api metadata URL).
		public string PublishHref { get; set; }

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
			=> Task.FromResult(new Link { Href = PublishHref ?? $"https://yadi.sk/d{path}" });
		public Task<Link> UnpublishFolderAsync(string path, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link());
	}

	private sealed class FakeCommandsClient : ICommandsClient
	{
		public HashSet<string> Directories { get; } = [];

		// The HttpStatusCode reported on the Link returned by DeleteAsync.
		public HttpStatusCode DeleteStatusCode { get; set; } = HttpStatusCode.NoContent;

		// Statuses returned by successive GetOperationStatus calls.
		public Queue<OperationStatus> OperationStatuses { get; } = new();

		public int GetOperationStatusCalls { get; private set; }

		public Task<Link> CopyAsync(CopyFileRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> MoveAsync(MoveFileRequest request, CancellationToken cancellationToken = default)
			=> throw new NotImplementedException();
		public Task<Link> DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
			=> Task.FromResult(new Link { HttpStatusCode = DeleteStatusCode, Href = "https://cloud-api.yandex.net/v1/disk/operations/op-1" });
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
		{
			GetOperationStatusCalls++;
			var status = OperationStatuses.Count > 0 ? OperationStatuses.Dequeue() : OperationStatus.Success;
			return Task.FromResult(new Operation { Status = status });
		}
	}

	// Seekable read-only stream with a configurable initial Position, to exercise
	// upload progress against a non-rewound stream.
	private sealed class SeekableReadStream(byte[] data) : Stream
	{
		private readonly byte[] _data = data ?? throw new ArgumentNullException(nameof(data));
		private int _pos;

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _data.Length;

		public override long Position
		{
			get => _pos;
			set => _pos = (int)value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var remaining = _data.Length - _pos;
			if (remaining <= 0 || count <= 0)
				return 0;

			var toRead = Math.Min(count, remaining);
			Array.Copy(_data, _pos, buffer, offset, toRead);
			_pos += toRead;
			return toRead;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			_pos = origin switch
			{
				SeekOrigin.Begin => (int)offset,
				SeekOrigin.Current => _pos + (int)offset,
				SeekOrigin.End => _data.Length + (int)offset,
				_ => _pos,
			};

			return _pos;
		}

		public override void Flush() { }
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	#endregion

	#region Unit tests (with fake)

	/// <summary>
	/// Regression test for download progress reporting: ensures DownloadAsync invokes the
	/// progress callback and reports a final value of 100. (Was: progress was never called,
	/// Backup.Yandex\YandexDiskService.cs:167.)
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

		(progressCalls.Count > 0).AssertTrue("Progress callback should be called during DownloadAsync");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	/// <summary>
	/// Regression test for upload progress reporting: ensures UploadAsync invokes the
	/// progress callback and reports a final value of 100. (Was: progress was never called,
	/// Backup.Yandex\YandexDiskService.cs:205.)
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

		(progressCalls.Count > 0).AssertTrue("Progress callback should be called during UploadAsync");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	/// <summary>
	/// Regression test for PublishAsync: ensures it returns the shareable public
	/// "https://yadi.sk/..." URL (Resource.PublicUrl from a follow-up GetInfoAsync) rather
	/// than the OAuth-protected cloud-api metadata Href from PublishFolderAsync.
	/// (Backup.Yandex\YandexDiskService.cs:279.)
	/// </summary>
	[TestMethod]
	public async Task PublishAsync_ShouldReturnPublicUrl_NotMetadataHref()
	{
		const string publicUrl = "https://yadi.sk/i/qmsXWAtm_WRmIQ";
		const string metadataHref = "https://cloud-api.yandex.net/v1/disk/resources?path=disk%3A%2Ffolder%2Ffile.txt";

		var fakeApi = new FakeDiskApi();
		fakeApi.MetaInfoClient.PublishHref = metadataHref;

		var folder = new BackupEntry { Name = "folder" };
		var entry = new BackupEntry { Name = "file.txt", Parent = folder };

		// After publishing, the resource metadata exposes the public URL.
		fakeApi.MetaInfoClient.Resources[entry.GetFullPath()] = new Resource
		{
			Type = ResourceType.File,
			Name = entry.Name,
			PublicUrl = publicUrl,
		};

		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		var url = await svc.PublishAsync(entry, expiresIn: null, cancellationToken: CancellationToken);

		AreEqual(publicUrl, url);
	}

	/// <summary>
	/// Regression test for asynchronous deletes: ensures DeleteAsync waits for the
	/// server-side operation (via DeleteAndWaitAsync), polling GetOperationStatus at least
	/// once when a non-empty-folder delete returns 202 Accepted. (Was: the raw delete Task
	/// was forwarded and completion reported before the server finished,
	/// Backup.Yandex\YandexDiskService.cs:159.)
	/// </summary>
	[TestMethod]
	public async Task DeleteAsync_OnAcceptedOperation_ShouldWaitForCompletion()
	{
		var fakeApi = new FakeDiskApi();
		fakeApi.CommandsClient.DeleteStatusCode = HttpStatusCode.Accepted;
		fakeApi.CommandsClient.OperationStatuses.Enqueue(OperationStatus.Success);

		var entry = new BackupEntry { Name = "non-empty-folder" };

		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		await svc.DeleteAsync(entry, CancellationToken);

		(fakeApi.CommandsClient.GetOperationStatusCalls > 0)
			.AssertTrue("DeleteAsync must poll the operation status for a 202 Accepted delete.");
	}

	/// <summary>
	/// Regression test for FindAsync against a file parent: ensures a parent path that
	/// resolves to a file (whose Resource.Embedded is null, since the API omits "_embedded"
	/// for files) yields an empty sequence rather than throwing. (Was: info.Embedded.Items
	/// dereferenced unguarded, causing a NullReferenceException,
	/// Backup.Yandex\YandexDiskService.cs:124.)
	/// </summary>
	[TestMethod]
	public async Task FindAsync_WhenParentIsFile_ShouldNotThrowNullReference()
	{
		var fakeApi = new FakeDiskApi();

		var parent = new BackupEntry { Name = "some-file.txt" };

		// FindAsyncImpl queries "<path>/" for the parent; register a file resource
		// (Embedded == null) at that exact path.
		var path = parent.GetFullPath().TrimEnd('/') + "/";
		fakeApi.MetaInfoClient.Resources[path] = new Resource
		{
			Type = ResourceType.File,
			Name = parent.Name,
			Embedded = null,
		};

		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		var found = new List<BackupEntry>();

		await foreach (var item in svc.FindAsync(parent, null).WithCancellation(CancellationToken))
			found.Add(item);

		AreEqual(0, found.Count);
	}

	/// <summary>
	/// Regression test for upload progress on a non-rewound seekable stream: ensures
	/// progress accounts for the stream's current Position (only Length - Position bytes are
	/// uploaded), reporting genuine intermediate values above 50% before completion when half
	/// the stream has already been consumed. (Was: stream.Length used as the progress total,
	/// ignoring Position, so progress topped out around 50% then jumped to 100,
	/// Backup.Yandex\YandexDiskService.cs:205.)
	/// </summary>
	[TestMethod]
	public async Task UploadAsync_NonRewoundStream_ShouldReportProgressFromRemaining()
	{
		var fakeApi = new FakeDiskApi();

		var entry = new BackupEntry { Name = "upload.bin", Parent = new BackupEntry { Name = "uploads" } };

		var data = new byte[10000];
		RandomGen.GetBytes(data);

		using var stream = new SeekableReadStream(data);
		stream.Position = data.Length / 2; // half already consumed

		var progressCalls = new List<int>();

		using var service = new YandexDiskService(fakeApi);
		var svc = (IBackupService)service;

		await svc.UploadAsync(entry, stream, progressCalls.Add, CancellationToken);

		// A genuine intermediate value must land in (50, 100): when Position is ignored only
		// 0..~50 plus the 100 fallback are reported, with nothing in between.
		progressCalls.Any(p => p > 50 && p < 100)
			.AssertTrue("Progress must account for the stream Position, reporting values above 50% before completion.");
	}

	/// <summary>
	/// Regression test for client ownership: ensures a client injected via the
	/// YandexDiskService(IDiskApi) constructor is owned by the caller and is NOT disposed by
	/// the service, so the shared (cache-and-reuse) client keeps working for other consumers.
	/// (Was: DisposeManaged unconditionally disposed the injected client,
	/// Backup.Yandex\YandexDiskService.cs:83.)
	/// </summary>
	[TestMethod]
	public void Dispose_ShouldNotDisposeInjectedClient()
	{
		var fakeApi = new FakeDiskApi();

		using (var service = new YandexDiskService(fakeApi))
		{
			var svc = (IBackupService)service;
			(svc.CanFolders).AssertTrue();
		}

		AreEqual(0, fakeApi.DisposeCount);
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
