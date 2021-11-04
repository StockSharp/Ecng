namespace Ecng.Backup.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Net;

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
		private const int _bufferSize = 1024 * 1024 * 10; // 10mb
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
		public AmazonS3Service(RegionEndpoint endpoint, string bucket, string accessKey, string secretKey)
		{
			if (bucket.IsEmpty())
				throw new ArgumentNullException(nameof(bucket));

			_credentials = new BasicAWSCredentials(accessKey, secretKey);
			_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
			_bucket = bucket;
			_client = new AmazonS3Client(_credentials, _endpoint);
		}

		bool IBackupService.CanFolders => true;

		async Task<IEnumerable<BackupEntry>> IBackupService.FindAsync(BackupEntry parent, string criteria, CancellationToken cancellationToken)
		{
			//if (parent != null && !parent.IsDirectory)
			//	throw new ArgumentException("{0} should be directory.".Put(parent.Name), "parent");

			var request = new ListObjectsV2Request
			{
				BucketName = _bucket,
				Prefix = parent != null ? GetKey(parent) : null,
			};

			if (!criteria.IsEmpty())
				request.Prefix += "/" + criteria;

			var retVal = new List<BackupEntry>();

			do
			{
				var response = await _client.ListObjectsV2Async(request, cancellationToken);

				foreach (var entry in response.S3Objects)
				{
					var be = GetPath(entry.Key);
					be.Size = entry.Size;
					retVal.Add(be);
				}

				foreach (var commonPrefix in response.CommonPrefixes)
				{
					retVal.Add(new BackupEntry
					{
						Name = commonPrefix,
						Parent = parent,
					});
				}

				if (response.IsTruncated)
					request.ContinuationToken = response.NextContinuationToken;
				else
					break;
			}
			while (true);

			return retVal;
		}

		Task<IEnumerable<BackupEntry>> IBackupService.GetChildsAsync(BackupEntry parent, CancellationToken cancellationToken)
		{
			return ((IBackupService)this).FindAsync(parent, null, cancellationToken);
		}

		async Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			await _client.DeleteObjectAsync(_bucket, GetKey(entry), cancellationToken);
		}

		async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
		{
			if (entry is null)
				throw new ArgumentNullException(nameof(entry));

			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (progress is null)
				throw new ArgumentNullException(nameof(progress));

			var key = GetKey(entry);

			var request = new GetObjectRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			if (offset != null || length != null)
			{
				if (offset is null || length is null)
					throw new NotSupportedException();

				request.ByteRange = new ByteRange(offset.Value, offset.Value + length.Value);
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

			var key = GetKey(entry);

			var initResponse = await _client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
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
				var response = await _client.UploadPartAsync(new UploadPartRequest
				{
					BucketName = _bucket,
					UploadId = initResponse.UploadId,
					PartNumber = partNum,
					PartSize = _bufferSize,
					//FilePosition = filePosition,
					InputStream = stream,
					Key = key
				}, cancellationToken);

				etags.Add(new PartETag(partNum, response.ETag));

				filePosition += _bufferSize;

				var currProgress = (int)(filePosition.Min(stream.Length) * 100 / stream.Length);

				if (currProgress > prevProgress)
				{
					progress(currProgress);
					prevProgress = currProgress;
				}

				partNum++;
			}

			await _client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
			{
				BucketName = _bucket,
				UploadId = initResponse.UploadId,
				Key = key,
				PartETags = etags
			}, cancellationToken);

			if (prevProgress < 100)
				progress(100);
		}

		async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			var key = GetKey(entry);

			var request = new GetObjectMetadataRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			var response = await _client.GetObjectMetadataAsync(request, cancellationToken);

			entry.Size = response.ContentLength;
		}

		bool IBackupService.CanPublish => true;

		async Task<string> IBackupService.PublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		{
			var key = GetKey(entry);

			var response = await _client.PutACLAsync(new PutACLRequest
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
			var key = GetKey(entry);

			var response = await _client.PutACLAsync(new PutACLRequest
			{
				BucketName = _bucket,
				Key = key,
				CannedACL = S3CannedACL.Private,
			}, cancellationToken);

			if (response.HttpStatusCode != HttpStatusCode.OK)
				throw new InvalidOperationException(response.HttpStatusCode.To<string>());
		}

		private static string GetKey(BackupEntry entry)
		{
			var key = entry.Name;

			if (key.IsEmpty())
				throw new ArgumentException(nameof(entry));

			if (entry.Parent != null)
				key = GetKey(entry.Parent) + "/" + key;

			return key;
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