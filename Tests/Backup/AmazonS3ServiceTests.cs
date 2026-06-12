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
	/// Regression test for S3 upload of empty streams: ensures uploading an empty stream
	/// succeeds and the stored object has size 0. (Was: multipart upload called
	/// CompleteMultipartUpload with zero parts, which S3 rejects; AmazonS3Service.cs:191.)
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
	/// Regression test for S3 FindAsync prefixing: ensures FindAsync(parent, null) returns
	/// only direct descendants of parent and does not leak objects from sibling "folders"
	/// sharing the prefix. (Was: the listing prefix was built from parent.GetFullPath()
	/// without a trailing '/', so listing parent "data" also returned "data2/" objects;
	/// AmazonS3Service.cs:67.)
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
