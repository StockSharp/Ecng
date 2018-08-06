#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Algo
File: AmazonS3Service.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace Ecng.Backup.Amazon
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;

	using global::Amazon;
	using global::Amazon.Runtime;
	using global::Amazon.S3;
	using global::Amazon.S3.Model;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The data storage service based on Amazon S3 http://aws.amazon.com/s3/.
	/// </summary>
	public class AmazonS3Service : Disposable, IBackupService
	{
		private readonly string _bucket;
		private readonly AmazonS3Client _client;
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
			: this(RegionEndpoint.GetBySystemName(endpoint), bucket, accessKey, secretKey)
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

		IEnumerable<BackupEntry> IBackupService.Find(BackupEntry parent, string criteria)
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

			do
			{
				var response = _client.ListObjectsV2(request);

				foreach (var entry in response.S3Objects)
				{
					var be = GetPath(entry.Key);
					be.Size = entry.Size;
					yield return be;
				}

				foreach (var commonPrefix in response.CommonPrefixes)
				{
					yield return new BackupEntry
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

		IEnumerable<BackupEntry> IBackupService.GetChilds(BackupEntry parent)
		{
			return ((IBackupService)this).Find(parent, null);
		}

		void IBackupService.Delete(BackupEntry entry)
		{
			_client.DeleteObject(_bucket, GetKey(entry));
		}

		// TODO make async

		public CancellationTokenSource Download(BackupEntry entry, byte[] buffer, long start, int length, Action<int> progress)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var source = new CancellationTokenSource();

			var key = GetKey(entry);

			var request = new GetObjectRequest
			{
				BucketName = _bucket,
				Key = key,
				ByteRange = new ByteRange(start, start + length)
			};

			var bytes = new byte[_bufferSize];
			var readTotal = 0L;

			using (var response = _client.GetObject(request))
			using (var responseStream = response.ResponseStream)
			{
				response.WriteObjectProgressEvent += (s, a) => progress(a.PercentDone);

				while (readTotal < response.ContentLength)
				{
					var len = (int)(response.ContentLength - readTotal).Min(bytes.Length);

					responseStream.ReadBytes(bytes, len);
					bytes.CopyTo(buffer, readTotal);

					readTotal += len;
				}
			}

			return source;
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

			var key = GetKey(entry);

			var request = new GetObjectRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			var bytes = new byte[_bufferSize];
			var readTotal = 0L;

			using (var response = _client.GetObject(request))
			using (var responseStream = response.ResponseStream)
			{
				response.WriteObjectProgressEvent += (s, a) => progress(a.PercentDone);

				while (readTotal < response.ContentLength)
				{
					var len = (int)(response.ContentLength - readTotal).Min(bytes.Length);

					responseStream.ReadBytes(bytes, len);
					stream.Write(bytes, 0, len);

					readTotal += len;
				}
			}

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

			var key = GetKey(entry);

			var initResponse = _client.InitiateMultipartUpload(new InitiateMultipartUploadRequest
			{
				BucketName = _bucket,
				Key = key,
			});

			var filePosition = 0L;
			var nextProgress = 1;

			var etags = new List<PartETag>();

			var partNum = 1;

			while (filePosition < stream.Length)
			{
				var response = _client.UploadPart(new UploadPartRequest
				{
					BucketName = _bucket,
					UploadId = initResponse.UploadId,
					PartNumber = partNum,
					PartSize = _bufferSize,
					//FilePosition = filePosition,
					InputStream = stream,
					Key = key
				});

				etags.Add(new PartETag(partNum, response.ETag));

				filePosition += _bufferSize;

				var currProgress = (int)(filePosition.Min(stream.Length) * 100 / stream.Length);

				if (currProgress >= nextProgress)
				{
					progress(currProgress);
					nextProgress = currProgress + 1;
				}

				partNum++;
			}

			_client.CompleteMultipartUpload(new CompleteMultipartUploadRequest
			{
				BucketName = _bucket,
				UploadId = initResponse.UploadId,
				Key = key,
				PartETags = etags
			});

			var source = new CancellationTokenSource();

			return source;
		}

		void IBackupService.FillInfo(BackupEntry entry)
		{
			var key = GetKey(entry);

			var request = new GetObjectMetadataRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			var response = _client.GetObjectMetadata(request);

			entry.Size = response.ContentLength;
		}

		bool IBackupService.CanPublish => false;

		string IBackupService.Publish(BackupEntry entry)
		{
			throw new NotSupportedException();
		}

		void IBackupService.UnPublish(BackupEntry entry)
		{
			throw new NotSupportedException();
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