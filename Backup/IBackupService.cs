namespace Ecng.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// The interface describing online data storage service.
	/// </summary>
	public interface IBackupService : IDisposable
	{
		/// <summary>
		/// Is publishing feature available.
		/// </summary>
		bool CanPublish { get; }

		bool CanFolders { get; }

		bool CanPartialDownload { get; }

		/// <summary>
		/// Find files by the specified criteria.
		/// </summary>
		/// <param name="parent">Parent element. Can be null.</param>
		/// <param name="criteria">Criteria.</param>
		/// <returns>File list.</returns>
		IAsyncEnumerable<BackupEntry> FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken = default);

		/// <summary>
		/// Fill file info.
		/// </summary>
		/// <param name="entry">Element.</param>
		Task FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken = default);

		/// <summary>
		/// Delete file from the service.
		/// </summary>
		/// <param name="entry">Element.</param>
		Task DeleteAsync(BackupEntry entry, CancellationToken cancellationToken = default);

		/// <summary>
		/// Save file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		Task DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken = default);

		/// <summary>
		/// Upload file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream">The stream of the open file into which data from the service will be downloaded.</param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		Task UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken = default);

		/// <summary>
		/// Get public url for the specified element.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <returns>Public url.</returns>
		Task<string> PublishAsync(BackupEntry entry, CancellationToken cancellationToken = default);

		/// <summary>
		/// Remove public url for the specified element.
		/// </summary>
		/// <param name="entry">Element.</param>
		Task UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken = default);
	}
}