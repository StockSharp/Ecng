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

		private void Process(Action<DiskSdkClient> handler, out bool cancelled)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			Process<object>(client =>
			{
				handler(client);
				return null;
			}, out cancelled);
		}

		private T Process<T>(Func<DiskSdkClient, T> handler, out bool cancelled)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			cancelled = false;

			if (_client != null)
			{
				return handler(_client);
			}

			var result = default(T);

			Exception error = null;

			var owner = Scope<Window>.Current?.Value ?? Application.Current?.MainWindow;

			YandexLoginWindow CreateWindow()
			{
				var loginWindow = new YandexLoginWindow();

				loginWindow.AuthCompleted += (s, e) =>
				{
					if (e.Error == null)
					{
						_client = new DiskSdkClient(e.Result);
						result = handler(_client);
					}
					else
						error = e.Error;
				};

				return loginWindow;
			}

			var retVal = owner?.GuiSync(() => CreateWindow().ShowModal(owner)) ?? CreateWindow().ShowDialog() == true;

			if (!retVal)
				cancelled = true;

			error?.Throw();

			return result;
		}

		private static string GetPath(BackupEntry entry)
		{
			if (entry == null)
				return string.Empty;

			return GetPath(entry.Parent) + "/" + entry.Name;
		}

		IEnumerable<BackupEntry> IBackupService.Find(BackupEntry parent, string criteria)
		{
			var entries = ((IBackupService)this).GetChilds(parent);

			if (!criteria.IsEmpty())
				entries = entries.Where(e => e.Name.ContainsIgnoreCase(criteria)).ToArray();
			
			return entries;
		}

		IEnumerable<BackupEntry> IBackupService.GetChilds(BackupEntry parent)
		{
			//if (parent == null)
			//	throw new ArgumentNullException(nameof(parent));

			var path = GetPath(parent) + "/";

			var retVal = Process(client =>
			{
				return client.AsyncWait<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>, IEnumerable<BackupEntry>>(
					nameof(DiskSdkClient.GetListCompleted),
					() => client.GetListAsync(path),
					e => e.Result.Select(i => new BackupEntry
					{
						Parent = parent,
						Name = i.DisplayName,
						Size = i.ContentLength
					}).ToArray());
			}, out var cancelled);

			if (cancelled)
				throw new UnauthorizedAccessException();

			return retVal;
		}

		void IBackupService.FillInfo(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			var info = Process(client =>
			{
				return client.AsyncWait<GenericSdkEventArgs<DiskItemInfo>, DiskItemInfo>(
					nameof(DiskSdkClient.GetItemInfoCompleted),
					() => client.GetItemInfoAsync(path),
					e => e.Result);
			}, out var cancelled);

			if (cancelled)
				throw new UnauthorizedAccessException();

			entry.Size = info.ContentLength;
		}

		void IBackupService.Delete(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			Process(client =>
			{
				client.AsyncWait<SdkEventArgs, object>(
					nameof(DiskSdkClient.RemoveCompleted),
					() => client.RemoveAsync(path),
					e => e);
			}, out _);
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

			Exception error = null;

			var sync = new SyncObject();
			var pulsed = false;

			Process(client =>
			{
				client.DownloadFileAsync(path, stream, new AsyncProgress((curr, total) => progress((int)(curr * 100 / total))), (s, e) =>
				{
					error = error ?? e.Error;

					lock (sync)
					{
						pulsed = true;
						sync.Pulse();
					}
				});
			}, out var cancelled);

			if (!cancelled)
			{
				lock (sync)
				{
					if (!pulsed)
						sync.Wait();
				}

				(stream as MemoryStream)?.UndoDispose();

				error?.Throw();
			}
			else
				source.Cancel();

			return source;
		}

		public CancellationTokenSource Download(BackupEntry entry, byte[] buffer, long start, int length, Action<int> progress)
		{
			throw new NotSupportedException();
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

			Exception error = null;

			var sync = new SyncObject();
			var pulsed = false;

			Process(client =>
			{
				client.UploadFileAsync(path, stream, new AsyncProgress((curr, total) => progress((int)(curr * 100 / total))), (s, e) =>
				{
					error = error ?? e.Error;

					lock (sync)
					{
						pulsed = true;
						sync.Pulse();
					}
				});
			}, out var cancelled);

			if (!cancelled)
			{
				lock (sync)
				{
					if (!pulsed)
						sync.Wait();
				}

				(stream as MemoryStream)?.UndoDispose();

				error?.Throw();
			}
			else
				source.Cancel();

			return source;
		}

		bool IBackupService.CanPublish => true;

		string IBackupService.Publish(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			return Process(client =>
			{
				return client.AsyncWait<GenericSdkEventArgs<string>, string>(
					nameof(DiskSdkClient.PublishCompleted),
					() => client.PublishAsync(path),
					e => e.Result);
			}, out _);
		}

		void IBackupService.UnPublish(BackupEntry entry)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			var path = GetPath(entry);

			Process(client =>
			{
				client.AsyncWait<SdkEventArgs, object>(
					nameof(DiskSdkClient.UnpublishCompleted),
					() => client.UnpublishAsync(path),
					e => e);
			}, out _);
		}
	}
}