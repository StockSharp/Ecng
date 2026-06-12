namespace Ecng.Tests.Backup;

using Ecng.Backup;
using Ecng.Backup.Azure;

/// <summary>
/// Integration tests for <see cref="AzureBlobService"/>.
/// All tests here exercise a live Azure Blob Storage account and are gated by
/// the BACKUP_AZURE_* secrets (see <see cref="BackupServicesTests"/>); without
/// credentials they are marked inconclusive rather than failing.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class AzureBlobServiceTests : BaseTestClass
{
	private static AzureBlobService CreateService()
	{
		if (IsLocalHost)
			Inconclusive("Azure tests are skipped on localhost.");

		var connectionString = GetSecret("BACKUP_AZURE_CONNECTION_STRING");
		var container = GetSecret("BACKUP_AZURE_CONTAINER");

		return new AzureBlobService(connectionString, container);
	}

	private static async Task UploadAsync(IBackupService service, BackupEntry entry, byte[] data, CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(data, writable: false);
		await service.UploadAsync(entry, stream, _ => { }, cancellationToken);
	}

	private static async Task<List<BackupEntry>> FindAllAsync(IBackupService service, BackupEntry parent, string criteria, CancellationToken cancellationToken)
	{
		var result = new List<BackupEntry>();
		await foreach (var f in service.FindAsync(parent, criteria).WithCancellation(cancellationToken))
			result.Add(f);
		return result;
	}

	/// <summary>
	/// Regression test for FindAsync prefix matching: ensures listing a parent returns only blobs
	/// actually inside that virtual folder and not sibling folders sharing the name prefix
	/// (e.g. "data" vs "data-old"). (Was: the blob name prefix lacked a trailing '/' folder
	/// boundary, Backup.Azure\AzureBlobService.cs:58.)
	/// </summary>
	[TestMethod]
	public async Task FindAsync_ParentPrefix_DoesNotLeakSiblingFolders()
	{
		using var svc = CreateService();
		IBackupService service = svc;

		var stamp = Guid.NewGuid().ToString("N");
		var insideFolder = new BackupEntry { Name = $"data-{stamp}" };
		var siblingFolder = new BackupEntry { Name = $"data-{stamp}-old" };

		var inside = new BackupEntry { Name = "inside.bin", Parent = insideFolder };
		var sibling = new BackupEntry { Name = "sibling.bin", Parent = siblingFolder };

		var data = RandomGen.GetBytes(64);

		try
		{
			await UploadAsync(service, inside, data, CancellationToken);
			await UploadAsync(service, sibling, data, CancellationToken);

			var found = await FindAllAsync(service, insideFolder, criteria: null, cancellationToken: CancellationToken);

			var foundPaths = found.Select(f => f.GetFullPath()).ToArray();

			// The blob that is genuinely inside the requested parent must be present.
			foundPaths.Any(p => p.EqualsIgnoreCase(inside.GetFullPath()))
				.AssertTrue($"Inside blob not found. Found: {foundPaths.Join(", ")}");

			// The sibling-folder blob shares the name prefix but is NOT inside the requested parent.
			foundPaths.Any(p => p.EqualsIgnoreCase(sibling.GetFullPath()))
				.AssertFalse($"Sibling-folder blob leaked into parent listing. Found: {foundPaths.Join(", ")}");
		}
		finally
		{
			await TryDeleteAsync(service, inside);
			await TryDeleteAsync(service, sibling);
		}
	}

	/// <summary>
	/// Regression test for FindAsync criteria filtering: ensures the criteria matches the leaf
	/// file name only, so a file whose leaf name lacks the criteria is excluded even when a parent
	/// folder segment contains it. (Was: criteria matched against the full blob key including
	/// folder segments, Backup.Azure\AzureBlobService.cs:62.)
	/// </summary>
	[TestMethod]
	public async Task FindAsync_Criteria_MatchesLeafNameNotFolderSegment()
	{
		using var svc = CreateService();
		IBackupService service = svc;

		var stamp = Guid.NewGuid().ToString("N");

		// Folder segment contains the criteria token "report", the leaf file name does not.
		var folder = new BackupEntry { Name = $"report-{stamp}" };
		var entry = new BackupEntry { Name = "data.bin", Parent = folder };

		var data = RandomGen.GetBytes(64);

		try
		{
			await UploadAsync(service, entry, data, CancellationToken);

			var found = await FindAllAsync(service, folder, criteria: "report", cancellationToken: CancellationToken);

			var foundPaths = found.Select(f => f.GetFullPath()).ToArray();

			// "report" appears only in the folder segment, not in the leaf name "data.bin",
			// so the leaf-name filter must exclude this entry.
			foundPaths.Any(p => p.EqualsIgnoreCase(entry.GetFullPath()))
				.AssertFalse($"Entry matched criteria via folder segment instead of leaf name. Found: {foundPaths.Join(", ")}");
		}
		finally
		{
			await TryDeleteAsync(service, entry);
		}
	}

	private async Task TryDeleteAsync(IBackupService service, BackupEntry entry)
	{
		try
		{
			await service.DeleteAsync(entry, CancellationToken);
		}
		catch
		{
		}
	}
}
