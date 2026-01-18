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
		: this(new DiskHttpApi(token.IsEmpty() ? throw new ArgumentNullException(nameof(token)) : token.UnSecure()))
	{
	}

	/// <summary>
	/// Initializes a new instance with a custom <see cref="IDiskApi"/> implementation.
	/// </summary>
	/// <param name="client">The Yandex.Disk client.</param>
	public YandexDiskService(IDiskApi client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
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

	IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria)
		=> FindAsyncImpl(parent, criteria);

	private async IAsyncEnumerable<BackupEntry> FindAsyncImpl(BackupEntry parent, string criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

		if (progress is null || !file.CanSeek)
		{
			await file.CopyToAsync(stream, cancellationToken).NoWait();
			progress?.Invoke(100);
		}
		else
		{
			var totalBytes = file.Length;
			var buffer = new byte[81920];
			long totalRead = 0;
			int lastPercent = 0;
			int bytesRead;

			while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length, cancellationToken).NoWait()) > 0)
			{
				await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).NoWait();
				totalRead += bytesRead;

				var percent = totalBytes > 0 ? (int)(totalRead * 100 / totalBytes) : 0;
				if (percent != lastPercent)
				{
					progress(percent);
					lastPercent = percent;
				}
			}

			if (lastPercent != 100)
				progress(100);
		}
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		var link = await _client.Files.GetUploadLinkAsync(entry.GetFullPath(), true, cancellationToken).NoWait();

		if (progress is null || !stream.CanSeek)
		{
			await _client.Files.UploadAsync(link, stream, cancellationToken).NoWait();
			progress?.Invoke(100);
		}
		else
		{
			var totalBytes = stream.Length;
			var wrapper = new ProgressReportingStream(stream, totalBytes, progress);
			await _client.Files.UploadAsync(link, wrapper, cancellationToken).NoWait();

			if (wrapper.LastReportedPercent != 100)
				progress(100);
		}
	}

	private sealed class ProgressReportingStream(Stream inner, long totalBytes, Action<int> progress) : Stream
	{
		private long _bytesRead;
		public int LastReportedPercent { get; private set; }

		public override int Read(byte[] buffer, int offset, int count)
		{
			var bytesRead = inner.Read(buffer, offset, count);
			ReportProgress(bytesRead);
			return bytesRead;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var bytesRead = await inner.ReadAsync(buffer, offset, count, cancellationToken);
			ReportProgress(bytesRead);
			return bytesRead;
		}

		private void ReportProgress(int bytesRead)
		{
			if (bytesRead <= 0) return;

			_bytesRead += bytesRead;
			var percent = totalBytes > 0 ? (int)(_bytesRead * 100 / totalBytes) : 0;
			if (percent != LastReportedPercent)
			{
				progress(percent);
				LastReportedPercent = percent;
			}
		}

		public override bool CanRead => inner.CanRead;
		public override bool CanSeek => inner.CanSeek;
		public override bool CanWrite => inner.CanWrite;
		public override long Length => inner.Length;
		public override long Position { get => inner.Position; set => inner.Position = value; }
		public override void Flush() => inner.Flush();
		public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
		public override void SetLength(long value) => inner.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
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
				await _client.Commands.CreateDirectoryAsync(path, cancellationToken).NoWait();
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
					await _client.Commands.CreateDirectoryAsync(path, cancellationToken).NoWait();
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