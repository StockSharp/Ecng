namespace Ecng.Backup.Amazon
{
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

		bool IBackupService.CanFolders => false;
		bool IBackupService.CanPublish => false;
		bool IBackupService.CanPartialDownload => true;

		IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
			=> _client.DeleteArchiveAsync(new()
			{
				VaultName = _vaultName,
				ArchiveId = entry.Name,
			}, cancellationToken);

		Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
			=> throw new NotImplementedException();

		async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
		{
			GetJobOutputRequest request = new()
			{
				//JobId = jobId,
				VaultName = _vaultName,
			};

			if (offset != null || length != null)
			{
				if (offset is null || length is null)
					throw new NotSupportedException();

				request.Range = $"bytes={offset}-{offset + length}";
			}

			var response = await _client.GetJobOutputAsync(request, cancellationToken);

			using var webStream = response.Body;

			var bytes = new byte[_bufferSize];
			var readTotal = 0L;

			var prevProgress = -1;

			var objLen = 0L; //TODO

			while (readTotal < objLen)
			{
				var expected = (int)(objLen - readTotal).Min(_bufferSize);
				var actual = await webStream.ReadAsync(bytes, 0, expected, cancellationToken);

				if (actual == 0)
					break;

				await stream.WriteAsync(bytes, 0, actual, cancellationToken);

				readTotal += actual;

				var currProgress = (int)(readTotal * 100L / objLen);

				if (currProgress < 100 && prevProgress < currProgress)
				{
					progress(currProgress);
					prevProgress = currProgress;
				}
			}
		}

		Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
			=> throw new NotImplementedException();

		Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
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
}