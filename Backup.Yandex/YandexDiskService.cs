namespace Ecng.Backup.Yandex;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;

/// <summary>
/// The class for work with the Yandex.Disk.
/// </summary>
public class YandexDiskService : Disposable, IBackupService
{
	private readonly IDiskApi _client;

	/// <summary>
	/// Initializes a new instance of the <see cref="YandexDiskService"/>.
	/// </summary>
	public YandexDiskService(SecureString token)
	{
		_client = new DiskHttpApi(token.UnSecure());
	}

	bool IBackupService.CanFolders => true;
	bool IBackupService.CanPublish => true;
	bool IBackupService.CanPartialDownload => false;

	private static string GetPath(BackupEntry entry)
	{
		if (entry is null)
			return string.Empty;

		return GetPath(entry.Parent) + "/" + entry.Name;
	}

	async IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var path = GetPath(parent) + "/";

		var offset = 0;
		var limit = 100;

		while (true)
		{
			var info = await _client.MetaInfo.GetInfoAsync(new()
			{
				Path = path,
				Offset = offset,
				Limit = limit,
			}, cancellationToken);

			foreach (var item in info.Embedded.Items)
			{
				if (!criteria.IsEmpty() && !item.Name.ContainsIgnoreCase(criteria))
					continue;

				yield return new()
				{
					Parent = parent,
					Name = item.Name,
					Size = item.Size,
					LastModified = item.Modified,
				};
			}

			if (info.Embedded.Items.Count < limit)
				break;

			offset += limit;
		}
	}

	async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var info = await _client.MetaInfo.GetInfoAsync(new()
		{
			Path = GetPath(entry),
		}, cancellationToken);

		entry.Size = info.Size;
		entry.LastModified = info.Modified;
	}

	Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		return _client.Commands.DeleteAsync(new()
		{
			Path = GetPath(entry),
		}, cancellationToken);
	}

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		if (offset is not null || length is not null)
			throw new NotSupportedException();

		var file = await _client.Files.DownloadFileAsync(GetPath(entry), cancellationToken);
		await file.CopyToAsync(stream, cancellationToken);
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		var link = await _client.Files.GetUploadLinkAsync(GetPath(entry), true, cancellationToken);
		await _client.Files.UploadAsync(link, stream, cancellationToken);
	}

	async Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var link = await _client.MetaInfo.PublishFolderAsync(GetPath(entry), cancellationToken);
		return link.Href;
	}

	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> _client.MetaInfo.UnpublishFolderAsync(GetPath(entry), cancellationToken);
}