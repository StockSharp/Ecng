namespace Ecng.Backup.Mega.Native;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Minimal MEGA client implementation used by <c>Ecng.Backup</c>.
/// </summary>
public sealed class Client : IDisposable
{
	private const string _applicationKey = "axhQiYyQ";
	private static readonly Uri _apiUri = new("https://g.api.mega.co.nz/cs");

	private readonly HttpClient _http;
	private readonly bool _disposeHttp;

	private uint _sequence = (uint)(uint.MaxValue * new Random().NextDouble());
	private string _sessionId;
	private byte[] _masterKey;

	/// <summary>
	/// Creates a new client instance.
	/// </summary>
	public Client(HttpClient httpClient = null)
	{
		if (httpClient is null)
		{
			var handler = new SocketsHttpHandler
			{
				AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
				ConnectCallback = ConnectWithRetryAsync,
			};

			_http = new HttpClient(handler)
			{
				Timeout = TimeSpan.FromMinutes(2),
			};
			_http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(nameof(Client), "1"));
			_disposeHttp = true;
		}
		else
		{
			_http = httpClient;
			_disposeHttp = false;
		}
	}

	private static async ValueTask<Stream> ConnectWithRetryAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
	{
		var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host).ConfigureAwait(false);
		Exception lastError = null;

		foreach (var address in addresses)
		{
			var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				attemptCts.CancelAfter(TimeSpan.FromSeconds(5));

				await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), attemptCts.Token).ConfigureAwait(false);
				return new NetworkStream(socket, ownsSocket: true);
			}
			catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
			{
				lastError = ex;
				socket.Dispose();
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				lastError = ex;
				socket.Dispose();
			}
		}

		throw new HttpRequestException($"Unable to connect to {context.DnsEndPoint.Host}:{context.DnsEndPoint.Port}.", lastError);
	}

	/// <summary>
	/// Whether the client has an active session.
	/// </summary>
	public bool IsLoggedIn => _sessionId is not null;

	/// <summary>
	/// Disposes the client.
	/// </summary>
	public void Dispose()
	{
		_sessionId = null;
		_masterKey = null;

		if (_disposeHttp)
			_http.Dispose();
	}

	/// <summary>
	/// Logs in using email and password.
	/// </summary>
	public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
	{
		if (email.IsEmpty())
			throw new ArgumentNullException(nameof(email));
		if (password.IsEmpty())
			throw new ArgumentNullException(nameof(password));
		if (IsLoggedIn)
			throw new InvalidOperationException("Already logged in.");

		var pre = await RequestAsync<PreLoginResponse>(new PreLoginRequest { User = email }, hashcash: null, cancellationToken).ConfigureAwait(false);
		if (pre.Version != 1)
			throw new NotSupportedException($"Unsupported account version {pre.Version}.");

		var passwordKey = Crypto.PrepareKey(password.ToBytesPassword());
		var hash = Crypto.GenerateHash(email.ToLowerInvariant(), passwordKey);

		var login = await RequestAsync<LoginResponse>(new LoginRequest { User = email, PasswordHash = hash }, hashcash: null, cancellationToken).ConfigureAwait(false);

		var masterKeyEnc = login.MasterKey.FromBase64Url();
		var masterKey = Crypto.DecryptKey(masterKeyEnc, passwordKey);

		var rsaPriv = Crypto.GetRsaPrivateKeyComponents(login.PrivateKey.FromBase64Url(), masterKey);
		var sessionMpi = Crypto.FromMpiNumber(login.SessionId.FromBase64Url());
		var sessionBytes = Crypto.RsaDecrypt(sessionMpi, rsaPriv[0], rsaPriv[1], rsaPriv[2]);

		_sessionId = sessionBytes.Take(43).ToArray().ToBase64Url();
		_masterKey = masterKey;
	}

	/// <summary>
	/// Logs out and clears the current session.
	/// </summary>
	public async Task LogoutAsync(CancellationToken cancellationToken = default)
	{
		if (!IsLoggedIn)
			return;

		try
		{
			try
			{
				await RequestAsync<string>(new LogoutRequest(), hashcash: null, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				// Best-effort server logout. The local session is still cleared below.
			}
		}
		finally
		{
			_sessionId = null;
			_masterKey = null;
		}
	}

	/// <summary>
	/// Retrieves the node tree (files/folders) for the current account.
	/// </summary>
	public async Task<IReadOnlyList<Node>> GetNodesAsync(CancellationToken cancellationToken = default)
	{
		EnsureLoggedIn();
		var root = await RequestElementAsync(new GetNodesRequest(), hashcash: null, cancellationToken).ConfigureAwait(false);
		return ParseNodes(root);
	}

	/// <summary>
	/// Creates a folder under the given parent node id.
	/// </summary>
	public async Task<Node> CreateFolderAsync(string parentId, string name, CancellationToken cancellationToken = default)
	{
		EnsureLoggedIn();

		var folderKey = Crypto.CreateAesKey();
		var attrs = Crypto.EncryptAttributes(new MegaAttributes { Name = name }, folderKey).ToBase64Url();
		var encKey = Crypto.EncryptAes(folderKey, _masterKey).ToBase64Url();

		var req = new CreateNodeRequest
		{
			ParentId = parentId,
			Nodes =
			[
				new CreateNodeData
				{
					CompletionHandle = "xxxxxxxx",
					Type = (int)NodeType.Directory,
					Attributes = attrs,
					Key = encKey,
				}
			]
		};

		var resp = await RequestElementAsync(req, hashcash: null, cancellationToken).ConfigureAwait(false);
		return ParseNodes(resp).Single();
	}

	/// <summary>
	/// Uploads a stream as a file under the given parent node id.
	/// </summary>
	public async Task<Node> UploadAsync(string parentId, string name, Stream stream, DateTime? modificationDate, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(stream);
		EnsureLoggedIn();

		var uploadUrl = await RequestAsync<UploadUrlResponse>(new UploadUrlRequest { Size = stream.Length }, hashcash: null, cancellationToken).ConfigureAwait(false);

		string completionHandle = null;

		using (var crypter = new MegaAesCtrStreamCrypter(stream))
		{
			var total = stream.Length;
			var sent = 0L;

			foreach (var chunkSize in ComputeChunkSizes(crypter.ChunksPositions, total))
			{
				cancellationToken.ThrowIfCancellationRequested();

				var buffer = new byte[chunkSize];
				var read = 0;
				while (read < chunkSize)
				{
					var r = crypter.Read(buffer, read, chunkSize - read);
					if (r == 0)
						break;
					read += r;
				}

				using var ms = new MemoryStream(buffer, 0, read, writable: false);
				var url = new Uri(uploadUrl.Url + "/" + sent);
				sent += read;

				completionHandle = await PostRawAsync(url, ms, cancellationToken).ConfigureAwait(false);
				progress?.Report((double)sent / total * 100d);
			}

			if (completionHandle.IsEmpty())
				throw new InvalidOperationException("Upload did not return completion handle.");

			var attrs = MegaAttributes.Create(name, stream, modificationDate);
			var encAttrs = Crypto.EncryptAttributes(attrs, crypter.FileKeyBytes).ToBase64Url();

			var fileKey = new byte[32];
			for (var i = 0; i < 8; i++)
			{
				fileKey[i] = (byte)(crypter.FileKeyBytes[i] ^ crypter.IvBytes[i]);
				fileKey[i + 16] = crypter.IvBytes[i];
			}
			for (var i = 8; i < 16; i++)
			{
				fileKey[i] = (byte)(crypter.FileKeyBytes[i] ^ crypter.ComputedMetaMac[i - 8]);
				fileKey[i + 16] = crypter.ComputedMetaMac[i - 8];
			}

			var encKey = Crypto.EncryptKey(fileKey, _masterKey).ToBase64Url();

			var req = new CreateNodeRequest
			{
				ParentId = parentId,
				Nodes =
				[
					new CreateNodeData
					{
						CompletionHandle = completionHandle,
						Type = (int)NodeType.File,
						Attributes = encAttrs,
						Key = encKey,
					}
				]
			};

			var resp = await RequestElementAsync(req, hashcash: null, cancellationToken).ConfigureAwait(false);
			return ParseNodes(resp).Single();
		}
	}

	/// <summary>
	/// Creates a public link (export) for a node and returns the URL.
	/// </summary>
	public async Task<string> PublishAsync(Node node, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(node);
		EnsureLoggedIn();

		if (node.Type != NodeType.File && node.Type != NodeType.Directory)
			throw new ArgumentException("Only file and directory nodes can be published.", nameof(node));

		if (node.NodeKey is null || node.NodeKey.Length == 0)
			throw new InvalidOperationException("Node key is missing.");

		var el = await RequestElementAsync(new ExportLinkRequest { NodeId = node.Id }, hashcash: null, cancellationToken).ConfigureAwait(false);

		string publicHandle;

		if (el.ValueKind == JsonValueKind.String)
			publicHandle = el.GetString();
		else if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("ph", out var ph))
			publicHandle = ph.GetString();
		else
			publicHandle = null;

		if (publicHandle.IsEmpty())
			throw new InvalidOperationException($"Export link response is missing public handle: {TrimForError(el.GetRawText())}");

		var key = node.NodeKey.ToBase64Url();
		var kind = node.Type == NodeType.Directory ? "folder" : "file";
		return $"https://mega.nz/{kind}/{publicHandle}#{key}";
	}

	/// <summary>
	/// Removes a public link (export) for a node.
	/// </summary>
	public async Task UnpublishAsync(string nodeId, CancellationToken cancellationToken = default)
	{
		if (nodeId.IsEmpty())
			throw new ArgumentNullException(nameof(nodeId));

		EnsureLoggedIn();
		await RequestAsync<string>(new ExportLinkRequest { NodeId = nodeId, Disable = 1 }, hashcash: null, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Downloads a file node into the destination stream.
	/// </summary>
	public async Task DownloadAsync(Node node, Stream destination, IProgress<double> progress, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(node);
		ArgumentNullException.ThrowIfNull(destination);
		EnsureLoggedIn();

		if (node.Type != NodeType.File)
			throw new ArgumentException("Only file nodes can be downloaded.", nameof(node));

		var dl = await RequestAsync<DownloadUrlResponse>(new DownloadUrlRequest { Id = node.Id }, hashcash: null, cancellationToken).ConfigureAwait(false);

		using var raw = await _http.GetStreamAsync(new Uri(dl.Url), cancellationToken).ConfigureAwait(false);
		using var decrypt = new MegaAesCtrStreamDecrypter(raw, dl.Size, node.FileKey, node.Iv, node.MetaMac);

		var buffer = new byte[64 * 1024];
		long readTotal = 0;

		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var r = decrypt.Read(buffer, 0, buffer.Length);
			if (r == 0)
				break;

			await destination.WriteAsync(buffer.AsMemory(0, r), cancellationToken).ConfigureAwait(false);
			readTotal += r;
			progress?.Report((double)readTotal / dl.Size * 100d);
		}
	}

	/// <summary>
	/// Resolves a public file handle (export link) to a direct download URL.
	/// </summary>
	public Task<DownloadUrlResponse> GetPublicDownloadUrlAsync(string publicHandle, CancellationToken cancellationToken = default)
	{
		if (publicHandle.IsEmpty())
			throw new ArgumentNullException(nameof(publicHandle));

		return RequestAsync<DownloadUrlResponse>(new DownloadUrlRequest { PublicHandle = publicHandle }, hashcash: null, cancellationToken);
	}

	/// <summary>
	/// Deletes a node by its id.
	/// </summary>
	public async Task DeleteAsync(string nodeId, CancellationToken cancellationToken = default)
	{
		EnsureLoggedIn();
		await RequestAsync<string>(new DeleteRequest { NodeId = nodeId }, hashcash: null, cancellationToken).ConfigureAwait(false);
	}

	private void EnsureLoggedIn()
	{
		if (!IsLoggedIn)
			throw new InvalidOperationException("Not logged in.");
	}

	private async Task<string> PostRawAsync(Uri url, Stream data, CancellationToken cancellationToken)
	{
		using var content = new StreamContent(data);
		content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

		using var resp = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
		resp.EnsureSuccessStatusCode();
		return (await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Trim();
	}

	private Uri BuildApiUrl(string requestId, Dictionary<string, string> extraQuery)
	{
		var q = new Dictionary<string, string>(extraQuery ?? new())
		{
			["id"] = requestId,
			["ak"] = _applicationKey,
		};

		if (_sessionId is not null)
			q["sid"] = _sessionId;

		var sb = new StringBuilder();
		foreach (var kv in q)
			sb.Append(kv.Key).Append('=').Append(kv.Value).Append('&');

		sb.Length--;

		return new UriBuilder(_apiUri) { Query = sb.ToString() }.Uri;
	}

	private async Task<JsonElement> RequestElementAsync(MegaRequest request, string hashcash, CancellationToken cancellationToken)
	{
		var requestId = (_sequence++ % uint.MaxValue).ToString();
		request.RequestId = requestId;

		var payload = JsonSerializer.Serialize(new object[] { request });
		var attempt = 0;

		while (true)
		{
			var url = BuildApiUrl(requestId, hashcash is null ? null : new Dictionary<string, string> { ["hashcash"] = hashcash });

			try
			{
				using var content = new StringContent(payload, Encoding.UTF8, "application/json");

				using var resp = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
				resp.EnsureSuccessStatusCode();

				var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

				if (text.IsEmpty())
					throw new InvalidOperationException("Empty API response.");

				using var doc = JsonDocument.Parse(text);
				var root = doc.RootElement;

				if (root.ValueKind == JsonValueKind.Number)
				{
					var code = root.GetInt32();
					if (code == 0)
						return root.Clone();

					throw new InvalidOperationException($"Mega API error: {code}.");
				}

				if (root.ValueKind == JsonValueKind.Object)
					return root.Clone();

				if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
					throw new InvalidOperationException($"Unexpected API response shape: {TrimForError(text)}");

				var first = root[0];

				if (first.ValueKind == JsonValueKind.Number)
				{
					var code = first.GetInt32();

					if (code == 0)
						return first.Clone();

					// Hashcash required: [-27, "<challenge>"]
					if (code == -27 && root.GetArrayLength() >= 2)
					{
						var challenge = root[1].GetString();
						hashcash = Crypto.GenerateHashcashToken(challenge);
						attempt = 0;
						continue;
					}

					throw new InvalidOperationException($"Mega API error: {code}.");
				}

				return first.Clone();
			}
			catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException)
			{
				attempt++;

				if (attempt >= 3)
					throw;

				await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
			}
		}
	}

	private static string TrimForError(string value)
	{
		if (value is null)
			return "<null>";

		value = value.Replace("\r", string.Empty).Replace("\n", string.Empty);
		return value.Length <= 512 ? value : value.Substring(0, 512) + "...";
	}

	private async Task<T> RequestAsync<T>(MegaRequest request, string hashcash, CancellationToken cancellationToken)
	{
		var el = await RequestElementAsync(request, hashcash, cancellationToken).ConfigureAwait(false);

		if (typeof(T) == typeof(string))
			return (T)(object)el.GetRawText().Trim('"');

		return JsonSerializer.Deserialize<T>(el.GetRawText());
	}

	private IReadOnlyList<Node> ParseNodes(JsonElement response)
	{
		// Response contains { "f": [...], "ok": [...] }
		if (!response.TryGetProperty("f", out var f) || f.ValueKind != JsonValueKind.Array)
			throw new InvalidOperationException("Missing nodes array.");

		var sharedKeys = new Dictionary<string, string>(StringComparer.Ordinal);

		if (response.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.Array)
		{
			foreach (var item in ok.EnumerateArray())
			{
				var id = item.GetProperty("h").GetString();
				var key = item.GetProperty("k").GetString();
				if (!id.IsEmpty() && !key.IsEmpty())
					sharedKeys[id] = key;
			}
		}

		var nodes = new List<Node>();

		foreach (var n in f.EnumerateArray())
		{
			var type = (NodeType)n.GetProperty("t").GetInt32();

			if (type != NodeType.File && type != NodeType.Directory && type != NodeType.Root && type != NodeType.Trash && type != NodeType.Inbox)
				continue;

			var id = n.GetProperty("h").GetString();
			if (id.IsEmpty())
				continue;

			var parentId = n.TryGetProperty("p", out var p) ? p.GetString() : null;
			var size = n.TryGetProperty("s", out var s) ? s.GetInt64() : 0L;
			var ts = n.TryGetProperty("ts", out var tsv) ? tsv.GetInt64() : (long?)null;
			var publicHandle = n.TryGetProperty("ph", out var ph) ? ph.GetString() : null;

			string name = null;
			DateTime? mod = null;
			byte[] fileKey = null;
			byte[] iv = null;
			byte[] metaMac = null;
			byte[] nodeKey = null;

			if (type == NodeType.File || type == NodeType.Directory)
			{
				if (!n.TryGetProperty("k", out var kprop))
					continue;

				var keyText = kprop.GetString();
				if (keyText.IsEmpty())
					continue;

				var keyPart = keyText.Split('/')[0];
				var colon = keyPart.IndexOf(':');
				if (colon <= 0)
					continue;

				var handle = keyPart.Substring(0, colon);
				var encKey = keyPart.Substring(colon + 1).FromBase64Url();

				var nodeMasterKey = _masterKey;

				if (sharedKeys.TryGetValue(handle, out var shared))
				{
					nodeMasterKey = Crypto.DecryptKey(shared.FromBase64Url(), _masterKey);
				}

				var fullKey = Crypto.DecryptKey(encKey, nodeMasterKey);
				nodeKey = fullKey;

				if (type == NodeType.File)
					Crypto.GetPartsFromDecryptedKey(fullKey, out iv, out metaMac, out fileKey);

				if (n.TryGetProperty("a", out var aprop))
				{
					var attrsEnc = aprop.GetString();
					if (!attrsEnc.IsEmpty())
					{
						var attrs = Crypto.DecryptAttributes(attrsEnc.FromBase64Url(), type == NodeType.File ? fileKey : fullKey);
						attrs.HydrateAfterDeserialize();
						name = attrs.Name;
						mod = attrs.ModificationDate;
					}
				}
			}

			nodes.Add(new Node
			{
				Id = id,
				Type = type,
				ParentId = parentId,
				Name = name,
				PublicHandle = publicHandle,
				Size = size,
				CreationDate = ts is long l ? l.FromEpochSeconds() : null,
				ModificationDate = mod,
				FileKey = fileKey,
				Iv = iv,
				MetaMac = metaMac,
				NodeKey = nodeKey,
			});
		}

		return nodes;
	}

	private static IEnumerable<int> ComputeChunkSizes(long[] chunkPositions, long streamLength)
	{
		for (var i = 0; i < chunkPositions.Length; i++)
		{
			var start = chunkPositions[i];
			var end = (i == chunkPositions.Length - 1) ? streamLength : chunkPositions[i + 1];
			yield return checked((int)(end - start));
		}
	}
}
