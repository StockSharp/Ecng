namespace Ecng.Backup.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Net;
	using System.Runtime.CompilerServices;

	using global::Amazon;
	using global::Amazon.Runtime;
	using global::Amazon.S3;
	using global::Amazon.S3.Model;

	using Ecng.Common;

	/// <summary>
	/// The data storage service based on Amazon S3 https://aws.amazon.com/s3/ .
	/// </summary>
	public class AmazonS3Service : Disposable, IBackupService
	{
		private readonly string _bucket;
		private readonly IAmazonS3 _client;
		private const int _bufferSize = FileSizes.MB * 10;
		private readonly AWSCredentials _credentials;
		private readonly RegionEndpoint _endpoint;

		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonS3Service"/>.
		/// </summary>
		/// <param name="endpoint">Region address.</param>
		/// <param name="bucket">Storage name.</param>
		/// <param name="accessKey">Key.</param>
		/// <param name="secretKey">Secret.</param>
		public AmazonS3Service(string endpoint, string bucket, string accessKey, string secretKey)
			: this(AmazonExtensions.GetEndpoint(endpoint), bucket, accessKey, secretKey)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonS3Service"/>.
		/// </summary>
		/// <param name="endpoint">Region address.</param>
		/// <param name="bucket">Storage name.</param>
		/// <param name="accessKey">Key.</param>
		/// <param name="secretKey">Secret.</param>
		[CLSCompliant(false)]
		public AmazonS3Service(RegionEndpoint endpoint, string bucket, string accessKey, string secretKey)
		{
			_credentials = new BasicAWSCredentials(accessKey, secretKey);
			_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			_bucket = bucket.ThrowIfEmpty(nameof(bucket));
			_client = new AmazonS3Client(_credentials, _endpoint);
		}

		bool IBackupService.CanFolders => false;
		bool IBackupService.CanPublish => true;
		bool IBackupService.CanPartialDownload => true;

		async IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria, [EnumeratorCancellation]CancellationToken cancellationToken)
		{
			//if (parent != null && !parent.IsDirectory)
			//	throw new ArgumentException("{0} should be directory.".Put(parent.Name), "parent");

			var request = new ListObjectsV2Request
			{
				BucketName = _bucket,
				Prefix = parent != null ? parent.GetFullPath() : null,
			};

			if (!criteria.IsEmpty())
				request.Prefix += "/" + criteria;

			do
			{
				var response = await _client.ListObjectsV2Async(request, cancellationToken);

				foreach (var entry in response.S3Objects)
				{
					var be = GetPath(entry.Key);
					be.LastModified = entry.LastModified;
					be.Size = entry.Size;
					yield return be;
				}

				foreach (var commonPrefix in response.CommonPrefixes)
				{
					yield return new()
					{
						Name = commonPrefix,
						Parent = parent,
					};
				}

				if (response.IsTruncated)
					request.ContinuationToken = response.NextContinuationToken;
				else
					break;
			}
			while (true);
		}

		Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
			=> _client.DeleteObjectAsync(_bucket, entry.GetFullPath(), cancellationToken);

		async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
		{
			if (entry is null)
				throw new ArgumentNullException(nameof(entry));

			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (progress is null)
				throw new ArgumentNullException(nameof(progress));

			var key = entry.GetFullPath();

			var request = new GetObjectRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			if (offset != null || length != null)
			{
				if (offset is null || length is null)
					throw new NotSupportedException();

				request.ByteRange = new(offset.Value, offset.Value + length.Value);
			}

			var bytes = new byte[_bufferSize];
			var readTotal = 0L;

			var prevProgress = -1;

			using (var response = await _client.GetObjectAsync(request, cancellationToken))
			using (var responseStream = response.ResponseStream)
			{
				var objLen = response.ContentLength;

				while (readTotal < objLen)
				{
					var expected = (int)(objLen - readTotal).Min(_bufferSize);
					var actual = await responseStream.ReadAsync(bytes, 0, expected, cancellationToken);

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

			if (prevProgress < 100)
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

			var key = entry.GetFullPath();

			var initResponse = await _client.InitiateMultipartUploadAsync(new()
			{
				BucketName = _bucket,
				Key = key,
			}, cancellationToken);

			var filePosition = 0L;
			var prevProgress = -1;

			var etags = new List<PartETag>();

			var partNum = 1;

			while (filePosition < stream.Length)
			{
				var response = await _client.UploadPartAsync(new()
				{
					BucketName = _bucket,
					UploadId = initResponse.UploadId,
					PartNumber = partNum,
					PartSize = _bufferSize,
					//FilePosition = filePosition,
					InputStream = stream,
					Key = key
				}, cancellationToken);

				etags.Add(new(partNum, response.ETag));

				filePosition += _bufferSize;

				var currProgress = (int)(filePosition.Min(stream.Length) * 100 / stream.Length);

				if (currProgress > prevProgress)
				{
					progress(currProgress);
					prevProgress = currProgress;
				}

				partNum++;
			}

			await _client.CompleteMultipartUploadAsync(new()
			{
				BucketName = _bucket,
				UploadId = initResponse.UploadId,
				Key = key,
				PartETags = etags
			}, cancellationToken);

			if (prevProgress < 100)
				progress(100);
		}

		Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			var key = entry.GetFullPath();

			var response = await _client.GetObjectMetadataAsync(new()
			{
				BucketName = _bucket,
				Key = key,
			}, cancellationToken);

			entry.Size = response.ContentLength;
		}

		async Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			var key = entry.GetFullPath();

			var response = await _client.PutACLAsync(new()
			{
				BucketName = _bucket,
				Key = key,
				CannedACL = S3CannedACL.PublicRead,
			}, cancellationToken);

			if (response.HttpStatusCode != HttpStatusCode.OK)
				throw new InvalidOperationException(response.HttpStatusCode.To<string>());
			
			return $"https://{_bucket}.s3.{_endpoint.SystemName}.amazonaws.com/{key}";
		}

		async Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			var key = entry.GetFullPath();

			var response = await _client.PutACLAsync(new()
			{
				BucketName = _bucket,
				Key = key,
				CannedACL = S3CannedACL.Private,
			}, cancellationToken);

			if (response.HttpStatusCode != HttpStatusCode.OK)
				throw new InvalidOperationException(response.HttpStatusCode.To<string>());
		}

		private static BackupEntry GetPath(string key)
		{
			var entities = key.Split('/').Select(p => new BackupEntry { Name = p }).ToArray();

			BackupEntry parent = null;

			foreach (var entity in entities)
			{
				entity.Parent = parent;
				parent = entity;
			}

			return entities.Last();
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			_client.Dispose();
			base.DisposeManaged();
		}
	}
}