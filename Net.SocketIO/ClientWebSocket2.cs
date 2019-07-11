namespace Ecng.Net
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.WebSockets;
	using System.Security.Cryptography;
	using System.Security.Cryptography.X509Certificates;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	public class ClientWebSocketOptions2
	{
		private bool _isReadOnly;
		private TimeSpan _keepAliveInterval;
		private ArraySegment<byte>? _buffer;
		private bool _useDefaultCredentials;
		private ICredentials _credentials;
		private IWebProxy _proxy;
		private CookieContainer _cookies;

		public ClientWebSocketOptions2()
		{
			RequestedSubProtocols = new List<string>();
			RequestHeaders = new Dictionary<string, string>();
			Proxy = WebRequest.DefaultWebProxy;
			ReceiveBufferSize = 16384;
			SendBufferSize = 16384;
			_keepAliveInterval = WebSocket.DefaultKeepAliveInterval;
		}

		public void SetRequestHeader(string headerName, string headerValue)
		{
			ThrowIfReadOnly();
			RequestHeaders[headerName] = headerValue;
		}

		public IDictionary<string, string> RequestHeaders { get; }

		/// <summary>Gets or sets a <see cref="T:System.Boolean" /> value that indicates if default credentials should be used during WebSocket handshake.</summary>
		/// <returns>Returns <see cref="T:System.Boolean" />.
		/// <see langword="true" /> if default credentials should be used during WebSocket handshake; otherwise <see langword="false" />. The default is <see langword="true" />.</returns>
		public bool UseDefaultCredentials
		{
			get => _useDefaultCredentials;
			set
			{
				ThrowIfReadOnly();
				_useDefaultCredentials = value;
			}
		}

		/// <summary>Gets or sets the credential information for the client.</summary>
		/// <returns>Returns <see cref="T:System.Net.ICredentials" />.The credential information for the client.</returns>
		public ICredentials Credentials
		{
			get => _credentials;
			set
			{
				ThrowIfReadOnly();
				_credentials = value;
			}
		}

		/// <summary>Gets or sets the proxy for WebSocket requests.</summary>
		/// <returns>Returns <see cref="T:System.Net.IWebProxy" />.The proxy for WebSocket requests.</returns>
		public IWebProxy Proxy
		{
			get => _proxy;
			set
			{
				ThrowIfReadOnly();
				_proxy = value;
			}
		}

		/// <summary>Gets or sets a collection of client side certificates.</summary>
		/// <returns>Returns <see cref="T:System.Security.Cryptography.X509Certificates.X509CertificateCollection" />.A collection of client side certificates.</returns>
		public X509CertificateCollection ClientCertificates
		{
			get => InternalClientCertificates ?? (InternalClientCertificates = new X509CertificateCollection());
			set
			{
				ThrowIfReadOnly();
				InternalClientCertificates = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		internal X509CertificateCollection InternalClientCertificates { get; private set; }

		/// <summary>Gets or sets the cookies associated with the request.</summary>
		/// <returns>Returns <see cref="T:System.Net.CookieContainer" />.The cookies associated with the request.</returns>
		public CookieContainer Cookies
		{
			get => _cookies;
			set
			{
				ThrowIfReadOnly();
				_cookies = value;
			}
		}

		/// <summary>Sets the client buffer parameters.</summary>
		/// <param name="receiveBufferSize">The size, in bytes, of the client receive buffer.</param>
		/// <param name="sendBufferSize">The size, in bytes, of the client send buffer.</param>
		public void SetBuffer(int receiveBufferSize, int sendBufferSize)
		{
			ThrowIfReadOnly();
			//WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, sendBufferSize);
			_buffer = new ArraySegment<byte>?();
			ReceiveBufferSize = receiveBufferSize;
			SendBufferSize = sendBufferSize;
		}

		/// <summary>Sets client buffer parameters.</summary>
		/// <param name="receiveBufferSize">The size, in bytes, of the client receive buffer.</param>
		/// <param name="sendBufferSize">The size, in bytes, of the client send buffer.</param>
		/// <param name="buffer">The receive buffer to use.</param>
		public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
		{
			ThrowIfReadOnly();

			//WebSocketHelpers.ValidateBufferSizes(receiveBufferSize, sendBufferSize);
			//WebSocketHelpers.ValidateArraySegment<byte>(buffer, nameof(buffer));
			//WebSocketBuffer.Validate(buffer.Count, receiveBufferSize, sendBufferSize, false);

			ReceiveBufferSize = receiveBufferSize;
			SendBufferSize = sendBufferSize;

			_buffer = AppDomain.CurrentDomain.IsFullyTrusted ? buffer : new ArraySegment<byte>?();
		}

		public int ReceiveBufferSize { get; private set; }

		public int SendBufferSize { get; private set; }

		internal ArraySegment<byte> GetOrCreateBuffer()
		{
			if (!_buffer.HasValue)
				_buffer = WebSocket.CreateClientBuffer(ReceiveBufferSize, SendBufferSize);

			return _buffer.Value;
		}

		/// <summary>Adds a sub-protocol to be negotiated during the WebSocket connection handshake.</summary>
		/// <param name="subProtocol">The WebSocket sub-protocol to add.</param>
		public void AddSubProtocol(string subProtocol)
		{
			ThrowIfReadOnly();
			//WebSocketHelpers.ValidateSubprotocol(subProtocol);

			foreach (var requestedSubProtocol in RequestedSubProtocols)
			{
				if (requestedSubProtocol.CompareIgnoreCase(subProtocol))
					throw new ArgumentException("net_WebSockets_NoDuplicateProtocol", nameof(subProtocol));
			}

			RequestedSubProtocols.Add(subProtocol);
		}

		public IList<string> RequestedSubProtocols { get; }

		/// <summary>Gets or sets the WebSocket protocol keep-alive interval in milliseconds.</summary>
		/// <returns>Returns <see cref="T:System.TimeSpan" />.The WebSocket protocol keep-alive interval in milliseconds.</returns>
		public TimeSpan KeepAliveInterval
		{
			get => _keepAliveInterval;
			set
			{
				ThrowIfReadOnly();

				if (value < Timeout.InfiniteTimeSpan)
					throw new ArgumentOutOfRangeException(nameof(value), value, "net_WebSockets_ArgumentOutOfRange_TooSmall");

				_keepAliveInterval = value;
			}
		}

		internal void SetToReadOnly()
		{
			_isReadOnly = true;
		}

		private void ThrowIfReadOnly()
		{
			if (_isReadOnly)
				throw new InvalidOperationException("net_WebSockets_AlreadyStarted");
		}

		public static string GetSecWebSocketAcceptString(string secWebSocketKey)
		{
			using (var shA1 = SHA1.Create())
			{
				var bytes = Encoding.UTF8.GetBytes(secWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
				return shA1.ComputeHash(bytes).Base64();
			}
		}
	}

	public class ClientWebSocket2 : WebSocket
	{
		private WebSocket _innerWebSocket;
		private readonly CancellationTokenSource _cts;
		private int _state;
		private const int _created = 0;
		private const int _connecting = 1;
		private const int _connected = 2;
		private const int _disposed = 3;

		static ClientWebSocket2()
		{
			RegisterPrefixes();
		}

		public ClientWebSocket2()
		{
			_state = 0;
			Options = new ClientWebSocketOptions2();
			_cts = new CancellationTokenSource();
		}

		public ClientWebSocketOptions2 Options { get; }

		public override WebSocketCloseStatus? CloseStatus => _innerWebSocket?.CloseStatus;

		public override string CloseStatusDescription => _innerWebSocket?.CloseStatusDescription;

		public override string SubProtocol => _innerWebSocket?.SubProtocol;

		public override WebSocketState State
		{
			get
			{
				if (_innerWebSocket != null)
					return _innerWebSocket.State;

				switch (_state)
				{
					case _created:
						return WebSocketState.None;
					case _connecting:
						return WebSocketState.Connecting;
					case _disposed:
						return WebSocketState.Closed;
					default:
						return WebSocketState.Closed;
				}
			}
		}

		public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
		{
			if (uri == null)
				throw new ArgumentNullException(nameof(uri));
			if (!uri.IsAbsoluteUri)
				throw new ArgumentException("net_uri_NotAbsolute", nameof(uri));

			if (uri.Scheme != "ws" && uri.Scheme != "wss")
				throw new ArgumentException("net_WebSockets_Scheme", nameof(uri));

			switch (Interlocked.CompareExchange(ref _state, 1, 0))
			{
				case _created:
					Options.SetToReadOnly();
					return ConnectAsyncCore(uri, cancellationToken);
				case _disposed:
					throw new ObjectDisposedException(GetType().FullName);
				default:
					throw new InvalidOperationException("net_WebSockets_AlreadyStarted");
			}
		}

		private async Task ConnectAsyncCore(Uri uri, CancellationToken cancellationToken)
		{
			HttpWebResponse response = null;
			var connectCancellation = new CancellationTokenRegistration();

			try
			{
				var request = CreateAndConfigureRequest(uri);

				connectCancellation = cancellationToken.Register(AbortRequest, request, false);
				response = await request.GetResponseAsync() as HttpWebResponse;

				var subProtocol = ValidateResponse(request, response);

				_innerWebSocket = CreateClientWebSocket(response.GetResponseStream(), subProtocol, Options.ReceiveBufferSize, Options.SendBufferSize,
					Options.KeepAliveInterval, false, Options.GetOrCreateBuffer());

				if (Interlocked.CompareExchange(ref _state, 2, 1) != 1)
					throw new ObjectDisposedException(GetType().FullName);
			}
			catch (WebException ex)
			{
				ConnectExceptionCleanup(response);
				throw new WebSocketException("net_webstatus_ConnectFailure", ex);
			}
			catch (Exception ex)
			{
				ConnectExceptionCleanup(response);
				throw;
			}
			finally
			{
				connectCancellation.Dispose();
			}
		}

		private void ConnectExceptionCleanup(HttpWebResponse response)
		{
			Dispose();
			response?.Dispose();
		}

		private HttpWebRequest CreateAndConfigureRequest(Uri uri)
		{
			if (!(WebRequest.Create(uri) is HttpWebRequest httpWebRequest))
				throw new InvalidOperationException("InvalidRegistration");

			foreach (var pair in Options.RequestHeaders)
			{
				var header = pair.Key;
				var value = pair.Value;

				if (header.CompareIgnoreCase("user-agent"))
					httpWebRequest.UserAgent = value;
				else
					httpWebRequest.Headers.Add(header, value);
			}

			if (Options.RequestedSubProtocols.Count > 0)
				httpWebRequest.Headers.Add("Sec-WebSocket-Protocol", string.Join(", ", Options.RequestedSubProtocols));

			if (Options.UseDefaultCredentials)
				httpWebRequest.UseDefaultCredentials = true;
			else if (Options.Credentials != null)
				httpWebRequest.Credentials = Options.Credentials;

			if (Options.InternalClientCertificates != null)
				httpWebRequest.ClientCertificates = Options.InternalClientCertificates;

			httpWebRequest.Proxy = Options.Proxy;
			httpWebRequest.CookieContainer = Options.Cookies;

			_cts.Token.Register(AbortRequest, httpWebRequest, false);
			return httpWebRequest;
		}

		private string ValidateResponse(WebRequest request, HttpWebResponse response)
		{
			if (response.StatusCode != HttpStatusCode.SwitchingProtocols)
				throw new WebSocketException($"Status: {response.StatusCode}");

			var upgradeHeader = response.Headers["Upgrade"];
			if (!upgradeHeader.CompareIgnoreCase("websocket"))
				throw new WebSocketException($"Header: {upgradeHeader}");

			var connectionHeader = response.Headers["Connection"];
			if (!connectionHeader.CompareIgnoreCase("Upgrade"))
				throw new WebSocketException("net_WebSockets_InvalidResponseHeader");

			var webSocketAcceptHeader = response.Headers["Sec-WebSocket-Accept"];
			var webSocketKey = ClientWebSocketOptions2.GetSecWebSocketAcceptString(request.Headers["Sec-WebSocket-Key"]);

			if (!webSocketAcceptHeader.CompareIgnoreCase(webSocketKey))
				throw new WebSocketException("net_WebSockets_InvalidResponseHeader");

			var protocolHeader = response.Headers["Sec-WebSocket-Protocol"];
			if (!protocolHeader.IsEmptyOrWhiteSpace() && Options.RequestedSubProtocols.Count > 0)
			{
				var flag = false;

				foreach (var requestedSubProtocol in Options.RequestedSubProtocols)
				{
					if (requestedSubProtocol.CompareIgnoreCase(protocolHeader))
					{
						flag = true;
						break;
					}
				}

				if (!flag)
					throw new WebSocketException("net_WebSockets_AcceptUnsupportedProtocol");
			}

			if (!protocolHeader.IsEmptyOrWhiteSpace())
				return protocolHeader;

			return null;
		}

		public override Task SendAsync(
			ArraySegment<byte> buffer,
			WebSocketMessageType messageType,
			bool endOfMessage,
			CancellationToken cancellationToken)
		{
			ThrowIfNotConnected();
			return _innerWebSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
		}

		public override Task<WebSocketReceiveResult> ReceiveAsync(
			ArraySegment<byte> buffer,
			CancellationToken cancellationToken)
		{
			ThrowIfNotConnected();
			return _innerWebSocket.ReceiveAsync(buffer, cancellationToken);
		}

		public override Task CloseAsync(
			WebSocketCloseStatus closeStatus,
			string statusDescription,
			CancellationToken cancellationToken)
		{
			ThrowIfNotConnected();
			return _innerWebSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
		}

		public override Task CloseOutputAsync(
			WebSocketCloseStatus closeStatus,
			string statusDescription,
			CancellationToken cancellationToken)
		{
			ThrowIfNotConnected();
			return _innerWebSocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
		}

		public override void Abort()
		{
			if (_state == _disposed)
				return;

			_innerWebSocket?.Abort();
			Dispose();
		}

		private static void AbortRequest(object obj)
		{
			((WebRequest)obj).Abort();
		}

		public override void Dispose()
		{
			if (Interlocked.Exchange(ref _state, _disposed) == _disposed)
				return;

			_cts.Cancel(false);
			_cts.Dispose();
			_innerWebSocket?.Dispose();
		}

		private void ThrowIfNotConnected()
		{
			if (_state == _disposed)
				throw new ObjectDisposedException(GetType().FullName);

			if (_state != _connected)
				throw new InvalidOperationException("Not connected.");
		}
	}
}