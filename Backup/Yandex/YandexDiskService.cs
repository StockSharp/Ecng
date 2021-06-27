namespace Ecng.Backup.Yandex
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using YandexDisk.Client.Http;
	using YandexDisk.Client.Protocol;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The class for work with the Yandex.Disk.
	/// </summary>
	public class YandexDiskService : Disposable, IBackupService
	{
		private readonly Func<CancellationToken, Task<(string token, bool result)>> _authorize;
		private DiskHttpApi _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="YandexDiskService"/>.
		/// </summary>
		public YandexDiskService(Func<CancellationToken, Task<(string token, bool result)>> authFunc)
		{
			_authorize = authFunc ?? throw new ArgumentNullException(nameof(authFunc));
		}

		private async Task<DiskHttpApi> GetClientAsync(CancellationToken cancellationToken)
		{
			//cancelled = false;

			if (_client != null)
				return _client;

			var (token, result) = await _authorize(cancellationToken);

			if (!result)
			{
				_client = new DiskHttpApi(token);
				return _client;
			}

			return default;
		}

		private static string GetPath(BackupEntry entry)
		{
			if (entry == null)
				return string.Empty;

			return GetPath(entry.Parent) + "/" + entry.Name;
		}

		async Task<IEnumerable<BackupEntry>> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		{
			var entries = await ((IBackupService)this).GetChildsAsync(parent, cancellationToken);

			if (!criteria.IsEmpty())
				entries = entries.Where(e => e.Name.ContainsIgnoreCase(criteria)).ToArray();
			
			return entries;
		}

		async Task<IEnumerable<BackupEntry>> IBackupService.GetChildsAsync(BackupEntry parent, CancellationToken cancellationToken)
		{
			//if (parent == null)
			//	throw new ArgumentNullException(nameof(parent));

			var path = GetPath(parent) + "/";

			var client = await GetClientAsync(cancellationToken);

			var resource = await client.MetaInfo.GetInfoAsync(new ResourceRequest { Path = path }, cancellationToken);

			return resource.Embedded.Items.Select(i => new BackupEntry
			{
				Parent = parent,
				Name = i.Name,
				Size = i.Size
			}).ToArray();
		}

		async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);

			var resource = await client.MetaInfo.GetInfoAsync(new ResourceRequest { Path = path }, cancellationToken);

			entry.Size = resource.Size;
		}

		async Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);

			await client.Commands.DeleteAsync(new DeleteFileRequest
			{
				Path = path,
			}, cancellationToken);
		}

		async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			if (offset != null || length != null)
				throw new NotSupportedException();

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);

			var link = await client.Files.GetDownloadLinkAsync(path, cancellationToken);

			using var responseStream = await client.Files.DownloadAsync(link, cancellationToken);
			await responseStream.CopyAsync(stream, offset ?? default, length, progress, cancellationToken);
		}

		async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);

			var link = await client.Files.GetUploadLinkAsync(path, true, cancellationToken);

			await client.Files.UploadAsync(link, stream, cancellationToken);
		}

		bool IBackupService.CanPublish => true;

		async Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);
			var link = await client.MetaInfo.PublishFolderAsync(path, cancellationToken);

			return link.Href;
		}

		async Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			var client = await GetClientAsync(cancellationToken);
			await client.MetaInfo.UnpublishFolderAsync(path, cancellationToken);
		}
	}
}