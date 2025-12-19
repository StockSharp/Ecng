namespace Ecng.Tests.Backup;

using System.Net;
using System.Net.Http;

using Ecng.Backup;
using Ecng.Backup.Amazon;
using Ecng.Backup.Azure;
using Ecng.Backup.Mega;
using Ecng.Backup.Yandex;

[TestClass]
[TestCategory("Integration")]
public class BackupServicesTests : BaseTestClass
{
	private static async Task RoundtripAsync(IBackupService service, string serviceName, CancellationToken cancellationToken)
	{
		var folder = service.CanFolders ? new BackupEntry { Name = "ecng-tests-" + Guid.NewGuid().ToString("N") } : null;
		if (folder is not null)
			await service.CreateFolder(folder, cancellationToken);

		var entry = new BackupEntry
		{
			Name = $"test-{Guid.NewGuid():N}.bin",
			Parent = folder,
		};

		var data = RandomGen.GetBytes(4096);

		using (var uploadStream = new MemoryStream(data, writable: false))
			await service.UploadAsync(entry, uploadStream, _ => { }, cancellationToken);

		var found = new List<BackupEntry>();
		await foreach (var f in service.FindAsync(folder, entry.Name, cancellationToken))
			found.Add(f);

		(found.Any(f => f.Name.EqualsIgnoreCase(entry.Name))).AssertTrue($"{serviceName}: uploaded entry not found");

		await service.FillInfoAsync(entry, cancellationToken);
		entry.Size.AssertEqual(data.Length);

		using var downloadStream = new MemoryStream();
		await service.DownloadAsync(entry, downloadStream, null, null, _ => { }, cancellationToken);
		downloadStream.ToArray().AssertEqual(data);

		await service.DeleteAsync(entry, cancellationToken);

		if (folder is not null)
			await service.DeleteAsync(folder, cancellationToken);
	}

	[TestMethod]
	public async Task AwsS3_Roundtrip()
	{
		var region = GetSecret("BACKUP_AWS_REGION");
		var bucket = GetSecret("BACKUP_AWS_BUCKET");
		var accessKey = GetSecret("BACKUP_AWS_ACCESS_KEY");
		var secretKey = GetSecret("BACKUP_AWS_SECRET_KEY");

		using var svc = new AmazonS3Service(region, bucket, accessKey, secretKey);
		await RoundtripAsync(svc, "AWS S3", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task AwsS3_Publish_Unpublish()
	{
		var region = GetSecret("BACKUP_AWS_REGION");
		var bucket = GetSecret("BACKUP_AWS_BUCKET");
		var accessKey = GetSecret("BACKUP_AWS_ACCESS_KEY");
		var secretKey = GetSecret("BACKUP_AWS_SECRET_KEY");

		using IBackupService svc = new AmazonS3Service(region, bucket, accessKey, secretKey);

		var entry = new BackupEntry { Name = $"publish-{Guid.NewGuid():N}.txt" };
		var data = "hello " + Guid.NewGuid();

		using (var uploadStream = new MemoryStream(data.UTF8(), writable: false))
			await svc.UploadAsync(entry, uploadStream, _ => { }, CancellationToken);

		try
		{
			var url = await svc.PublishAsync(entry, expiresIn: TimeSpan.FromMinutes(5), cancellationToken: CancellationToken);

			url.IsEmpty().AssertFalse();

			using (var http = new HttpClient())
			{
				var downloaded = await http.GetByteArrayAsync(url, CancellationToken);
				downloaded.UTF8().AssertEqual(data);
			}

			await svc.UnPublishAsync(entry, CancellationToken);
		}
		finally
		{
			await svc.DeleteAsync(entry, CancellationToken);
		}
	}

	[TestMethod]
	public async Task AzureBlob_Roundtrip()
	{
		if (IsLocalHost)
			Inconclusive("Azure tests are skipped on localhost.");

		var connectionString = GetSecret("BACKUP_AZURE_CONNECTION_STRING");
		var container = GetSecret("BACKUP_AZURE_CONTAINER");

		using var svc = new AzureBlobService(connectionString, container);
		await RoundtripAsync(svc, "Azure Blob", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task Mega_Roundtrip()
	{
		var email = GetSecret("BACKUP_MEGA_EMAIL");
		var password = GetSecret("BACKUP_MEGA_PASSWORD");

		using IBackupService svc = new MegaService(email, password.Secure());
		await RoundtripAsync(svc, "Mega", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task Mega_Publish_Unpublish()
	{
		var email = GetSecret("BACKUP_MEGA_EMAIL");
		var password = GetSecret("BACKUP_MEGA_PASSWORD");

		using IBackupService svc = new MegaService(email, password.Secure());

		var folder = new BackupEntry { Name = "ecng-publish-tests-" + Guid.NewGuid().ToString("N") };
		await svc.CreateFolder(folder, CancellationToken);

		var entry = new BackupEntry
		{
			Name = $"publish-{Guid.NewGuid():N}.bin",
			Parent = folder,
		};

		var data = RandomGen.GetBytes(1024);

		using (var uploadStream = new MemoryStream(data, writable: false))
			await svc.UploadAsync(entry, uploadStream, _ => { }, CancellationToken);

		var url = await svc.PublishAsync(entry, cancellationToken: CancellationToken);
		url.IsEmpty().AssertFalse();

		var phStart = url.IndexOf("/file/", StringComparison.OrdinalIgnoreCase);
		(phStart >= 0).AssertTrue(url);
		phStart += "/file/".Length;

		var hashPos = url.IndexOf('#', phStart);
		(hashPos > phStart).AssertTrue(url);

		var publicHandle = url.Substring(phStart, hashPos - phStart);
		publicHandle.IsEmpty().AssertFalse();

		using (var native = new Ecng.Backup.Mega.Native.Client())
		{
			await native.LoginAsync(email, password, CancellationToken);
			var dl = await native.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
			dl.Url.IsEmpty().AssertFalse();
		}

		await svc.UnPublishAsync(entry, CancellationToken);

		using (var native = new Ecng.Backup.Mega.Native.Client())
		{
			await native.LoginAsync(email, password, CancellationToken);
			await ThrowsAsync<InvalidOperationException>(() => native.GetPublicDownloadUrlAsync(publicHandle, CancellationToken));
		}

		await svc.DeleteAsync(entry, CancellationToken);
		await svc.DeleteAsync(folder, CancellationToken);
	}

	[TestMethod]
	public async Task YandexDisk_Roundtrip()
	{
		var token = GetSecret("BACKUP_YANDEX_TOKEN");

		using var svc = new YandexDiskService(token.Secure());
		await RoundtripAsync(svc, "Yandex Disk", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task YandexDisk_Publish_Unpublish()
	{
		var token = GetSecret("BACKUP_YANDEX_TOKEN");

		using IBackupService svc = new YandexDiskService(token.Secure());

		var folder = new BackupEntry { Name = "ecng-yandex-publish-tests-" + Guid.NewGuid().ToString("N") };
		var entry = new BackupEntry
		{
			Name = $"publish-{Guid.NewGuid():N}.txt",
			Parent = folder,
		};

		var fullPath = entry.GetFullPath();

		try
		{
			await svc.CreateFolder(folder, CancellationToken);

			using (var uploadStream = new MemoryStream(("hello " + Guid.NewGuid()).UTF8(), writable: false))
				await svc.UploadAsync(entry, uploadStream, _ => { }, CancellationToken);

			var url = await svc.PublishAsync(entry, cancellationToken: CancellationToken);
			url.IsEmpty().AssertFalse();

			using var api = new YandexDisk.Client.Http.DiskHttpApi(token);

			var published = false;

			for (var i = 0; i < 120; i++)
			{
				YandexDisk.Client.Protocol.Resource info;

				try
				{
					info = await api.MetaInfo.GetInfoAsync(new() { Path = fullPath }, CancellationToken);
				}
				catch (YandexDisk.Client.YandexApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
				{
					await Task.Delay(500, CancellationToken);
					continue;
				}

				if (!info.PublicUrl.IsEmpty())
				{
					published = true;
					break;
				}

				await Task.Delay(500, CancellationToken);
			}

			if (!published)
				Assert.Fail("Published resource did not get PublicUrl.");

			await svc.UnPublishAsync(entry, CancellationToken);

			var unpublished = false;

			for (var i = 0; i < 120; i++)
			{
				YandexDisk.Client.Protocol.Resource info;

				try
				{
					info = await api.MetaInfo.GetInfoAsync(new() { Path = fullPath }, CancellationToken);
				}
				catch (YandexDisk.Client.YandexApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
				{
					await Task.Delay(500, CancellationToken);
					continue;
				}

				if (info.PublicUrl.IsEmpty())
				{
					unpublished = true;
					break;
				}

				await Task.Delay(500, CancellationToken);
			}

			if (!unpublished)
				Assert.Fail("Unpublished resource still has PublicUrl.");
		}
		finally
		{
			try
			{
				await svc.UnPublishAsync(entry, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await svc.DeleteAsync(entry, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await svc.DeleteAsync(folder, CancellationToken);
			}
			catch
			{
			}
		}
	}
}
