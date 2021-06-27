namespace Ecng.Backup.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	using global::Amazon;
	using global::Amazon.Runtime;
	using global::Amazon.S3;

	using Ecng.Common;

	/// <summary>
	/// The data storage service based on Amazon Glacier http://aws.amazon.com/glacier/.
	/// </summary>
	public class AmazonGlacierService : Disposable, IBackupService
	{
		private readonly AmazonS3Client _client;
		private readonly string _vaultName;
		private readonly AWSCredentials _credentials;
		private readonly RegionEndpoint _endpoint;

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
			_client = new AmazonS3Client(_credentials, _endpoint);
		}

		Task<IEnumerable<BackupEntry>> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task<IEnumerable<BackupEntry>> IBackupService.GetChildsAsync(BackupEntry parent, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		Task IBackupService.UploadAsync(BackupEntry entry, Stream stream, Action<int> progress, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		bool IBackupService.CanPublish => false;

		Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

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