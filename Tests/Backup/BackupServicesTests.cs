namespace Ecng.Tests.Backup;

using System.IO;

using Ecng.Backup;
using Ecng.Backup.Amazon;
using Ecng.Backup.Azure;
using Ecng.Backup.Mega;
using Ecng.Backup.Yandex;

// TODO
//[TestClass]
public class BackupServicesTests : BaseTestClass
{
	private const string _secretsFileName = "secrets.json";

	private sealed class Secrets
	{
		public AwsSecrets Aws { get; init; }
		public AzureSecrets Azure { get; init; }
		public MegaSecrets Mega { get; init; }
		public YandexSecrets Yandex { get; init; }
	}

	private sealed class AwsSecrets
	{
		public string Region { get; init; }
		public string Bucket { get; init; }
		public string AccessKey { get; init; }
		public string SecretKey { get; init; }
	}

	private sealed class AzureSecrets
	{
		public string ConnectionString { get; init; }
		public string Container { get; init; }
	}

	private sealed class MegaSecrets
	{
		public string Email { get; init; }
		public string Password { get; init; }
	}

	private sealed class YandexSecrets
	{
		public string Token { get; init; }
	}

	private static Secrets LoadSecrets()
	{
        static string Env(string name) => Environment.GetEnvironmentVariable(name);

		var fromEnv = new Secrets
		{
			Aws = new AwsSecrets
			{
				Region = Env("BACKUP_AWS_REGION"),
				Bucket = Env("BACKUP_AWS_BUCKET"),
				AccessKey = Env("BACKUP_AWS_ACCESS_KEY"),
				SecretKey = Env("BACKUP_AWS_SECRET_KEY"),
			},
			Azure = new AzureSecrets
			{
				ConnectionString = Env("BACKUP_AZURE_CONNECTION_STRING"),
				Container = Env("BACKUP_AZURE_CONTAINER"),
			},
			Mega = new MegaSecrets
			{
				Email = Env("BACKUP_MEGA_EMAIL"),
				Password = Env("BACKUP_MEGA_PASSWORD"),
			},
			Yandex = new YandexSecrets
			{
				Token = Env("BACKUP_YANDEX_TOKEN"),
			},
		};

		var fileSecrets = TryLoadSecretsFile();
		if (fileSecrets is null)
			return fromEnv;

		// env overrides file
		return new Secrets
		{
			Aws = new AwsSecrets
			{
				Region = fromEnv.Aws.Region ?? fileSecrets.Aws?.Region,
				Bucket = fromEnv.Aws.Bucket ?? fileSecrets.Aws?.Bucket,
				AccessKey = fromEnv.Aws.AccessKey ?? fileSecrets.Aws?.AccessKey,
				SecretKey = fromEnv.Aws.SecretKey ?? fileSecrets.Aws?.SecretKey,
			},
			Azure = new AzureSecrets
			{
				ConnectionString = fromEnv.Azure.ConnectionString ?? fileSecrets.Azure?.ConnectionString,
				Container = fromEnv.Azure.Container ?? fileSecrets.Azure?.Container,
			},
			Mega = new MegaSecrets
			{
				Email = fromEnv.Mega.Email ?? fileSecrets.Mega?.Email,
				Password = fromEnv.Mega.Password ?? fileSecrets.Mega?.Password,
			},
			Yandex = new YandexSecrets
			{
				Token = fromEnv.Yandex.Token ?? fileSecrets.Yandex?.Token,
			},
		};
	}

	private static Secrets TryLoadSecretsFile()
	{
		var explicitPath = Environment.GetEnvironmentVariable("BACKUP_SECRETS_FILE");
		if (!explicitPath.IsEmpty() && File.Exists(explicitPath))
			return explicitPath.DeserializeSecrets<Secrets>();

		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		for (var i = 0; i < 8 && dir != null; i++)
		{
			var candidate = Path.Combine(dir.FullName, _secretsFileName);
			if (File.Exists(candidate))
				return candidate.DeserializeSecrets<Secrets>();
			dir = dir.Parent;
		}

		return null;
	}

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

		var data = new byte[4096];
		new Random().NextBytes(data);

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
		var s = LoadSecrets().Aws;
		if (s?.Region.IsEmpty() != false || s.Bucket.IsEmpty() || s.AccessKey.IsEmpty() || s.SecretKey.IsEmpty())
			Assert.Inconclusive("AWS secrets missing. Set BACKUP_AWS_REGION, BACKUP_AWS_BUCKET, BACKUP_AWS_ACCESS_KEY, BACKUP_AWS_SECRET_KEY or provide secrets.json.");

		using var svc = new AmazonS3Service(s.Region, s.Bucket, s.AccessKey, s.SecretKey);
		await RoundtripAsync(svc, "AWS S3", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task AzureBlob_Roundtrip()
	{
		var s = LoadSecrets().Azure;
		if (s?.ConnectionString.IsEmpty() != false || s.Container.IsEmpty())
			Assert.Inconclusive("Azure secrets missing. Set BACKUP_AZURE_CONNECTION_STRING and BACKUP_AZURE_CONTAINER or provide secrets.json.");

		using var svc = new AzureBlobService(s.ConnectionString, s.Container);
		await RoundtripAsync(svc, "Azure Blob", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task Mega_Roundtrip()
	{
		var s = LoadSecrets().Mega;
		if (s?.Email.IsEmpty() != false || s.Password.IsEmpty())
			Assert.Inconclusive("Mega secrets missing. Set BACKUP_MEGA_EMAIL and BACKUP_MEGA_PASSWORD or provide secrets.json.");

		using var svc = new MegaService(s.Email, s.Password.Secure());
		await RoundtripAsync(svc, "Mega", cancellationToken: CancellationToken);
	}

	[TestMethod]
	public async Task YandexDisk_Roundtrip()
	{
		var s = LoadSecrets().Yandex;
		if (s?.Token.IsEmpty() != false)
			Assert.Inconclusive("Yandex secrets missing. Set BACKUP_YANDEX_TOKEN or provide secrets.json.");

		using var svc = new YandexDiskService(s.Token.Secure());
		await RoundtripAsync(svc, "Yandex Disk", cancellationToken: CancellationToken);
	}
}

