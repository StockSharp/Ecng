namespace Ecng.Tests.Backup;

using System.IO;
using System.Net;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

using Ecng.Common;

[TestClass]
[TestCategory("Integration")]
public class AwsS3ClientTests : BaseTestClass
{
	private sealed class Secrets
	{
		public AwsSecrets Aws { get; init; }
	}

	private sealed class AwsSecrets
	{
		public string Region { get; init; }
		public string Bucket { get; init; }
		public string AccessKey { get; init; }
		public string SecretKey { get; init; }
	}

	private static AwsSecrets LoadAwsSecrets()
	{
		static string Env(string name) => Environment.GetEnvironmentVariable(name);

		var fromEnv = new AwsSecrets
		{
			Region = Env("BACKUP_AWS_REGION"),
			Bucket = Env("BACKUP_AWS_BUCKET"),
			AccessKey = Env("BACKUP_AWS_ACCESS_KEY"),
			SecretKey = Env("BACKUP_AWS_SECRET_KEY"),
		};

		var fromFile = TryLoadSecretsFile()?.Aws;

		var s = new AwsSecrets
		{
			Region = fromEnv.Region ?? fromFile?.Region,
			Bucket = fromEnv.Bucket ?? fromFile?.Bucket,
			AccessKey = fromEnv.AccessKey ?? fromFile?.AccessKey,
			SecretKey = fromEnv.SecretKey ?? fromFile?.SecretKey,
		};

		if (s.Region.IsEmpty() || s.Bucket.IsEmpty() || s.AccessKey.IsEmpty() || s.SecretKey.IsEmpty())
			Assert.Inconclusive("AWS secrets missing. Set BACKUP_AWS_REGION, BACKUP_AWS_BUCKET, BACKUP_AWS_ACCESS_KEY, BACKUP_AWS_SECRET_KEY or provide secrets.json.");

		return s;
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
	public async Task Roundtrip_Put_Get_Delete()
	{
		var s = LoadAwsSecrets();

		using var client = new AmazonS3Client(new BasicAWSCredentials(s.AccessKey, s.SecretKey), RegionEndpoint.GetBySystemName(s.Region));

		var key = $"ecng-aws-native-tests/{Guid.NewGuid():N}.bin";
		var data = RandomGen.GetBytes(4096);

		try
		{
			using (var upload = new MemoryStream(data, writable: false))
			{
				var put = await client.PutObjectAsync(new PutObjectRequest
				{
					BucketName = s.Bucket,
					Key = key,
					InputStream = upload,
				}, CancellationToken);

				put.HttpStatusCode.AssertEqual(HttpStatusCode.OK);
			}

			var meta = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
			{
				BucketName = s.Bucket,
				Key = key,
			}, CancellationToken);

			meta.ContentLength.AssertEqual(data.Length);

			using var obj = await client.GetObjectAsync(new GetObjectRequest { BucketName = s.Bucket, Key = key }, CancellationToken);
			using var ms = new MemoryStream();
			await obj.ResponseStream.CopyToAsync(ms, CancellationToken);
			ms.ToArray().AssertEqual(data);
		}
		finally
		{
			try
			{
				await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = s.Bucket, Key = key }, CancellationToken);
			}
			catch
			{
			}
		}
	}

	[TestMethod]
	public async Task Publish_PresignedUrl_Download()
	{
		var s = LoadAwsSecrets();

		using var client = new AmazonS3Client(new BasicAWSCredentials(s.AccessKey, s.SecretKey), RegionEndpoint.GetBySystemName(s.Region));

		var key = $"ecng-aws-native-publish-tests/{Guid.NewGuid():N}.txt";
		var data = "hello " + Guid.NewGuid();

		try
		{
			using (var upload = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(data), writable: false))
			{
				await client.PutObjectAsync(new PutObjectRequest
				{
					BucketName = s.Bucket,
					Key = key,
					InputStream = upload,
					ContentType = "text/plain",
				}, CancellationToken);
			}

			var url = client.GetPreSignedURL(new GetPreSignedUrlRequest
			{
				BucketName = s.Bucket,
				Key = key,
				Verb = HttpVerb.GET,
				Expires = DateTime.UtcNow.AddMinutes(5),
			});

			url.IsEmpty().AssertFalse();

			using var http = new System.Net.Http.HttpClient();
			var downloaded = await http.GetByteArrayAsync(url, CancellationToken);
			System.Text.Encoding.UTF8.GetString(downloaded).AssertEqual(data);
		}
		finally
		{
			try
			{
				await client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = s.Bucket, Key = key }, CancellationToken);
			}
			catch
			{
			}
		}
	}
}
