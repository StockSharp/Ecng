namespace Ecng.Tests.Backup;

using Ecng.Backup.Mega.Native;

[TestClass]
[TestCategory("Integration")]
public class MegaNativeClientTests : BaseTestClass
{
	private (string email, string password) LoadMegaSecrets()
		=> (GetSecret("BACKUP_MEGA_EMAIL"), GetSecret("BACKUP_MEGA_PASSWORD"));

	/// <summary>
	/// A read-only stream wrapper that never returns more than one byte per
	/// <see cref="Read(byte[], int, int)"/> call, emulating the partial reads that
	/// real network streams (and many wrapping streams) are allowed to perform.
	/// </summary>
	private sealed class OneByteAtATimeStream(byte[] data) : Stream
	{
		private readonly byte[] _data = data ?? throw new ArgumentNullException(nameof(data));
		private int _pos;

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _data.Length;

		public override long Position
		{
			get => _pos;
			set => _pos = (int)value;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count <= 0 || _pos >= _data.Length)
				return 0;

			// Deliberately satisfy at most a single byte per call.
			buffer[offset] = _data[_pos++];
			return 1;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			_pos = origin switch
			{
				SeekOrigin.Begin => (int)offset,
				SeekOrigin.Current => _pos + (int)offset,
				SeekOrigin.End => _data.Length + (int)offset,
				_ => _pos,
			};

			return _pos;
		}

		public override void Flush() { }
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	private async Task<Client> LoginAsync(CancellationToken cancellationToken)
	{
		var (email, password) = LoadMegaSecrets();

		var client = new Client();
		await client.LoginAsync(email, password, cancellationToken);
		return client;
	}

	private static async Task<string> GetRootIdAsync(Client client, CancellationToken cancellationToken)
	{
		var nodes = await client.GetNodesAsync(cancellationToken);
		return nodes.First(n => n.Type == NodeType.Root).Id;
	}

	[TestMethod]
	public async Task Login_And_GetNodes()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		nodes.Any(n => n.Type == NodeType.Root).AssertTrue();
	}

	[TestMethod]
	public async Task Roundtrip_Upload_Download_Delete()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		var root = nodes.First(n => n.Type == NodeType.Root);

		var folder = await client.CreateFolderAsync(root.Id, "ecng-mega-native-tests-" + Guid.NewGuid().ToString("N"), CancellationToken);

		var data = RandomGen.GetBytes(4096);

		var fileName = $"test-{Guid.NewGuid():N}.bin";

		Node fileNode = null;

		try
		{
			using (var uploadStream = new MemoryStream(data, writable: false))
				fileNode = await client.UploadAsync(folder.Id, fileName, uploadStream, modificationDate: null, progress: null, CancellationToken);

			using var downloadStream = new MemoryStream();
			await client.DownloadAsync(fileNode, downloadStream, progress: null, CancellationToken);
			downloadStream.ToArray().AssertEqual(data);
		}
		finally
		{
			try
			{
				if (fileNode is not null)
					await client.DeleteAsync(fileNode.Id, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await client.DeleteAsync(folder.Id, CancellationToken);
			}
			catch
			{
			}
		}
	}

	[TestMethod]
	public async Task Logout_InvalidatesSession()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		client.IsLoggedIn.AssertTrue();

		await client.LogoutAsync(CancellationToken);

		client.IsLoggedIn.AssertFalse();
		await ThrowsAsync<InvalidOperationException>(() => client.GetNodesAsync(CancellationToken));

		await client.LoginAsync(email, password, CancellationToken);
		client.IsLoggedIn.AssertTrue();
	}

	[TestMethod]
	public async Task Publish_Unpublish_File()
	{
		var (email, password) = LoadMegaSecrets();

		using var client = new Client();
		await client.LoginAsync(email, password, CancellationToken);

		var nodes = await client.GetNodesAsync(CancellationToken);
		var root = nodes.First(n => n.Type == NodeType.Root);

		var folder = await client.CreateFolderAsync(root.Id, "ecng-mega-native-publish-tests-" + Guid.NewGuid().ToString("N"), CancellationToken);

		var data = RandomGen.GetBytes(1024);

		var fileName = $"publish-{Guid.NewGuid():N}.bin";

		Node fileNode = null;

		try
		{
			using (var uploadStream = new MemoryStream(data, writable: false))
				fileNode = await client.UploadAsync(folder.Id, fileName, uploadStream, modificationDate: null, progress: null, CancellationToken);

			var url = await client.PublishAsync(fileNode, CancellationToken);
			url.IsEmpty().AssertFalse();

			url.Contains("/file/", StringComparison.OrdinalIgnoreCase).AssertTrue(url);
			url.Contains('#', StringComparison.Ordinal).AssertTrue(url);

			var phStart = url.IndexOf("/file/", StringComparison.OrdinalIgnoreCase);
			(phStart >= 0).AssertTrue(url);
			phStart += "/file/".Length;

			var hashPos = url.IndexOf('#', phStart);
			(hashPos > phStart).AssertTrue(url);

			var publicHandle = url.Substring(phStart, hashPos - phStart);
			publicHandle.IsEmpty().AssertFalse();

			var dl = await client.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
			dl.Url.IsEmpty().AssertFalse();
			(dl.Size > 0).AssertTrue();

			await client.UnpublishAsync(fileNode.Id, CancellationToken);

			var resolvedAfterUnpublish = false;

			for (var i = 0; i < 5; i++)
			{
				try
				{
					await client.GetPublicDownloadUrlAsync(publicHandle, CancellationToken);
					resolvedAfterUnpublish = true;
					await Task.Delay(250, CancellationToken);
				}
				catch (InvalidOperationException)
				{
					resolvedAfterUnpublish = false;
					break;
				}
			}

			resolvedAfterUnpublish.AssertFalse("Public link still resolves after unpublish.");
		}
		finally
		{
			try
			{
				if (fileNode is not null)
					await client.DeleteAsync(fileNode.Id, CancellationToken);
			}
			catch
			{
			}

			try
			{
				await client.DeleteAsync(folder.Id, CancellationToken);
			}
			catch
			{
			}
		}
	}

	/// <summary>
	/// BUG: MegaAesCtrStream.Read fills each 16-byte block with at most two
	/// Stream.Read calls; a source stream that returns fewer bytes than requested
	/// more than twice in a row leaves the block partially filled, yet pos still
	/// advances by 16 and the MAC/keystream only cover the bytes actually read.
	/// This corrupts every subsequent block. (Backup.Mega\Native\Streams.cs:101-103)
	/// Expected: uploading from a short-reading source and downloading back yields
	/// the exact original bytes (a full read loop must drain each block).
	/// Actual: the encrypted upload is corrupted, so the download either throws
	/// "Checksum is invalid" or returns mismatching bytes.
	/// </summary>
	[TestMethod]
	public async Task Streams_ShortReadingSource_RoundtripShouldPreserveData()
	{
		using var client = await LoginAsync(CancellationToken);
		var rootId = await GetRootIdAsync(client, CancellationToken);

		// Several full 16-byte blocks so a one-byte-at-a-time source forces many
		// partial reads inside a single MegaAesCtrStream block.
		var data = RandomGen.GetBytes(4096);
		var name = $"mega-shortread-{Guid.NewGuid():N}.bin";

		Node uploaded = null;

		try
		{
			using (var source = new OneByteAtATimeStream(data))
				uploaded = await client.UploadAsync(rootId, name, source, modificationDate: null, progress: null, CancellationToken);

			IsNotNull(uploaded);

			using var downloaded = new MemoryStream();
			await client.DownloadAsync(uploaded, downloaded, progress: null, CancellationToken);

			AreEqual(data, downloaded.ToArray());
		}
		finally
		{
			if (uploaded is not null)
				await client.DeleteAsync(uploaded.Id, CancellationToken);
		}
	}

	/// <summary>
	/// BUG: Crypto.FromEpochSeconds returns epoch.AddSeconds(seconds).ToLocalTime(),
	/// so Node.CreationDate / ModificationDate carry machine-local DateTime values
	/// (Kind=Local), breaking the repository-wide UTC convention and the symmetry
	/// with ToEpochSeconds (which normalizes via ToUniversalTime).
	/// (Backup.Mega\Native\Crypto.cs:336)
	/// Expected: the timestamp is UTC (Kind=Utc).
	/// Actual: the timestamp is local time (Kind=Local).
	/// </summary>
	[TestMethod]
	public async Task Node_CreationDate_ShouldBeUtc()
	{
		using var client = await LoginAsync(CancellationToken);
		var rootId = await GetRootIdAsync(client, CancellationToken);

		var data = RandomGen.GetBytes(64);
		var name = $"mega-utc-{Guid.NewGuid():N}.bin";

		Node uploaded = null;

		try
		{
			using (var source = new MemoryStream(data, writable: false))
				uploaded = await client.UploadAsync(rootId, name, source, modificationDate: null, progress: null, CancellationToken);

			// Re-read the node tree so the timestamp comes through ParseNodes
			// (ts -> FromEpochSeconds), which is where the bug lives.
			var nodes = await client.GetNodesAsync(CancellationToken);
			var node = nodes.First(n => n.Id == uploaded.Id);

			IsNotNull(node.CreationDate);
			AreEqual(DateTimeKind.Utc, node.CreationDate.Value.Kind);
		}
		finally
		{
			if (uploaded is not null)
				await client.DeleteAsync(uploaded.Id, CancellationToken);
		}
	}

	/// <summary>
	/// BUG: Crypto.SerializeToBytes shifts a long via arithmetic ">>= 8"; for a
	/// negative input x converges to -1 and never reaches 0, so the while loop
	/// overruns the 9-element buffer and throws IndexOutOfRangeException. This is
	/// reached through MegaAttributes.Create when Client.UploadAsync is given a
	/// modificationDate before 1970-01-01 (ToEpochSeconds() is negative).
	/// (Backup.Mega\Native\Crypto.cs:347)
	/// Expected: uploading with a pre-1970 modification date serializes the
	/// fingerprint without overrunning the buffer (no IndexOutOfRangeException).
	/// Actual: attribute creation throws IndexOutOfRangeException, aborting upload.
	/// </summary>
	[TestMethod]
	public async Task Upload_Pre1970ModificationDate_ShouldNotOverrunBuffer()
	{
		using var client = await LoginAsync(CancellationToken);
		var rootId = await GetRootIdAsync(client, CancellationToken);

		var data = RandomGen.GetBytes(64);
		var name = $"mega-pre1970-{Guid.NewGuid():N}.bin";

		// A modification date before the Unix epoch -> negative epoch seconds.
		var modificationDate = new DateTime(1960, 6, 15, 12, 0, 0, DateTimeKind.Utc);

		Node uploaded = null;

		try
		{
			using var source = new MemoryStream(data, writable: false);
			uploaded = await client.UploadAsync(rootId, name, source, modificationDate, progress: null, CancellationToken);

			IsNotNull(uploaded);
		}
		finally
		{
			if (uploaded is not null)
				await client.DeleteAsync(uploaded.Id, CancellationToken);
		}
	}
}
