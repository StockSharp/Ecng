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
		await foreach (var f in service.FindAsync(folder, entry.Name).WithCancellation(cancellationToken))
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

			// Give Yandex API time to propagate the publish status
			await Task.Delay(2000, CancellationToken);

			var published = false;

			for (var i = 0; i < 180; i++)
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
				Fail("Published resource did not get PublicUrl.");

			await svc.UnPublishAsync(entry, CancellationToken);

			// Give Yandex API time to propagate the unpublish status
			await Task.Delay(2000, CancellationToken);

			var unpublished = false;

			for (var i = 0; i < 180; i++)
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
				Fail("Unpublished resource still has PublicUrl.");
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

	/// <summary>
	/// BUG: DeleteAsync calls Find() before EnsureLogin(), so _nodes is empty on fresh service.
	/// Expected: Should work (login first, then find).
	/// Actual: InvalidOperationException from GetRoot().First() because _nodes is empty.
	/// </summary>
	[TestMethod]
	public async Task Mega_DeleteAsync_OnFreshService_ShouldNotThrow()
	{
		var email = GetSecret("BACKUP_MEGA_EMAIL");
		var password = GetSecret("BACKUP_MEGA_PASSWORD");

		// First, create and upload a file using a working service
		var testEntry = new BackupEntry
		{
			Name = $"bug-test-{Guid.NewGuid():N}.bin",
		};

		using (IBackupService setupSvc = new MegaService(email, password.Secure()))
		{
			using var uploadStream = new MemoryStream([1, 2, 3]);
			await setupSvc.UploadAsync(testEntry, uploadStream, _ => { }, CancellationToken);
		}

		// Now create a FRESH service and immediately call DeleteAsync
		// BUG: This will fail because Find() is called before EnsureLogin()
		using IBackupService freshSvc = new MegaService(email, password.Secure());

		// This should work but will throw InvalidOperationException
		// because _nodes is empty (EnsureLogin not called yet)
		await freshSvc.DeleteAsync(testEntry, CancellationToken);
	}

	/// <summary>
	/// BUG: DownloadAsync calls Find() before EnsureLogin(), so _nodes is empty on fresh service.
	/// </summary>
	[TestMethod]
	public async Task Mega_DownloadAsync_OnFreshService_ShouldNotThrow()
	{
		var email = GetSecret("BACKUP_MEGA_EMAIL");
		var password = GetSecret("BACKUP_MEGA_PASSWORD");

		// First, create and upload a file using a working service
		var testEntry = new BackupEntry
		{
			Name = $"bug-test-download-{Guid.NewGuid():N}.bin",
		};
		var testData = new byte[] { 1, 2, 3, 4, 5 };

		using (IBackupService setupSvc = new MegaService(email, password.Secure()))
		{
			using var uploadStream = new MemoryStream(testData);
			await setupSvc.UploadAsync(testEntry, uploadStream, _ => { }, CancellationToken);
		}

		try
		{
			// Now create a FRESH service and immediately call DownloadAsync
			// BUG: This will fail because Find() is called before EnsureLogin()
			using IBackupService freshSvc = new MegaService(email, password.Secure());
			using var downloadStream = new MemoryStream();

			// This should work but will throw because _nodes is empty
			await freshSvc.DownloadAsync(testEntry, downloadStream, null, null, _ => { }, CancellationToken);
		}
		finally
		{
			// Cleanup
			using IBackupService cleanupSvc = new MegaService(email, password.Secure());
			await cleanupSvc.DeleteAsync(testEntry, CancellationToken);
		}
	}

	/// <summary>
	/// BUG: CreateFolder doesn't check entry for null.
	/// Expected: ArgumentNullException.
	/// Actual: NullReferenceException.
	/// </summary>
	[TestMethod]
	public async Task Mega_CreateFolder_NullEntry_ShouldThrowArgumentNullException()
	{
		var email = GetSecret("BACKUP_MEGA_EMAIL");
		var password = GetSecret("BACKUP_MEGA_PASSWORD");

		using IBackupService svc = new MegaService(email, password.Secure());

		// This should throw ArgumentNullException, but throws NullReferenceException
		await ThrowsAsync<ArgumentNullException>(() => svc.CreateFolder(null, CancellationToken));
	}
}
