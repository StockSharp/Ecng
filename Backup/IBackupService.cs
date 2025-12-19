namespace Ecng.Backup;

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

	/// <summary>
	/// Is expirable publishing feature available (via <see cref="PublishAsync"/> with <c>expiresIn</c>).
	/// </summary>
	bool CanExpirable { get; }

	/// <summary>
	/// Is folders feature available.
	/// </summary>
	bool CanFolders { get; }

	/// <summary>
	/// Is partial download feature available.
	/// </summary>
	bool CanPartialDownload { get; }

	/// <summary>
	/// Create folder on the service.
	/// </summary>
	/// <param name="entry">Folder to create.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	Task CreateFolder(BackupEntry entry, CancellationToken cancellationToken = default);

	/// <summary>
	/// Find files by the specified criteria.
	/// </summary>
	/// <param name="parent">Parent element. Can be null.</param>
	/// <param name="criteria">Criteria.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>File list.</returns>
	IAsyncEnumerable<BackupEntry> FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken = default);

	/// <summary>
	/// Fill file info.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken = default);

	/// <summary>
	/// Delete file from the service.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task DeleteAsync(BackupEntry entry, CancellationToken cancellationToken = default);

	/// <summary>
	/// Download file.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="stream">The stream to write downloaded data into.</param>
	/// <param name="offset">Optional offset to start downloading from.</param>
	/// <param name="length">Optional length to download.</param>
	/// <param name="progress">Progress notification.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken = default);

	/// <summary>
	/// Upload file.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="stream">The stream to read data from for uploading.</param>
	/// <param name="progress">Progress notification.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken = default);

	/// <summary>
	/// Get public url for the specified element.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="expiresIn">Link expiration. If null, means infinite.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Public url.</returns>
	Task<string> PublishAsync(BackupEntry entry, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Remove public url for the specified element.
	/// </summary>
	/// <param name="entry">Element.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	Task UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken = default);
}