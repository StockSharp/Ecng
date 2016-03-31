namespace Ecng.Backup.Yandex
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Windows;

	using Disk.SDK;
	using Disk.SDK.Provider;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	/// <summary>
	/// The class for work with the Yandex.Disk.
	/// </summary>
	public class YandexDiskService : Disposable, IBackupService
	{
		private DiskSdkClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="YandexDiskService"/>.
		/// </summary>
		public YandexDiskService()
		{
		}

		private YandexLoginWindow CreateWindow(out Exception error)
		{
			Exception error1 = null;
			var loginWindow = new YandexLoginWindow();

			loginWindow.AuthCompleted += (s, e) =>
			{
				if (e.Error == null)
					_client = new DiskSdkClient(e.Result);
				else
					error1 = e.Error;
			};

			error = error1;
			return loginWindow;
		}

		void IDelayInitService.Init()
		{
			Exception error = null;

			var owner = Scope<Window>.Current?.Value ?? Application.Current?.MainWindow;

			var retVal = owner?.GuiSync(() => CreateWindow(out error).ShowModal(owner)) ?? CreateWindow(out error).ShowDialog() == true;

			if (!retVal)
				throw new UnauthorizedAccessException();

			error?.Throw();
		}

		private static string GetPath(BackupEntry entry)
		{
			if (entry == null)
				return string.Empty;

			return GetPath(entry.Parent) + "/" + entry.Name;
		}

		IEnumerable<BackupEntry> IBackupService.Get(BackupEntry parent)
		{
			//if (parent == null)
			//	throw new ArgumentNullException(nameof(parent));

			var path = GetPath(parent) + "/";

			return _client.AsyncWait<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>, IEnumerable<BackupEntry>>(
				nameof(DiskSdkClient.GetListCompleted),
				() => _client.GetListAsync(path),
				e => e.Result.Select(i => new BackupEntry
				{
					Parent = parent,
					Name = i.DisplayName,
					Size = i.ContentLength
				}).ToArray());
		}

		void IBackupService.Delete(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			_client.AsyncWait<SdkEventArgs, object>(
				nameof(DiskSdkClient.RemoveCompleted),
				() => _client.RemoveAsync(path),
				e => e);
		}

		CancellationTokenSource IBackupService.Download(BackupEntry entry, Stream stream, Action<int> progress)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var source = new CancellationTokenSource();
			var path = GetPath(entry);

			var sync = new SyncObject();
			var pulsed = false;

			_client.DownloadFileAsync(path, stream, new AsyncProgress((curr, total) => progress((int)(curr * 100 / total))), (s, e) =>
			{
				lock (sync)
				{
					pulsed = true;
					sync.Pulse();
				}
			});

			lock (sync)
			{
				if (!pulsed)
					sync.Wait();
			}

			(stream as MemoryStream)?.UndoDispose();

			return source;
		}

		CancellationTokenSource IBackupService.Upload(BackupEntry entry, Stream stream, Action<int> progress)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var source = new CancellationTokenSource();
			var path = GetPath(entry);

			var sync = new SyncObject();
			var pulsed = false;

			_client.UploadFileAsync(path, stream, new AsyncProgress((curr, total) => progress((int)(curr * 100 / total))), (s, e) =>
			{
				lock (sync)
				{
					pulsed = true;
					sync.Pulse();
				}
			});

			lock (sync)
			{
				if (!pulsed)
					sync.Wait();
			}

			(stream as MemoryStream)?.UndoDispose();

			return source;
		}

		string IBackupService.Publish(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			return _client.AsyncWait<GenericSdkEventArgs<string>, string>(
				nameof(DiskSdkClient.PublishCompleted),
				() => _client.PublishAsync(path),
				e => e.Result);
		}

		void IBackupService.UnPublish(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			_client.AsyncWait<SdkEventArgs, object>(
				nameof(DiskSdkClient.UnpublishCompleted),
				() => _client.UnpublishAsync(path),
				e => e);
		}
	}
}