namespace Ecng.Backup.Amazon;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
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

	private readonly ConcurrentDictionary<string, ArchiveInfo> _sessionArchives = new(StringComparer.Ordinal);
	private volatile InventoryCache _inventory;

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
		: this(endpoint, vaultName, new BasicAWSCredentials(accessKey, secretKey))
	{
	}

	[CLSCompliant(false)]
	public AmazonGlacierService(RegionEndpoint endpoint, string vaultName, AWSCredentials credentials)
		: this(new AmazonGlacierClient(credentials, endpoint), endpoint, vaultName, credentials)
	{
	}

	/// <summary>
	/// Initializes a new instance with a custom <see cref="IAmazonGlacier"/> implementation.
	/// </summary>
	/// <param name="client">The Glacier client.</param>
	/// <param name="endpoint">Region endpoint.</param>
	/// <param name="vaultName">Vault name.</param>
	/// <param name="credentials">Optional credentials.</param>
	[CLSCompliant(false)]
	public AmazonGlacierService(IAmazonGlacier client, RegionEndpoint endpoint, string vaultName, AWSCredentials credentials = null)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
		_endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
		_vaultName = vaultName.ThrowIfEmpty(nameof(vaultName));
		_credentials = credentials;
	}

	private TimeSpan _jobTimeOut = TimeSpan.FromHours(6);

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

	private TimeSpan _pollInterval = TimeSpan.FromMinutes(1);

	/// <summary>
	/// Poll interval for Glacier jobs.
	/// </summary>
	public TimeSpan PollInterval
	{
		get => _pollInterval;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), "Poll interval must be positive.");

			_pollInterval = value;
		}
	}

	bool IBackupService.CanFolders => false;
	bool IBackupService.CanPublish => false;
	bool IBackupService.CanExpirable => false;
	bool IBackupService.CanPartialDownload => true;

	IAsyncEnumerable<BackupEntry> IBackupService.FindAsync(BackupEntry parent, string criteria)
		=> FindAsyncImpl(parent, criteria);

	private async IAsyncEnumerable<BackupEntry> FindAsyncImpl(BackupEntry parent, string criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// Glacier inventory is eventually consistent and can be up to 24h stale.
		// We rely on the last inventory plus in-session uploads.
		var inventory = await GetInventoryAsync(cancellationToken).NoWait();

		var parentPrefix = parent?.GetFullPath();
		if (!parentPrefix.IsEmpty())
			parentPrefix = parentPrefix.TrimEnd('/') + "/";

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var item in inventory.Archives.Values.Concat(_sessionArchives.Values))
		{
			cancellationToken.ThrowIfCancellationRequested();

			var fullPath = item.Description;

			if (fullPath.IsEmpty())
				continue;

			if (!parentPrefix.IsEmpty() && !fullPath.StartsWith(parentPrefix, StringComparison.OrdinalIgnoreCase))
				continue;

			var name = fullPath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
			if (name.IsEmpty())
				continue;

			if (!criteria.IsEmpty() && !name.ContainsIgnoreCase(criteria))
				continue;

			if (!seen.Add(fullPath))
				continue;

			var be = GetPath(fullPath);
			be.Parent = parent;
			be.Size = item.Size;
			be.LastModified = item.CreationDate?.ToLocalTime() ?? default;
			yield return be;
		}
	}

	Task IBackupService.DeleteAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> DeleteByPathAsync(entry, cancellationToken);

	async Task IBackupService.FillInfoAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		var info = await ResolveArchiveAsync(entry, cancellationToken).NoWait();
		entry.Size = info.Size;
		entry.LastModified = info.CreationDate?.ToLocalTime() ?? default;
	}

	async Task IBackupService.DownloadAsync(BackupEntry entry, Stream stream, long? offset, long? length, Action<int> progress, CancellationToken cancellationToken)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (progress is null)
			throw new ArgumentNullException(nameof(progress));

		var archiveId = await ResolveArchiveIdAsync(entry, cancellationToken).NoWait();

		var init = await _client.InitiateJobAsync(new()
		{
			VaultName = _vaultName,
			JobParameters = new()
			{
				ArchiveId = archiveId,
				Type = "archive-retrieval",
			}
		}, cancellationToken).NoWait();

		var describe = await WaitForJobAsync(init.JobId, cancellationToken).NoWait();

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

		var description = entry.GetFullPath();

		_sessionArchives[description] = new ArchiveInfo
		{
			ArchiveId = response.ArchiveId,
			Description = description,
			Size = stream.Length,
			CreationDate = DateTime.UtcNow,
		};

		progress(100);
	}

	Task<string> IBackupService.PublishAsync(BackupEntry entry, TimeSpan? expiresIn, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	Task IBackupService.UnPublishAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	Task IBackupService.CreateFolder(BackupEntry entry, CancellationToken cancellationToken)
		=> throw new NotSupportedException();

	private sealed class InventoryCache
	{
		public DateTime CreatedUtc { get; init; }
		public Dictionary<string, ArchiveInfo> Archives { get; init; }
	}

	private sealed class ArchiveInfo
	{
		public string ArchiveId { get; init; }
		public string Description { get; init; }
		public long Size { get; init; }
		public DateTime? CreationDate { get; init; }
	}

	private sealed class InventoryResponse
	{
		public List<InventoryArchive> ArchiveList { get; set; }
	}

	private sealed class InventoryArchive
	{
		public string ArchiveId { get; set; }
		public string ArchiveDescription { get; set; }
		public long Size { get; set; }
		public string CreationDate { get; set; }
	}

	private async Task<InventoryCache> GetInventoryAsync(CancellationToken cancellationToken)
	{
		var cached = _inventory;
		if (cached is not null && (DateTime.UtcNow - cached.CreatedUtc) < TimeSpan.FromHours(1))
			return cached;

		var init = await _client.InitiateJobAsync(new InitiateJobRequest
		{
			VaultName = _vaultName,
			JobParameters = new JobParameters
			{
				Type = "inventory-retrieval",
				Format = "JSON",
			},
		}, cancellationToken).NoWait();

		await WaitForJobAsync(init.JobId, cancellationToken).NoWait();

		var output = await _client.GetJobOutputAsync(new GetJobOutputRequest
		{
			JobId = init.JobId,
			VaultName = _vaultName,
		}, cancellationToken).NoWait();

		using var body = output.Body;
		using var ms = new MemoryStream();
		await body.CopyToAsync(ms, cancellationToken).NoWait();

		var text = System.Text.Encoding.UTF8.GetString(ms.ToArray());
		var inv = JsonSerializer.Deserialize<InventoryResponse>(text);

		var dict = new Dictionary<string, ArchiveInfo>(StringComparer.Ordinal);

		foreach (var a in inv?.ArchiveList ?? [])
		{
			if (a?.ArchiveId.IsEmpty() != false)
				continue;

			var desc = a.ArchiveDescription;
			if (desc.IsEmpty())
				continue;

			DateTime? created = null;
			if (!a.CreationDate.IsEmpty() && DateTime.TryParse(a.CreationDate, out var dt))
				created = dt.ToUniversalTime();

			dict[desc] = new ArchiveInfo
			{
				ArchiveId = a.ArchiveId,
				Description = desc,
				Size = a.Size,
				CreationDate = created,
			};
		}

		cached = new InventoryCache
		{
			CreatedUtc = DateTime.UtcNow,
			Archives = dict,
		};

		_inventory = cached;
		return cached;
	}

	private static BackupEntry GetPath(string key)
	{
		BackupEntry entry = null;

		foreach (var part in key.Split('/', StringSplitOptions.RemoveEmptyEntries))
			entry = new() { Name = part, Parent = entry };

		return entry;
	}

	private async Task<DescribeJobResponse> WaitForJobAsync(string jobId, CancellationToken cancellationToken)
	{
		var left = JobTimeOut;

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var describe = await _client.DescribeJobAsync(new DescribeJobRequest
			{
				JobId = jobId,
				VaultName = _vaultName,
			}, cancellationToken).NoWait();

			if (describe.Completed == true)
				return describe;

			if (left <= TimeSpan.Zero)
				throw new TimeoutException("Timed out waiting for Amazon Glacier job to complete.");

			var delay = left < PollInterval ? left : PollInterval;
			await delay.Delay(cancellationToken).NoWait();
			left -= delay;
		}
	}

	private async Task DeleteByPathAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entry);

		var archiveId = await ResolveArchiveIdAsync(entry, cancellationToken).NoWait();

		await _client.DeleteArchiveAsync(new DeleteArchiveRequest
		{
			VaultName = _vaultName,
			ArchiveId = archiveId,
		}, cancellationToken).NoWait();

		var desc = entry.GetFullPath();
		_sessionArchives.TryRemove(desc, out _);

		var inv = _inventory;
		if (inv is not null)
			inv.Archives.Remove(desc);
	}

	private async Task<string> ResolveArchiveIdAsync(BackupEntry entry, CancellationToken cancellationToken)
		=> (await ResolveArchiveAsync(entry, cancellationToken).NoWait()).ArchiveId;

	private async Task<ArchiveInfo> ResolveArchiveAsync(BackupEntry entry, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(entry);

		var desc = entry.GetFullPath();

		if (_sessionArchives.TryGetValue(desc, out var inSession))
			return inSession;

		var inventory = await GetInventoryAsync(cancellationToken).NoWait();
		if (inventory.Archives.TryGetValue(desc, out var fromInventory))
			return fromInventory;

		// Fallback: try match by file name only.
		var name = entry.Name;
		if (!name.IsEmpty())
		{
			var candidate = inventory.Archives.Values
				.Where(a => a.Description?.EndsWith("/" + name, StringComparison.OrdinalIgnoreCase) == true || a.Description.Equals(name, StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(a => a.CreationDate ?? DateTime.MinValue)
				.FirstOrDefault();

			if (candidate is not null)
				return candidate;
		}

		throw new ArgumentOutOfRangeException(nameof(entry), $"Archive not found in vault inventory: {desc}.");
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
