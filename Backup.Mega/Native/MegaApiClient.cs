namespace Ecng.Backup.Mega.Native
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Security.Cryptography;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Ecng.Backup.Mega.Native.Cryptography;
	using Ecng.Common;
	using Medo.Security.Cryptography;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using Serialization;

	partial class MegaApiClient
	{
		private static readonly Uri s_baseApiUri = new Uri("https://g.api.mega.co.nz/cs");
		private static readonly Uri s_baseUri = new Uri("https://mega.nz");

		private readonly Options _options;
		private readonly IWebClient _webClient;

		private readonly object _apiRequestLocker = new object();

		private Node _trashNode;
		private string _sessionId;
		private byte[] _masterKey;
		private uint _sequenceIndex = (uint)(uint.MaxValue * new Random().NextDouble());
		private bool _authenticatedLogin;

		#region Constructors

		/// <summary>
		/// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options"/> and default <see cref="IWebClient"/>
		/// </summary>
		public MegaApiClient()
			: this(new Options(), new WebClient())
		{
		}

		/// <summary>
		/// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options" /> and default <see cref="IWebClient"/>
		/// </summary>
		public MegaApiClient(Options options)
			: this(options, new WebClient())
		{
		}

		/// <summary>
		/// Instantiate a new <see cref="MegaApiClient" /> object with default <see cref="Options" /> and custom <see cref="IWebClient"/>
		/// </summary>
		public MegaApiClient(IWebClient webClient)
			: this(new Options(), webClient)
		{
		}

		/// <summary>
		/// Instantiate a new <see cref="MegaApiClient" /> object with custom <see cref="Options"/> and custom <see cref="IWebClient" />
		/// </summary>
		public MegaApiClient(Options options, IWebClient webClient)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
			_webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
			_webClient.BufferSize = options.BufferSize;
		}

		#endregion

		#region Public API

		/// <summary>
		/// Generate authentication informations and store them in a serializable object to allow persistence
		/// </summary>
		/// <param name="email">email</param>
		/// <param name="password">password</param>
		/// <param name="mfaKey"></param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <returns><see cref="AuthInfos" /> object containing encrypted data</returns>
		/// <exception cref="ArgumentNullException">email or password is null</exception>
		private async Task<AuthInfos> GenerateAuthInfosAsync(string email, string password, string mfaKey, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(email))
			{
				throw new ArgumentNullException("email");
			}

			if (string.IsNullOrEmpty(password))
			{
				throw new ArgumentNullException("password");
			}

			// Prelogin to retrieve account version
			var preLoginRequest = new PreLoginRequest(email);
			var preLoginResponse = await Request<PreLoginResponse>(preLoginRequest, default, cancellationToken);

			if (preLoginResponse.Version == 2 && !string.IsNullOrEmpty(preLoginResponse.Salt))
			{
				// Mega uses a new way to hash password based on a salt sent by Mega during prelogin
				var saltBytes = preLoginResponse.Salt.FromBase64();
				var passwordBytes = password.ToBytesPassword();
				const int Iterations = 100000;

				var derivedKeyBytes = new byte[32];
				using (var hmac = new HMACSHA512())
				{
					var pbkdf2 = new Pbkdf2(hmac, passwordBytes, saltBytes, Iterations);
					derivedKeyBytes = pbkdf2.GetBytes(derivedKeyBytes.Length);
				}

				// Derived key contains master key (0-16) and password hash (16-32)
				if (!string.IsNullOrEmpty(mfaKey))
				{
					return new AuthInfos(
					  email,
					  derivedKeyBytes.Skip(16).ToArray().ToBase64(),
					  derivedKeyBytes.Take(16).ToArray(),
					  mfaKey);
				}

				return new AuthInfos(
				  email,
				  derivedKeyBytes.Skip(16).ToArray().ToBase64(),
				  derivedKeyBytes.Take(16).ToArray());
			}
			else if (preLoginResponse.Version == 1)
			{
				// Retrieve password as UTF8 byte array
				var passwordBytes = password.ToBytesPassword();

				// Encrypt password to use password as key for the hash
				var passwordAesKey = PrepareKey(passwordBytes);

				// Hash email and password to decrypt master key on Mega servers
				var hash = GenerateHash(email.ToLowerInvariant(), passwordAesKey);
				if (!string.IsNullOrEmpty(mfaKey))
				{
					return new AuthInfos(email, hash, passwordAesKey, mfaKey);
				}

				return new AuthInfos(email, hash, passwordAesKey);
			}
			else
			{
				throw new NotSupportedException("Version of account not supported");
			}
		}

		public event EventHandler<ApiRequestFailedEventArgs> ApiRequestFailed;

		public bool IsLoggedIn => _sessionId != null;

		/// <summary>
		/// Login to Mega.co.nz service using email/password credentials
		/// </summary>
		/// <param name="email">email</param>
		/// <param name="password">password</param>
		/// <param name="mfaKey"></param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="ApiException">Service is not available or credentials are invalid</exception>
		/// <exception cref="ArgumentNullException">email or password is null</exception>
		/// <exception cref="NotSupportedException">Already logged in</exception>
		public async Task<LogonSessionToken> LoginAsync(string email, string password, string mfaKey = null, CancellationToken cancellationToken = default)
		{
			EnsureLoggedOut();
			_authenticatedLogin = true;

			var authInfos = await GenerateAuthInfosAsync(email, password, mfaKey, cancellationToken);

			// Request Mega Api
			LoginRequest request;
			if (!string.IsNullOrEmpty(authInfos.MFAKey))
			{
				request = new LoginRequest(authInfos.Email, authInfos.Hash, authInfos.MFAKey);
			}
			else
			{
				request = new LoginRequest(authInfos.Email, authInfos.Hash);
			}

			var response = await Request<LoginResponse>(request, default, cancellationToken);

			// Decrypt master key using our password key
			var cryptedMasterKey = response.MasterKey.FromBase64();
			_masterKey = Crypto.DecryptKey(cryptedMasterKey, authInfos.PasswordAesKey);

			// Decrypt RSA private key using decrypted master key
			var cryptedRsaPrivateKey = response.PrivateKey.FromBase64();
			var rsaPrivateKeyComponents = Crypto.GetRsaPrivateKeyComponents(cryptedRsaPrivateKey, _masterKey);

			// Decrypt session id
			var encryptedSid = response.SessionId.FromBase64();
			var sid = Crypto.RsaDecrypt(encryptedSid.FromMPINumber(), rsaPrivateKeyComponents[0], rsaPrivateKeyComponents[1], rsaPrivateKeyComponents[2]);

			// Session id contains only the first 43 bytes
			_sessionId = sid.Take(43).ToArray().ToBase64();

			return new LogonSessionToken(_sessionId, _masterKey);
		}

		/// <summary>
		/// Login anonymously to Mega.co.nz service
		/// </summary>
		/// <exception cref="ApiException">Throws if service is not available</exception>
		public async Task LoginAnonymousAsync(CancellationToken cancellationToken = default)
		{
			EnsureLoggedOut();
			_authenticatedLogin = false;

			var random = new Random();

			// Generate random master key
			_masterKey = new byte[16];
			random.NextBytes(_masterKey);

			// Generate a random password used to encrypt the master key
			var passwordAesKey = new byte[16];
			random.NextBytes(passwordAesKey);

			// Generate a random session challenge
			var sessionChallenge = new byte[16];
			random.NextBytes(sessionChallenge);

			var encryptedMasterKey = Crypto.EncryptAes(_masterKey, passwordAesKey);

			// Encrypt the session challenge with our generated master key
			var encryptedSessionChallenge = Crypto.EncryptAes(sessionChallenge, _masterKey);
			var encryptedSession = new byte[32];
			Array.Copy(sessionChallenge, 0, encryptedSession, 0, 16);
			Array.Copy(encryptedSessionChallenge, 0, encryptedSession, 16, encryptedSessionChallenge.Length);

			// Request Mega Api to obtain a temporary user handle
			var request = new AnonymousLoginRequest(encryptedMasterKey.ToBase64(), encryptedSession.ToBase64());
			var userHandle = await Request(request, cancellationToken);

			// Request Mega Api to retrieve our temporary session id
			var request2 = new LoginRequest(userHandle, null);
			var response2 = await Request<LoginResponse>(request2, default, cancellationToken);

			_sessionId = response2.TemporarySessionId;
		}

		/// <summary>
		/// Logout from Mega.co.nz service
		/// </summary>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		public async Task LogoutAsync(CancellationToken cancellationToken = default)
		{
			EnsureLoggedIn();

			if (_authenticatedLogin == true)
			{
				await Request(new LogoutRequest(), cancellationToken);
			}

			// Reset values retrieved by Login methods
			_masterKey = null;
			_sessionId = null;
		}

		/// <summary>
		/// Retrieve recovery key
		/// </summary>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		public string GetRecoveryKey()
		{
			EnsureLoggedIn();

			if (!_authenticatedLogin)
			{
				throw new NotSupportedException("Anonymous login is not supported");
			}

			return _masterKey.ToBase64();
		}

		/// <summary>
		/// Retrieve account (quota) information
		/// </summary>
		/// <returns>An object containing account information</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		public async Task<IAccountInformation> GetAccountInformationAsync(CancellationToken cancellationToken = default)
		{
			EnsureLoggedIn();

			var request = new AccountInformationRequest();
			return await Request<AccountInformationResponse>(request, default, cancellationToken);
		}

		/// <summary>
		/// Retrieve session history
		/// </summary>
		/// <returns>A collection of sessions</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		public async Task<IEnumerable<ISession>> GetSessionsHistoryAsync(CancellationToken cancellationToken = default)
		{
			EnsureLoggedIn();

			var request = new SessionHistoryRequest();
			return await Request<SessionHistoryResponse>(request, default, cancellationToken);
		}

		/// <summary>
		/// Retrieve all filesystem nodes
		/// </summary>
		/// <returns>Flat representation of all the filesystem nodes</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		public async Task<IEnumerable<INode>> GetNodesAsync(CancellationToken cancellationToken = default)
		{
			EnsureLoggedIn();

			var request = new GetNodesRequest();
			var response = await Request<GetNodesResponse>(request, _masterKey, cancellationToken);

			var nodes = response.Nodes;
			if (_trashNode == null)
			{
				_trashNode = nodes.First(n => n.Type == NodeType.Trash);
			}

			return nodes.Distinct().OfType<INode>();
		}

		/// <summary>
		/// Retrieve children nodes of a parent node
		/// </summary>
		/// <returns>Flat representation of children nodes</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">Parent node is null</exception>
		public async Task<IEnumerable<INode>> GetNodesAsync(INode parent, CancellationToken cancellationToken = default)
		{
			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}

			return (await GetNodesAsync(cancellationToken)).Where(n => n.ParentId == parent.Id);
		}

		/// <summary>
		/// Delete a node from the filesytem
		/// </summary>
		/// <remarks>
		/// You can only delete <see cref="NodeType.Directory" /> or <see cref="NodeType.File" /> node
		/// </remarks>
		/// <param name="node">Node to delete</param>
		/// <param name="moveToTrash">Moved to trash if true, Permanently deleted if false</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">node is null</exception>
		/// <exception cref="ArgumentException">node is not a directory or a file</exception>
		public Task DeleteAsync(INode node, bool moveToTrash = true, CancellationToken cancellationToken = default)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}

			if (node.Type != NodeType.Directory && node.Type != NodeType.File)
			{
				throw new ArgumentException("Invalid node type");
			}

			EnsureLoggedIn();

			if (moveToTrash)
			{
				return MoveAsync(node, _trashNode, cancellationToken);
			}
			else
			{
				return Request(new DeleteRequest(node), cancellationToken);
			}
		}

		/// <summary>
		/// Create a folder on the filesytem
		/// </summary>
		/// <param name="name">Folder name</param>
		/// <param name="parent">Parent node to attach created folder</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">name or parent is null</exception>
		/// <exception cref="ArgumentException">parent is not valid (all types are allowed expect <see cref="NodeType.File" />)</exception>
		public async Task<INode> CreateFolderAsync(string name, INode parent, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}

			if (parent.Type == NodeType.File)
			{
				throw new ArgumentException("Invalid parent node");
			}

			EnsureLoggedIn();

			var key = Crypto.CreateAesKey();
			var attributes = Crypto.EncryptAttributes(new Attributes(name), key);
			var encryptedKey = Crypto.EncryptAes(key, _masterKey);

			var request = CreateNodeRequest.CreateFolderNodeRequest(parent, attributes.ToBase64(), encryptedKey.ToBase64(), key);
			var response = await Request<GetNodesResponse>(request, _masterKey, cancellationToken);
			return response.Nodes[0];
		}

		/// <summary>
		/// Retrieve an url to download specified node
		/// </summary>
		/// <param name="node">Node to retrieve the download link (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</param>
		/// <returns>Download link to retrieve the node with associated key</returns>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">node is null</exception>
		/// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> or <see cref="NodeType.Directory" /> can be downloaded)</exception>
		public async Task<Uri> GetDownloadLinkAsync(INode node, CancellationToken cancellationToken = default)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}

			if (node.Type != NodeType.File && node.Type != NodeType.Directory)
			{
				throw new ArgumentException("Invalid node");
			}

			EnsureLoggedIn();

			if (node.Type == NodeType.Directory)
			{
				// Request an export share on the node or we will receive an AccessDenied
				await Request(new ShareNodeRequest(node, _masterKey, await GetNodesAsync(cancellationToken)), cancellationToken);

				node = (await GetNodesAsync(cancellationToken)).First(x => x.Equals(node));
			}

			if (node is not INodeCrypto nodeCrypto)
			{
				throw new ArgumentException("node must implement INodeCrypto");
			}

			var request = new GetDownloadLinkRequest(node);
			var response = await Request<string>(request, default, cancellationToken);

			return new Uri(s_baseUri, string.Format(
				"/{0}/{1}#{2}",
				node.Type == NodeType.Directory ? "folder" : "file",
				response,
				node.Type == NodeType.Directory ? nodeCrypto.SharedKey.ToBase64() : nodeCrypto.FullKey.ToBase64()));
		}

		/// <summary>
		/// Retrieve a Stream to download and decrypt the specified node
		/// </summary>
		/// <param name="node">Node to download (only <see cref="NodeType.File" /> can be downloaded)</param>
		/// <param name="cancellationToken">CancellationToken used to cancel the action</param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">node or outputFile is null</exception>
		/// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
		/// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
		public async Task<Stream> Download(INode node, CancellationToken cancellationToken)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}

			if (node.Type != NodeType.File)
			{
				throw new ArgumentException("Invalid node");
			}

			if (!(node is INodeCrypto nodeCrypto))
			{
				throw new ArgumentException("node must implement INodeCrypto");
			}

			EnsureLoggedIn();

			// Retrieve download URL
			var downloadRequest = node is PublicNode publicNode && publicNode.ParentId == null ? (RequestBase)new DownloadUrlRequestFromId(node.Id) : new DownloadUrlRequest(node);
			var downloadResponse = await Request<DownloadUrlResponse>(downloadRequest, default, cancellationToken);

			Stream dataStream = await _webClient.GetRequestRaw(new Uri(downloadResponse.Url), cancellationToken);

			Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, nodeCrypto.Key, nodeCrypto.Iv, nodeCrypto.MetaMac);

			return resultStream;
		}

		/// <summary>
		/// Retrieve a Stream to download and decrypt the specified Uri
		/// </summary>
		/// <param name="uri">Uri to download</param>
		/// <param name="cancellationToken">CancellationToken used to cancel the action</param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">uri is null</exception>
		/// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
		/// <exception cref="DownloadException">Checksum is invalid. Downloaded data are corrupted</exception>
		public async Task<Stream> Download(Uri uri, CancellationToken cancellationToken)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}

			EnsureLoggedIn();

			uri.GetPartsFromUri(out var id, out var iv, out var metaMac, out var key);

			// Retrieve download URL
			var downloadRequest = new DownloadUrlRequestFromId(id);
			var downloadResponse = await Request<DownloadUrlResponse>(downloadRequest, default, cancellationToken);

			Stream dataStream = new BufferedStream(await _webClient.GetRequestRaw(new Uri(downloadResponse.Url), cancellationToken));

			Stream resultStream = new MegaAesCtrStreamDecrypter(dataStream, downloadResponse.Size, key, iv, metaMac);

			return resultStream;
		}

		/// <summary>
		/// Retrieve public properties of a file from a specified Uri
		/// </summary>
		/// <param name="uri">Uri to retrive properties</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">uri is null</exception>
		/// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
		public async Task<INode> GetNodeFromLinkAsync(Uri uri, CancellationToken cancellationToken = default)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}

			EnsureLoggedIn();

			uri.GetPartsFromUri(out var id, out var iv, out var metaMac, out var key);

			// Retrieve attributes
			var downloadRequest = new DownloadUrlRequestFromId(id);
			var downloadResponse = await Request<DownloadUrlResponse>(downloadRequest, default, cancellationToken);

			return new PublicNode(new Node(id, downloadResponse, key, iv, metaMac), null);
		}

		/// <summary>
		/// Retrieve list of nodes from a specified Uri
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">uri is null</exception>
		/// <exception cref="ArgumentException">Uri is not valid (id and key are required)</exception>
		public async Task<IEnumerable<INode>> GetNodesFromLinkAsync(Uri uri, CancellationToken cancellationToken = default)
		{
			if (uri == null)
			{
				throw new ArgumentNullException("uri");
			}

			EnsureLoggedIn();

			uri.GetPartsFromUri(out var shareId, out _, out _, out var key);

			// Retrieve attributes
			var getNodesRequest = new GetNodesRequest(shareId);
			var getNodesResponse = await Request<GetNodesResponse>(getNodesRequest, key, cancellationToken);

			return getNodesResponse.Nodes.Select(x => new PublicNode(x, shareId)).OfType<INode>();
		}

		/// <summary>
		/// Upload a stream on Mega.co.nz and attach created node to selected parent
		/// </summary>
		/// <param name="stream">Data to upload</param>
		/// <param name="name">Created node name</param>
		/// <param name="modificationDate">Custom modification date stored in the Node attributes</param>
		/// <param name="parent">Node to attach the uploaded file (all types except <see cref="NodeType.File" /> are supported)</param>
		/// <param name="cancellationToken">CancellationToken used to cancel the action</param>
		/// <returns>Created node</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">stream or name or parent is null</exception>
		/// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
		public async Task<INode> Upload(Stream stream, string name, INode parent, DateTime? modificationDate = null, CancellationToken cancellationToken = default)
		{
			if (stream == null)
			{
				throw new ArgumentNullException("stream");
			}

			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException("name");
			}

			if (parent == null)
			{
				throw new ArgumentNullException("parent");
			}

			if (parent.Type == NodeType.File)
			{
				throw new ArgumentException("Invalid parent node");
			}

			if (parent is PublicNode)
			{
				throw new ApiException(ApiResultCode.AccessDenied);
			}

			EnsureLoggedIn();

			var completionHandle = string.Empty;
			var attempt = 0;
			while (_options.ComputeApiRequestRetryWaitDelay(++attempt, out var retryDelay))
			{
				// Retrieve upload URL
				var uploadRequest = new UploadUrlRequest(stream.Length);
				var uploadResponse = await Request<UploadUrlResponse>(uploadRequest, default, cancellationToken);

				var apiResult = ApiResultCode.Ok;
				using (var encryptedStream = new MegaAesCtrStreamCrypter(stream))
				{
					long chunkStartPosition = 0;
					var chunksSizesToUpload = ComputeChunksSizesToUpload(encryptedStream.ChunksPositions, encryptedStream.Length).ToArray();
					Uri uri = null;
					for (var i = 0; i < chunksSizesToUpload.Length; i++)
					{
						completionHandle = string.Empty;

						var chunkSize = chunksSizesToUpload[i];
						var chunkBuffer = new byte[chunkSize];
						encryptedStream.Read(chunkBuffer, 0, chunkSize);

						using (var chunkStream = new MemoryStream(chunkBuffer))
						{
							uri = new Uri(uploadResponse.Url + "/" + chunkStartPosition);
							chunkStartPosition += chunkSize;
							try
							{
								completionHandle = await _webClient.PostRequestRaw(uri, chunkStream, cancellationToken);
								if (string.IsNullOrEmpty(completionHandle))
								{
									apiResult = ApiResultCode.Ok;
									continue;
								}

								if (completionHandle.FromBase64().Length != 27 && long.TryParse(completionHandle, out var retCode))
								{
									apiResult = (ApiResultCode)retCode;
									break;
								}
							}
							catch (Exception ex)
							{
								apiResult = ApiResultCode.RequestFailedRetry;
								ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiResult, ex));

								break;
							}
						}
					}

					if (apiResult != ApiResultCode.Ok)
					{
						ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiResult, completionHandle));

						if (apiResult == ApiResultCode.RequestFailedRetry || apiResult == ApiResultCode.RequestFailedPermanetly || apiResult == ApiResultCode.TooManyRequests)
						{
							// Restart upload from the beginning
							await retryDelay.Delay();

							// Reset steam position
							stream.Seek(0, SeekOrigin.Begin);

							continue;
						}

						throw new ApiException(apiResult);
					}

					// Encrypt attributes
					var cryptedAttributes = Crypto.EncryptAttributes(new Attributes(name, stream, modificationDate), encryptedStream.FileKey);

					// Compute the file key
					var fileKey = new byte[32];
					for (var i = 0; i < 8; i++)
					{
						fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.Iv[i]);
						fileKey[i + 16] = encryptedStream.Iv[i];
					}

					for (var i = 8; i < 16; i++)
					{
						fileKey[i] = (byte)(encryptedStream.FileKey[i] ^ encryptedStream.MetaMac[i - 8]);
						fileKey[i + 16] = encryptedStream.MetaMac[i - 8];
					}

					var encryptedKey = Crypto.EncryptKey(fileKey, _masterKey);

					var createNodeRequest = CreateNodeRequest.CreateFileNodeRequest(parent, cryptedAttributes.ToBase64(), encryptedKey.ToBase64(), fileKey, completionHandle);
					var createNodeResponse = await Request<GetNodesResponse>(createNodeRequest, _masterKey, cancellationToken);
					return createNodeResponse.Nodes[0];
				}
			}

			throw new UploadException(completionHandle);
		}

		/// <summary>
		/// Change node parent
		/// </summary>
		/// <param name="sourceNode">Node to move</param>
		/// <param name="destinationParentNode">New parent</param>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <returns>Moved node</returns>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">node or destinationParentNode is null</exception>
		/// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.Directory" /> and <see cref="NodeType.File" /> are supported)</exception>
		/// <exception cref="ArgumentException">parent is not valid (all types except <see cref="NodeType.File" /> are supported)</exception>
		public async Task<INode> MoveAsync(INode sourceNode, INode destinationParentNode, CancellationToken cancellationToken = default)
		{
			if (sourceNode == null)
			{
				throw new ArgumentNullException("node");
			}

			if (destinationParentNode == null)
			{
				throw new ArgumentNullException("destinationParentNode");
			}

			if (sourceNode.Type != NodeType.Directory && sourceNode.Type != NodeType.File)
			{
				throw new ArgumentException("Invalid node type");
			}

			if (destinationParentNode.Type == NodeType.File)
			{
				throw new ArgumentException("Invalid destination parent node");
			}

			EnsureLoggedIn();

			await Request(new MoveRequest(sourceNode, destinationParentNode), cancellationToken);
			return (await GetNodesAsync(cancellationToken)).First(n => n.Equals(sourceNode));
		}

		public async Task<INode> RenameAsync(INode sourceNode, string newName, CancellationToken cancellationToken = default)
		{
			if (sourceNode == null)
			{
				throw new ArgumentNullException("node");
			}

			if (sourceNode.Type != NodeType.Directory && sourceNode.Type != NodeType.File)
			{
				throw new ArgumentException("Invalid node type");
			}

			if (string.IsNullOrEmpty(newName))
			{
				throw new ArgumentNullException("newName");
			}

			if (!(sourceNode is INodeCrypto nodeCrypto))
			{
				throw new ArgumentException("node must implement INodeCrypto");
			}

			EnsureLoggedIn();

			var encryptedAttributes = Crypto.EncryptAttributes(new Attributes(newName, ((Node)sourceNode).Attributes), nodeCrypto.Key);
			await Request(new RenameRequest(sourceNode, encryptedAttributes.ToBase64()), cancellationToken);
			return (await GetNodesAsync(cancellationToken)).First(n => n.Equals(sourceNode));
		}

		/// <summary>
		/// Download thumbnail from file attributes (or return null if thumbnail is not available)
		/// </summary>
		/// <param name="node">Node to download the thumbnail from (only <see cref="NodeType.File" /> can be downloaded)</param>
		/// <param name="fileAttributeType">File attribute type to retrieve</param>
		/// <param name="cancellationToken">CancellationToken used to cancel the action</param>
		/// <exception cref="NotSupportedException">Not logged in</exception>
		/// <exception cref="ApiException">Mega.co.nz service reports an error</exception>
		/// <exception cref="ArgumentNullException">node or outputFile is null</exception>
		/// <exception cref="ArgumentException">node is not valid (only <see cref="NodeType.File" /> can be downloaded)</exception>
		/// <exception cref="InvalidOperationException">file attribute data is invalid</exception>
		public async Task<Stream> DownloadFileAttributeAsync(INode node, FileAttributeType fileAttributeType, CancellationToken cancellationToken = default)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			if (node.Type != NodeType.File)
			{
				throw new ArgumentException("Invalid node");
			}

			if (!(node is INodeCrypto nodeCrypto))
			{
				throw new ArgumentException("node must implement INodeCrypto");
			}

			EnsureLoggedIn();

			var fileAttribute = node.FileAttributes.FirstOrDefault(_ => _.Type == fileAttributeType);
			if (fileAttribute == null)
			{
				return null;
			}

			var downloadRequest = new DownloadFileAttributeRequest(fileAttribute.Handle);
			var downloadResponse = await Request<DownloadFileAttributeResponse>(downloadRequest, default, cancellationToken);

			var fileAttributeHandle = fileAttribute.Handle.FromBase64();
			using (var stream = await _webClient.PostRequestRawAsStream(new Uri(downloadResponse.Url + "/0"), new MemoryStream(fileAttributeHandle), cancellationToken))
			{
				using (var memoryStream = new MemoryStream())
				{
					stream.CopyTo(memoryStream);
					memoryStream.Position = 0;

					const int DataOffset = 12; // handle (8) + position (4)
					var data = memoryStream.ToArray();
					var dataHandle = data.CopySubArray(8, 0);
					if (!dataHandle.SequenceEqual(fileAttributeHandle))
					{
						throw new InvalidOperationException($"File attribute handle mismatch ({fileAttribute.Handle} requested but {dataHandle.ToBase64()} received)");
					}

					var dataSize = BitConverter.ToUInt32(data.CopySubArray(4, 8), 0);
					if (dataSize != data.Length - DataOffset)
					{
						throw new InvalidOperationException($"File attribute size mismatch ({dataSize} expected but {data.Length - DataOffset} received)");
					}

					data = data.CopySubArray(data.Length - DataOffset, DataOffset);

					return Crypto.DecryptAes(data, nodeCrypto.Key).To<Stream>();
				}
			}
		}

		#endregion

		#region Private static methods

		private static string GenerateHash(string email, byte[] passwordAesKey)
		{
			var emailBytes = email.ToBytes();
			var hash = new byte[16];

			// Compute email in 16 bytes array
			for (var i = 0; i < emailBytes.Length; i++)
			{
				hash[i % 16] ^= emailBytes[i];
			}

			// Encrypt hash using password key
			using (var encryptor = Crypto.CreateAesEncryptor(passwordAesKey))
			{
				for (var it = 0; it < 16384; it++)
				{
					hash = Crypto.EncryptAes(hash, encryptor);
				}
			}

			// Retrieve bytes 0-4 and 8-12 from the hash
			var result = new byte[8];
			Array.Copy(hash, 0, result, 0, 4);
			Array.Copy(hash, 8, result, 4, 4);

			return result.ToBase64();
		}

		private static byte[] PrepareKey(byte[] data)
		{
			var pkey = new byte[] { 0x93, 0xC4, 0x67, 0xE3, 0x7D, 0xB0, 0xC7, 0xA4, 0xD1, 0xBE, 0x3F, 0x81, 0x01, 0x52, 0xCB, 0x56 };

			for (var it = 0; it < 65536; it++)
			{
				for (var idx = 0; idx < data.Length; idx += 16)
				{
					// Pad the data to 16 bytes blocks
					var key = data.CopySubArray(16, idx);

					pkey = Crypto.EncryptAes(pkey, key);
				}
			}

			return pkey;
		}

		#endregion

		#region Web

		private Task<string> Request(RequestBase request, CancellationToken cancellationToken)
		{
			return Request<string>(request, default, cancellationToken);
		}

		private Task<TResponse> Request<TResponse>(RequestBase request, byte[] key, CancellationToken cancellationToken)
			where TResponse : class
		{
			if (_options.SynchronizeApiRequests)
			{
				lock (_apiRequestLocker)
				{
					return RequestCore<TResponse>(request, key, cancellationToken);
				}
			}
			else
			{
				return RequestCore<TResponse>(request, key, cancellationToken);
			}
		}

		private async Task<TResponse> RequestCore<TResponse>(RequestBase request, byte[] key, CancellationToken cancellationToken)
			where TResponse : class
		{
			var dataRequest = JsonConvert.SerializeObject(new object[] { request });
			var uri = GenerateUrl(request.QueryArguments);
			object jsonData = null;
			var attempt = 0;
			while (_options.ComputeApiRequestRetryWaitDelay(++attempt, out var retryDelay))
			{
				var dataResult = await _webClient.PostRequestJson(uri, dataRequest, cancellationToken);

				if (string.IsNullOrEmpty(dataResult)
				  || (jsonData = JsonConvert.DeserializeObject(dataResult)) == null
				  || jsonData is long
				  || jsonData is JArray array && array[0].Type == JTokenType.Integer)
				{
					var apiCode = jsonData == null
					  ? ApiResultCode.RequestFailedRetry
					  : jsonData is long
						? (ApiResultCode)Enum.ToObject(typeof(ApiResultCode), jsonData)
						: (ApiResultCode)((JArray)jsonData)[0].Value<int>();

					if (apiCode != ApiResultCode.Ok)
					{
						ApiRequestFailed?.Invoke(this, new ApiRequestFailedEventArgs(uri, attempt, retryDelay, apiCode, dataResult));
					}

					if (apiCode == ApiResultCode.RequestFailedRetry)
					{
						await retryDelay.Delay(cancellationToken);
						continue;
					}

					if (apiCode != ApiResultCode.Ok)
					{
						throw new ApiException(apiCode);
					}
				}

				break;
			}

			var data = ((JArray)jsonData)[0].ToString();
			return (typeof(TResponse) == typeof(string)) ? data as TResponse : JsonConvert.DeserializeObject<TResponse>(data, new GetNodesResponseConverter(key));
		}

		private Uri GenerateUrl(Dictionary<string, string> queryArguments)
		{
			var query = new Dictionary<string, string>(queryArguments)
			{
				["id"] = (_sequenceIndex++ % uint.MaxValue).ToString(CultureInfo.InvariantCulture),
				["ak"] = _options.ApplicationKey
			};

			if (!string.IsNullOrEmpty(_sessionId))
			{
				query["sid"] = _sessionId;
			}

			var builder = new UriBuilder(s_baseApiUri);
			var arguments = "";
			foreach (var item in query)
			{
				arguments = arguments + item.Key + "=" + item.Value + "&";
			}

			arguments = arguments.Substring(0, arguments.Length - 1);

			builder.Query = arguments;
			return builder.Uri;
		}

		#endregion

		#region Private methods

		private void EnsureLoggedIn()
		{
			if (_sessionId == null)
			{
				throw new NotSupportedException("Not logged in");
			}
		}

		private void EnsureLoggedOut()
		{
			if (_sessionId != null)
			{
				throw new NotSupportedException("Already logged in");
			}
		}

		private IEnumerable<int> ComputeChunksSizesToUpload(long[] chunksPositions, long streamLength)
		{
			for (var i = 0; i < chunksPositions.Length; i++)
			{
				var currentChunkPosition = chunksPositions[i];
				var nextChunkPosition = i == chunksPositions.Length - 1
				  ? streamLength
				  : chunksPositions[i + 1];

				// Pack multiple chunks in a single upload
				while (((int)(nextChunkPosition - currentChunkPosition) < _options.ChunksPackSize || _options.ChunksPackSize == -1) && i < chunksPositions.Length - 1)
				{
					i++;
					nextChunkPosition = i == chunksPositions.Length - 1
					  ? streamLength
					  : chunksPositions[i + 1];
				}

				yield return (int)(nextChunkPosition - currentChunkPosition);
			}
		}

		#endregion

		#region AuthInfos

		public class AuthInfos
		{
			public AuthInfos(string email, string hash, byte[] passwordAesKey, string mfaKey = null)
			{
				Email = email;
				Hash = hash;
				PasswordAesKey = passwordAesKey;
				MFAKey = mfaKey;
			}

			[JsonProperty]
			public string Email { get; private set; }

			[JsonProperty]
			public string Hash { get; private set; }

			[JsonProperty]
			public byte[] PasswordAesKey { get; private set; }

			[JsonProperty]
			public string MFAKey { get; private set; }
		}

		public class LogonSessionToken : IEquatable<LogonSessionToken>
		{
			[JsonProperty]
			public string SessionId { get; }

			[JsonProperty]
			public byte[] MasterKey { get; }

			private LogonSessionToken()
			{
			}

			public LogonSessionToken(string sessionId, byte[] masterKey)
			{
				SessionId = sessionId;
				MasterKey = masterKey;
			}

			public override int GetHashCode() => SessionId.GetHashCode() * 23 + (MasterKey?.GetHashCode() ?? 0);

			public override bool Equals(object obj) => Equals(obj as LogonSessionToken);

			public bool Equals(LogonSessionToken other)
			{
				if (other == null)
				{
					return false;
				}

				if (SessionId == null || other.SessionId == null || string.CompareOrdinal(SessionId, other.SessionId) != 0)
				{
					return false;
				}

				if (MasterKey == null || other.MasterKey == null || !Enumerable.SequenceEqual(MasterKey, other.MasterKey))
				{
					return false;
				}

				return true;
			}
		}

		#endregion

	}
}