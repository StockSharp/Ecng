namespace Ecng.Backup.Amazon;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using global::Amazon;
using global::Amazon.Runtime;
using global::Amazon.Glacier.Model;
using global::Amazon.Glacier;

using Ecng.Common;

/// <summary>
/// The data storage service based on Amazon Glacier https://aws.amazon.com/s3/glacier/ .
/// </summary>
public class AmazonGlacierService : Disposable, IBackupService
{
	private readonly IAmazonGlacier _client;
	private readonly string _vaultName;
	private readonly AWSCredentials _credentials;
	private readonly RegionEndpoint _endpoint;
	private const int _bufferSize = FileSizes.MB * 100;

	/// <summary>
	/// Initializes a new instance of the <see cref="AmazonGlacierService"/>.
	/// </summary>
	/// <param name="endpoint">Region address.</param>
	/// <param name="bucket">Storage name.</param>
	/// <param name="accessKey">Key.</param>
	/// <param name="secretKey">Secret.</param>
	public AmazonGlacierService(string endpoint, string bucket, string accessKey, string secretKey)
		: this(AmazonExtensions.GetEndpoint(endpoint), bucket, accessKey, secretKey)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AmazonGlacierService"/>.
	/// </summary>
	/// <param name="endpoint">Region address.</param>
	/// <param name="vaultName">Storage name.</param>
	/// <param name="accessKey">Key.</param>
	/// <param name="secretKey">Secret.</param>
	[CLSCompliant(false)]
	public AmazonGlacierService(RegionEndpoint endpoint, string vaultName, string accessKey, string secretKey)
	{
		_credentials = new BasicAWSCredentials(accessKey, secretKey);
		_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
		_vaultName = vaultName.ThrowIfEmpty(nameof(vaultName));
		_client = new AmazonGlacierClient(_credentials, _endpoint);
	}

	private TimeSpan _jobTimeOut = TimeSpan.FromMinutes(10);

	/// <summary>
	/// Job timeout.
	/// </summary>
	public TimeSpan JobTimeOut
	{
		get => _jobTimeOut;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), "Job timeout must be positive.");

			_jobTimeOut = value;
		}
	}

	bool IBackupService.CanFolders => false;
	bool IBackupService.CanPublish => false;
	bool IBackupService.CanExpirable => false;
	bool IBackupService.CanPartialDownload => true;

	IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> _client.DeleteArchiveAsync(new()
		{
			VaultName = _vaultName,
			ArchiveId = entry.Name,
		}, cancellationToken);

	Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (progress is null)
			throw new ArgumentNullException(nameof(progress));

		var init = await _client.InitiateJobAsync(new()
		{
			VaultName = _vaultName,
			JobParameters = new()
			{
				ArchiveId = entry.Name,
				Type = "archive-retrieval",
			}
		}, cancellationToken).NoWait();

		var left = JobTimeOut;
		var interval = TimeSpan.FromMinutes(1);

		DescribeJobResponse describe;

		do
		{
			await interval.Delay(cancellationToken).NoWait();

			describe = await _client.DescribeJobAsync(new()
			{
				JobId = init.JobId,
				VaultName = _vaultName,
			}, cancellationToken).NoWait();

			left -= interval;

			if (left <= TimeSpan.Zero)
				throw new TimeoutException("Timed out waiting for Amazon Glacier job to complete.");
		}
		while (describe.Completed == false);

		GetJobOutputRequest request = new()
		{
			JobId = init.JobId,
			VaultName = _vaultName,
		};

		if (offset != null || length != null)
		{
			if (offset is null || length is null)
				throw new NotSupportedException();

			var end = offset.Value + length.Value - 1; // inclusive end
			request.Range = $"bytes={offset}-{end}";
		}

		var response = await _client.GetJobOutputAsync(request, cancellationToken).NoWait();

		using var webStream = response.Body;

		var bytes = new byte[_bufferSize];
		var readTotal = 0L;
		var objLen = describe.ArchiveSizeInBytes ?? 0;
		var prevProgress = -1;

		while (true)
		{
			var actual = await webStream.ReadAsync(bytes.AsMemory(0, _bufferSize), cancellationToken).NoWait();

			if (actual == 0)
				break;

			await stream.WriteAsync(bytes.AsMemory(0, actual), cancellationToken).NoWait();

			readTotal += actual;

			if (objLen > 0)
			{
				var currProgress = (int)(readTotal * 100L / objLen);

				if (currProgress < 100 && prevProgress < currProgress)
				{
					progress(currProgress);
					prevProgress = currProgress;
				}
			}
		}

		if (objLen > 0 && prevProgress < 100)
			progress(100);
	}

	async Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (progress is null)
			throw new ArgumentNullException(nameof(progress));

		var response = await _client.UploadArchiveAsync(new()
		{
			VaultName = _vaultName,
			ArchiveDescription = entry.GetFullPath(),
			Body = stream,
		}, cancellationToken).NoWait();

		entry.Name = response.ArchiveId;

		progress(100);
	}

	Task<string> IBackupService.PublishAsync(BackupEntry entry, TimeSpan? expiresIn, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	/// <summary>
	/// Disposes the managed resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_client.Dispose();
		base.DisposeManaged();
	}
}
