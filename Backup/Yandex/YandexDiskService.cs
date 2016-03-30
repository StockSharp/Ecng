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
	public class YandexDiskService : IBackupService
	{
		//private readonly Window _owner;
		private DiskSdkClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="YandexDiskService"/>.
		/// </summary>
		/// <param name="owner">The login window owner.</param>
		public YandexDiskService(Window owner = null)
		{
			//_owner = owner;

			Exception error = null;

			var loginWindow = new YandexLoginWindow();
			loginWindow.AuthCompleted += (s, e) =>
			{
				if (e.Error == null)
				{
					_client = new DiskSdkClient(e.Result);

					//var remotePath = RootPath + "/" + fileName;

					//try
					//{
					//	result = action(client);
					//}
					//catch (Exception excp)
					//{
					//	error = excp;
					//}
				}
				else
					error = e.Error;
			};

			var retVal = owner == null ? loginWindow.ShowModal() : loginWindow.ShowModal(owner);

			if (!retVal)
				throw new UnauthorizedAccessException();

			error?.Throw();
		}

		///// <summary>
		///// The directory in the Yandex.Disk where the files will be downloaded.
		///// </summary>
		//public static string RootPath { get; set; } = "/StockSharp";

		///// <summary>
		///// To share a file.
		///// </summary>
		///// <param name="fileName">File name.</param>
		///// <param name="file">File.</param>
		///// <param name="owner">The login window owner.</param>
		///// <returns>The link to a file.</returns>
		//private static string Publish(string fileName, Stream file, Window owner = null)
		//{
		//	return Do(client =>
		//	{
		//		UploadFile(client, fileName, file);
		//		return Publish(client, fileName);
		//	}, owner);
		//}

		///// <summary>
		///// To replace a file.
		///// </summary>
		///// <param name="fileName">File name.</param>
		///// <param name="file">File.</param>
		///// <param name="owner">The login window owner.</param>
		//private static void Replace(string fileName, Stream file, Window owner = null)
		//{
		//	Do<object>(client =>
		//	{
		//		UploadFile(client, fileName, file);
		//		return null;
		//	}, owner);
		//}

		//private static void TryCreateDirectory(DiskSdkClient client, string path)
		//{
		//	var sync = new SyncObject();
		//	var items = Enumerable.Empty<DiskItemInfo>();

		//	Exception error = null;
		//	var pulsed = false;

		//	EventHandler<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>> listHandler = (s, e) =>
		//	{
		//		if (e.Error != null)
		//			error = e.Error;
		//		else
		//			items = e.Result;

		//		lock (sync)
		//		{
		//			pulsed = true;
		//			sync.Pulse();
		//		}
		//	};

		//	client.GetListCompleted += listHandler;
		//	client.GetListAsync();

		//	lock (sync)
		//	{
		//		if (!pulsed)
		//			sync.Wait();	
		//	}
			
		//	client.GetListCompleted -= listHandler;

		//	error?.Throw();

		//	if (items.Any(i => i.IsDirectory && i.OriginalFullPath.TrimEnd("/") == path))
		//		return;

		//	client.AsyncWait<SdkEventArgs, object>(
		//		nameof(DiskSdkClient.MakeFolderCompleted),
		//		() => client.MakeDirectoryAsync(path),
		//		e => e);
		//}

		//private static void UploadFile(DiskSdkClient client, string remotePath, Stream file)
		//{
		//	TryCreateDirectory(client, RootPath);

		//	var sync = new SyncObject();
		//	Exception error = null;

		//	client.UploadFileAsync(remotePath, file,
		//		new AsyncProgress((c, t) => { }),
		//		(us, ua) =>
		//		{
		//			error = ua.Error;
		//			sync.Pulse();
		//		});

		//	sync.Wait();

		//	error?.Throw();
		//}

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