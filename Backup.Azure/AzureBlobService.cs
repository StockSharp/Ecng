namespace Ecng.Backup.Azure;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using global::Azure.Storage.Blobs;
using global::Azure.Storage.Blobs.Specialized;

using Ecng.Common;

/// <summary>
/// The data storage service based on Azure Blob Storage.
/// </summary>
public class AzureBlobService : Disposable, IBackupService
{
	private readonly BlobContainerClient _container;
	private const int _bufferSize = FileSizes.MB * 4;

	/// <summary>
	/// Initializes a new instance of the <see cref="AzureBlobService"/>.
	/// </summary>
	/// <param name="connectionString">Storage connection string.</param>
	/// <param name="container">Container name.</param>
	public AzureBlobService(string connectionString, string container)
	{
		if (connectionString.IsEmpty())
			throw new ArgumentNullException(nameof(connectionString));

		if (container.IsEmpty())
			throw new ArgumentNullException(nameof(container));

		_container = new(connectionString, container);
	}

	bool IBackupService.CanFolders => false;
	bool IBackupService.CanPublish => false;
	bool IBackupService.CanPartialDownload => true;

	Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
		=> Task.CompletedTask;

	async IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var prefix = parent?.GetFullPath();

		await foreach (var item in _container.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
		{
			if (!criteria.IsEmpty() && !item.Name.ContainsIgnoreCase(criteria))
				continue;

			var be = GetPath(item.Name);
			be.LastModified = item.Properties.LastModified?.UtcDateTime ?? default;
			be.Size = item.Properties.ContentLength ?? 0;
			yield return be;
		}
	}

	private static BackupEntry GetPath(string key)
	{
		BackupEntry entry = null;

		foreach (var part in key.Split('/', StringSplitOptions.RemoveEmptyEntries))
			entry = new() { Name = part, Parent = entry };

		return entry;
	}

	async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var blob = _container.GetBlobClient(entry.GetFullPath());
		var props = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);

		entry.LastModified = props.Value.LastModified.UtcDateTime;
		entry.Size = props.Value.ContentLength;
	}

	Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> _container.DeleteBlobAsync(entry.GetFullPath(), cancellationToken: cancellationToken);

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		var blob = _container.GetBlobClient(entry.GetFullPath());
		var response = await blob.DownloadAsync(new(offset ?? 0, length), cancellationToken: cancellationToken);
		var source = response.Value.Content;
		var total = response.Value.ContentLength;

		var buffer = new byte[_bufferSize];
		var read = 0L;
		var prevProgress = -1;

		while (read < total)
		{
			var expected = (int)Math.Min(total - read, _bufferSize);
			var actual = await source.ReadAsync(buffer.AsMemory(0, expected), cancellationToken);

			if (actual == 0)
				break;

			await stream.WriteAsync(buffer.AsMemory(0, actual), cancellationToken);
			read += actual;

			var curr = (int)(read * 100L / total);
			if (curr < 100 && curr > prevProgress)
			{
				progress(curr);
				prevProgress = curr;
			}
		}

		if (prevProgress < 100)
			progress(100);
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		var blob = _container.GetBlockBlobClient(entry.GetFullPath());
		var buffer = new byte[_bufferSize];
		var uploaded = 0L;
		var prevProgress = -1;

		stream.Position = 0;
		var blockIds = new List<string>();
		var blockNum = 0;

		while (uploaded < stream.Length)
		{
			var expected = (int)Math.Min(stream.Length - uploaded, _bufferSize);
			var actual = await stream.ReadAsync(buffer.AsMemory(0, expected), cancellationToken);
			if (actual == 0)
				break;

			var id = Convert.ToBase64String(BitConverter.GetBytes(blockNum));
			await blob.StageBlockAsync(id, new MemoryStream(buffer, 0, actual, writable: false), cancellationToken: cancellationToken);
			blockIds.Add(id);
			uploaded += actual;
			blockNum++;

			var curr = (int)(uploaded * 100L / stream.Length);
			if (curr > prevProgress)
			{
				progress(curr);
				prevProgress = curr;
			}
		}

		await blob.CommitBlockListAsync(blockIds, cancellationToken: cancellationToken);

		if (prevProgress < 100)
			progress(100);
	}

	Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken) => throw new NotSupportedException();
	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken) => throw new NotSupportedException();
}
