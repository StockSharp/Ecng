namespace Ecng.Tests.Backup;

using Ecng.Backup;
using Ecng.Backup.Amazon;

/// <summary>
/// Integration tests for <see cref="AmazonS3Service"/>.
///
/// <see cref="AmazonS3Service"/> exposes no constructor accepting an
/// <see cref="Amazon.S3.IAmazonS3"/> (the client is private and there is no
/// InternalsVisibleTo), so the defective code can only be reached through a real
/// bucket. These tests are gated by <see cref="BaseTestClass.GetSecret"/> exactly
/// like <c>BackupServicesTests</c>.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class AmazonS3ServiceTests : BaseTestClass
{
	/// <summary>
	/// BUG: S3 UploadAsync streams via multipart and calls CompleteMultipartUpload with
	/// zero parts for an empty stream, which S3 rejects. (AmazonS3Service.cs:200)
	/// Expected: uploading an empty stream succeeds and the stored object has size 0.
	/// Actual: the upload throws (MalformedXML / "must specify at least one part").
	///
	/// Integration: AmazonS3Service has no IAmazonS3 injection point, so this is exercised
	/// against a real bucket gated by BACKUP_AWS_* secrets.
	/// </summary>
	[TestMethod]
	public async Task UploadAsync_EmptyStream_ShouldSucceed()
	{
		var region = GetSecret("BACKUP_AWS_REGION");
		var bucket = GetSecret("BACKUP_AWS_BUCKET");
		var accessKey = GetSecret("BACKUP_AWS_ACCESS_KEY");
		var secretKey = GetSecret("BACKUP_AWS_SECRET_KEY");

		using IBackupService svc = new AmazonS3Service(region, bucket, accessKey, secretKey);

		var entry = new BackupEntry { Name = $"empty-{Guid.NewGuid():N}.bin" };

		try
		{
			using (var uploadStream = new MemoryStream([], writable: false))
				await svc.UploadAsync(entry, uploadStream, _ => { }, CancellationToken);

			await svc.FillInfoAsync(entry, CancellationToken);
			AreEqual(0L, entry.Size);
		}
		finally
		{
			try
			{
				await svc.DeleteAsync(entry, CancellationToken);
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// BUG: S3 FindAsync builds the listing prefix from parent.GetFullPath() without a
	/// trailing '/', so listing parent "data" also returns objects under sibling
	/// "data2/" etc. (AmazonS3Service.cs:68)
	/// Expected: FindAsync(parent, null) returns only direct descendants of parent.
	/// Actual: entries from sibling "folders" sharing the prefix leak into the result.
	///
	/// Integration: gated by BACKUP_AWS_* secrets.
	/// </summary>
	[TestMethod]
	public async Task FindAsync_ParentPrefix_ShouldNotLeakSiblingFolders()
	{
		var region = GetSecret("BACKUP_AWS_REGION");
		var bucket = GetSecret("BACKUP_AWS_BUCKET");
		var accessKey = GetSecret("BACKUP_AWS_ACCESS_KEY");
		var secretKey = GetSecret("BACKUP_AWS_SECRET_KEY");

		using IBackupService svc = new AmazonS3Service(region, bucket, accessKey, secretKey);

		var stamp = Guid.NewGuid().ToString("N");
		var folderName = "data-" + stamp;
		var siblingName = folderName + "2"; // shares the prefix "data-<stamp>"

		var child = new BackupEntry { Name = "child.bin", Parent = new BackupEntry { Name = folderName } };
		var sibling = new BackupEntry { Name = "child.bin", Parent = new BackupEntry { Name = siblingName } };

		try
		{
			using (var s = new MemoryStream(RandomGen.GetBytes(64), writable: false))
				await svc.UploadAsync(child, s, _ => { }, CancellationToken);

			using (var s = new MemoryStream(RandomGen.GetBytes(64), writable: false))
				await svc.UploadAsync(sibling, s, _ => { }, CancellationToken);

			var found = new List<string>();
			await foreach (var f in svc.FindAsync(new BackupEntry { Name = folderName }, null).WithCancellation(CancellationToken))
				found.Add(f.GetFullPath());

			IsTrue(found.Contains(child.GetFullPath()), "Direct child should be listed.");
			IsFalse(found.Contains(sibling.GetFullPath()),
				$"Sibling folder entry leaked into parent listing: {sibling.GetFullPath()}.");
		}
		finally
		{
			try { await svc.DeleteAsync(child, CancellationToken); } catch { }
			try { await svc.DeleteAsync(sibling, CancellationToken); } catch { }
		}
	}
}
