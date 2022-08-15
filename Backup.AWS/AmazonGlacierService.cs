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
		private const int _bufferSize = 1024 * 1024 * 100; // 100mb

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
		public AmazonGlacierService(RegionEndpoint endpoint, string vaultName, string accessKey, string secretKey)
		{
			if (vaultName.IsEmpty())
				throw new ArgumentNullException(nameof(vaultName));

			_credentials = new BasicAWSCredentials(accessKey, secretKey);
			_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			_vaultName = vaultName;
			_client = new AmazonGlacierClient(_credentials, _endpoint);
		}

		bool IBackupService.CanFolders => false;

		Task<IEnumerable<BackupEntry>> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task<IEnumerable<BackupEntry>> IBackupService.GetChildsAsync(BackupEntry parent, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

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
			var getJobOutputResponse = await _client.GetJobOutputAsync(new()
			{
				//JobId = jobId,
				VaultName = _vaultName
			}, cancellationToken);

			using var webStream = getJobOutputResponse.Body;

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

		bool IBackupService.CanPublish => false;

		Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
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