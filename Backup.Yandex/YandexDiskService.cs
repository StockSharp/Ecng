namespace Ecng.Backup.Yandex;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using YandexDisk.Client;
using YandexDisk.Client.Clients;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

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
		if (token.IsEmpty())
			throw new ArgumentNullException(nameof(token));

		_client = new DiskHttpApi(token.UnSecure());
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_client.Dispose();
		base.DisposeManaged();
	}

	bool IBackupService.CanFolders => true;
	bool IBackupService.CanPublish => true;
	bool IBackupService.CanExpirable => false;
	bool IBackupService.CanPartialDownload => false;

	async IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
	{
		var path = parent is null ? "/" : parent.GetFullPath().TrimEnd('/') + "/";

		var offset = 0;
		var limit = 100;

		while (true)
		{
			Resource info;

			try
			{
				info = await _client.MetaInfo.GetInfoAsync(new()
				{
					Path = path,
					Offset = offset,
					Limit = limit,
				}, cancellationToken).NoWait();
			}
			catch (YandexApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
			{
				yield break;
			}

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
			Path = entry.GetFullPath(),
		}, cancellationToken).NoWait();

		entry.Size = info.Size;
		entry.LastModified = info.Modified;
	}

	Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		return _client.Commands.DeleteAsync(new()
		{
			Path = entry.GetFullPath(),
		}, cancellationToken);
	}

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		if (offset is not null || length is not null)
			throw new NotSupportedException();

		var file = await _client.Files.DownloadFileAsync(entry.GetFullPath(), cancellationToken).NoWait();
		await file.CopyToAsync(stream, cancellationToken).NoWait();
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		var link = await _client.Files.GetUploadLinkAsync(entry.GetFullPath(), true, cancellationToken).NoWait();
		await _client.Files.UploadAsync(link, stream, cancellationToken).NoWait();
	}

	async Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		var folders = new List<BackupEntry>();

		do
		{
			folders.Add(entry);
			entry = entry.Parent;
		}
		while (entry is not null);

		folders.Reverse();

		var needCheck = true;

		foreach (var folder in folders)
		{
			var path = folder.GetFullPath();

			if (!needCheck)
			{
				await _client.Commands.CreateDictionaryAsync(path, cancellationToken).NoWait();
				continue;
			}

			try
			{
				await _client.MetaInfo.GetInfoAsync(new() { Path = path }, cancellationToken).NoWait();
			}
			catch (YandexApiException ex)
			{
				if (ex.StatusCode == HttpStatusCode.NotFound)
				{
					await _client.Commands.CreateDictionaryAsync(path, cancellationToken).NoWait();
					needCheck = false;
				}
				else
					throw;
			}
		}
	}

	async Task<string> IBackupService.PublishAsync(BackupEntry entry, TimeSpan? expiresIn, CancellationToken cancellationToken)
	{
		if (expiresIn is not null)
			throw new NotSupportedException("Expiring links are not supported by Yandex.Disk publish API.");

		var link = await _client.MetaInfo.PublishFolderAsync(entry.GetFullPath(), cancellationToken).NoWait();
		return link.Href;
	}

	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> _client.MetaInfo.UnpublishFolderAsync(entry.GetFullPath(), cancellationToken);
}