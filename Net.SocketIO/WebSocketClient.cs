namespace Ecng.Net;

using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Buffers;

using Ecng.Reflection;
using Ecng.Localization;

/// <summary>
/// Represents a client for WebSocket connections.
/// </summary>
public class WebSocketClient : Disposable, IConnection
{
	private ClientWebSocket _ws;
	private CancellationTokenSource _source;

	private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = [];

	private readonly Action<Exception> _error;
	private readonly Func<WebSocketClient, WebSocketMessage, CancellationToken, ValueTask> _process;
	private readonly Action<string, object> _infoLog;
	private readonly Action<string, object> _errorLog;
	private readonly Action<string, object> _verboseLog;
	private readonly Uri _url;

	private readonly CachedSynchronizedList<(long subId, byte[] buffer, WebSocketMessageType type, Func<long, CancellationToken, ValueTask> pre)> _reConnectCommands = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="WebSocketClient"/> class.
	/// </summary>
	/// <param name="url">The URL to connect to.</param>
	/// <param name="stateChanged">Action to call when connection state changes.</param>
	/// <param name="error">Action to handle errors.</param>
	/// <param name="process">Function to process incoming messages.</param>
	/// <param name="infoLog">Action to log informational messages.</param>
	/// <param name="errorLog">Action to log error messages.</param>
	/// <param name="verboseLog">Action to log verbose messages.</param>
	/// <exception cref="ArgumentNullException">If any required parameter is null.</exception>
	public WebSocketClient(string url, Action<ConnectionStates> stateChanged, Action<Exception> error,
		Func<WebSocketMessage, CancellationToken, ValueTask> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verboseLog)
		: this(url, stateChanged, error, (cl, msg, t) => process(msg, t), infoLog, errorLog, verboseLog)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WebSocketClient"/> class.
	/// </summary>
	/// <param name="url">The URL to connect to.</param>
	/// <param name="stateChanged">Action to call when the connection state changes.</param>
	/// <param name="error">Action to handle errors.</param>
	/// <param name="process">Function to process incoming messages with reference to the client instance.</param>
	/// <param name="infoLog">Action to log informational messages.</param>
	/// <param name="errorLog">Action to log error messages.</param>
	/// <param name="verboseLog">Action to log verbose messages.</param>
	/// <exception cref="ArgumentNullException">If any required parameter is null.</exception>
	public WebSocketClient(string url, Action<ConnectionStates> stateChanged, Action<Exception> error,
		Func<WebSocketClient, WebSocketMessage, CancellationToken, ValueTask> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verboseLog)
	{
		_url = new(url.ThrowIfEmpty(nameof(url)));

		StateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
		_error = error ?? throw new ArgumentNullException(nameof(error));
		_process = process ?? throw new ArgumentNullException(nameof(process));
		_infoLog = infoLog ?? throw new ArgumentNullException(nameof(infoLog));
		_errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
		_verboseLog = verboseLog/* ?? throw new ArgumentNullException(nameof(verboseLog))*/;

		BufferSize = 1024 * 1024;
		BufferSizeUncompress = BufferSize * 10;
	}

	private Encoding _encoding = Encoding.UTF8;

	/// <summary>
	/// Gets or sets the encoding used to convert between bytes and string data.
	/// </summary>
	/// <exception cref="ArgumentNullException">Thrown if a null value is set.</exception>
	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	private TimeSpan _reconnectInterval = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Gets or sets the interval between reconnection attempts.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if a negative time span is set.</exception>
	public TimeSpan ReconnectInterval
	{
		get => _reconnectInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));

			_reconnectInterval = value;
		}
	}

	private TimeSpan _resendInterval = TimeSpan.FromSeconds(2);

	/// <summary>
	/// Gets or sets the interval between resend attempts for messages.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if a negative time span is set.</exception>
	public TimeSpan ResendInterval
	{
		get => _resendInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));

			_resendInterval = value;
		}
	}

	private int _reconnectAttempts;

	/// <summary>
	/// Gets or sets the number of reconnection attempts.
	/// -1 means infinite attempts, 0 means no reconnect.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if value is less than -1.</exception>
	public int ReconnectAttempts
	{
		get => _reconnectAttempts;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.".Localize());

			_reconnectAttempts = value;
		}
	}

	/// <summary>
	/// Occurs when the connection state changes.
	/// </summary>
	public event Action<ConnectionStates> StateChanged;

	/// <summary>
	/// Occurs when the client web socket is initialized.
	/// </summary>
	public event Action<ClientWebSocket> Init;

	/// <summary>
	/// Occurs after a successful connection.
	/// </summary>
	public event Func<bool, CancellationToken, ValueTask> PostConnect;

	/// <summary>
	/// Occurs before processing received data.
	/// </summary>
	[Obsolete("Use PreProcess2 event instead.")]
	public event Func<ArraySegment<byte>, byte[], int> PreProcess;

	/// <summary>
	/// Occurs before processing received data.
	/// Input: original buffer (<see cref="ReadOnlyMemory{T}"/>), output: destination buffer (<see cref="Memory{T}"/>) returns processed length.
	/// </summary>
	public event Func<ReadOnlyMemory<byte>, Memory<byte>, int> PreProcess2;

	/// <summary>
	/// Connects to the server synchronously.
	/// </summary>
	/// <remarks>This method runs the asynchronous connection method synchronously.</remarks>
	public void Connect()
		=> AsyncHelper.Run(() => ConnectAsync(default));

	/// <summary>
	/// Asynchronously connects to the server.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous connect operation.</returns>
	public async ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		RaiseStateChanged(ConnectionStates.Connecting);

		_reConnectCommands.Clear();

		var source = new CancellationTokenSource();

		_source = source;

		_disconnectionStates[source] = false;

		using var linked = CancellationTokenSource.CreateLinkedTokenSource(source.Token, cancellationToken);
		await ConnectImpl(source, false, 0, linked.Token);
	}

	private void RaiseStateChanged(ConnectionStates state)
	{
		if (state == ConnectionStates.Failed)
			_errorLog("{0}", state);
		else
			_infoLog("{0}", state);

		State = state;
		StateChanged?.Invoke(state);
	}

	private async ValueTask ConnectImpl(CancellationTokenSource source, bool reconnect, int attempts, CancellationToken token)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		while (true)
		{
			token.ThrowIfCancellationRequested();

			if (attempts > 0)
				attempts--;

			var ws = new ClientWebSocket();
			_ws = ws;

			Init?.Invoke(ws);

			try
			{
				_infoLog("Connecting to {0}...", _url);
				await ws.ConnectAsync(_url, token).NoWait();

				if (PostConnect is not null)
					await PostConnect(reconnect, token).NoWait();

				break;
			}
			catch
			{
				if (attempts > 0 || attempts == -1)
				{
					_errorLog("Reconnect failed. Attempts left {0}.", attempts);
					await ReconnectInterval.Delay(token).NoWait();
					continue;
				}

				try { _ws?.Dispose(); } catch { }
				_ws = null;
				throw;
			}
		}

		RaiseStateChanged(reconnect ? ConnectionStates.Restored : ConnectionStates.Connected);

		_ = Task.Run(() => OnReceive(source));

		if (reconnect && !DisableAutoResend && _reConnectCommands.Count > 0)
		{
			await ResendTimeout.Delay(token).NoWait();
			await ResendAsync(token).NoWait();
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether auto resend is disabled.
	/// </summary>
	public bool DisableAutoResend { get; set; }

	/// <summary>
	/// Gets or sets the timeout before resend of commands after reconnection.
	/// </summary>
	public TimeSpan ResendTimeout { get; set; }

	/// <summary>
	/// Gets the current connection state.
	/// </summary>
	public ConnectionStates State { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the client is connected.
	/// </summary>
	public bool IsConnected => _ws?.State == WebSocketState.Open;

	/// <summary>
	/// Disconnects from the server.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown if the client is not connected.</exception>
	public void Disconnect()
	{
		var source = _source;

		if (source is null)
			throw new InvalidOperationException("Not connected.");

		RaiseStateChanged(ConnectionStates.Disconnecting);

		_disconnectionStates[source] = true;
		source.Cancel();
		_source = null;

		// Immediately make further sends fail deterministically
		try { _ws?.Dispose(); } catch { }
		_ws = null;
		
		_reConnectCommands.Clear();
	}

	/// <summary>
	/// Gets or sets the disconnect timeout period.
	/// </summary>
	public TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(10);

	private int _bufferSize;

	/// <summary>
	/// Gets or sets the buffer size for compressed data.
	/// </summary>
	public int BufferSize
	{
		get => _bufferSize;
		set => _bufferSize = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
	}

	private int _bufferSizeUncompress;

	/// <summary>
	/// Gets or sets the buffer size for uncompressed data.
	/// </summary>
	public int BufferSizeUncompress
	{
		get => _bufferSizeUncompress;
		set => _bufferSizeUncompress = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
	}

	private async Task OnReceive(CancellationTokenSource source)
	{
		try
		{
			var token = source.Token;

			Memory<byte> recvMem = new byte[BufferSize];
			var responseBuffer = new ArrayBufferWriter<byte>(BufferSize);

			var preProcess = PreProcess; // legacy
			var preProcessBuf = preProcess != null ? new byte[BufferSizeUncompress] : null;

			var preProcess2 = PreProcess2; // new Memory-based
			var preProcess2Mem = preProcess2 != null ? new byte[BufferSizeUncompress] : Memory<byte>.Empty;

			var errorCount = 0;

			const int maxParsingErrors = 100;
			const int maxNetworkErrors = 10;

			var attempts = ReconnectAttempts;

			var needClose = true;

			_infoLog("Starting receive loop, {0} attempts", attempts);

			while (!token.IsCancellationRequested)
			{
				try
				{
					var ws = _ws;

					if (ws is null)
						break;

					ValueWebSocketReceiveResult result;

					try
					{
						result = await ws.ReceiveAsync(recvMem, token).NoWait();
					}
					catch (Exception ex)
					{
						if (!token.IsCancellationRequested)
						{
							_error(ex);
							needClose = false;
						}

						break;
					}

					if (result.MessageType == WebSocketMessageType.Close)
					{
						_infoLog("Socket closed by peer.", null);

						needClose = false;
						break;
					}

					responseBuffer.Write(recvMem.Span[..result.Count]);

					if (!result.EndOfMessage)
						continue;

					if (responseBuffer.WrittenCount == 0)
						continue;

					try
					{
						// Memory-based path primary
						var roMem = responseBuffer.WrittenMemory;

						if (preProcess2 != null)
						{
							var count = preProcess2(roMem, preProcess2Mem);
							roMem = preProcess2Mem[..count];
						}
						else if (preProcessBuf != null && preProcess != null)
						{
							if (!MemoryMarshal.TryGetArray(roMem, out var seg))
								seg = new(roMem.ToArray());

							var count = preProcess(seg, preProcessBuf);
							roMem = new(preProcessBuf, 0, count);
						}

						if (_verboseLog is not null)
							_verboseLog("{0}", Encoding.GetString(roMem.Span));

						await _process(this, new(Encoding, roMem), token).NoWait();

						errorCount = 0;
					}
					catch (Exception ex)
					{
						if (token.IsCancellationRequested)
							break;

						_error(new InvalidOperationException($"Error parsing string '{Encoding.GetString(responseBuffer.WrittenSpan)}'.", ex));

						if (++errorCount < maxParsingErrors)
							continue;

						_errorLog("Max parsing error {0} limit reached.", maxParsingErrors);
					}
					finally
					{
						responseBuffer.Clear();
					}
				}
				catch (AggregateException ex)
				{
					if (!token.IsCancellationRequested)
						_error(ex);

					if (ex.InnerExceptions.FirstOrDefault() is WebSocketException)
						break;

					if (++errorCount < maxNetworkErrors)
					{
						if (!token.IsCancellationRequested)
							_errorLog("{0} errors", $"{errorCount}/{maxNetworkErrors}");

						continue;
					}

					_errorLog("Max network error {0} limit reached.", maxNetworkErrors);
					break;
				}
				catch (Exception ex)
				{
					if (!token.IsCancellationRequested)
						_error(ex);
				}
			}

			try
			{
				if (needClose && _ws is ClientWebSocket ws)
				{
					using var cts = DisconnectTimeout.CreateTimeout();
					await ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cts.Token);
				}
			}
			catch (Exception ex)
			{
				if (!token.IsCancellationRequested)
					_error(ex);
			}

			try
			{
				_ws?.Dispose();
			}
			catch { }

			_ws = null;

			var expected = _disconnectionStates.TryGetAndRemove2(source);
			_infoLog("websocket disconnected, {0}", $"expected={expected}, attempts={attempts}");

			if (expected == true)
			{
				RaiseStateChanged(ConnectionStates.Disconnected);

				if (ReferenceEquals(_source, source))
					_source = null;

				try { source.Dispose(); } catch { }
			}
			else
			{
				if (attempts > 0 || attempts == -1)
				{
					RaiseStateChanged(ConnectionStates.Reconnecting);

					_infoLog("Socket re-connecting '{0}'.", _url);

					try
					{
						await ConnectImpl(source, true, attempts, token);
						return;
					}
					catch (Exception ex)
					{
						if (!token.IsCancellationRequested)
							_error(ex);
					}
				}

				RaiseStateChanged(ConnectionStates.Failed);

				if (ReferenceEquals(_source, source))
					_source = null;

				try { source.Dispose(); } catch { }
			}
		}
		catch (Exception ex)
		{
			_error(ex);
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether output JSON should be indented.
	/// </summary>
	public bool Indent { get; set; } = true;

	/// <summary>
	/// Gets or sets the JSON serializer settings used when sending objects.
	/// </summary>
	public JsonSerializerSettings SendSettings { get; set; }

	private string ToJson(object obj)
		=> obj.ToJson(Indent, SendSettings);

	/// <summary>
	/// Sends an object to the server synchronously.
	/// </summary>
	/// <param name="obj">The object to send. If not a byte array, it is converted to JSON.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	public void Send(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> AsyncHelper.Run(() => SendAsync(obj, subId, pre));

	/// <summary>
	/// Asynchronously sends an object to the server.
	/// </summary>
	/// <param name="obj">The object to send. If not a byte array, it is converted to JSON.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public ValueTask SendAsync(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> SendAsync(obj, _source?.Token ?? throw new InvalidOperationException("Connection was not established."), subId, pre);

	/// <summary>
	/// Asynchronously sends an object to the server.
	/// </summary>
	/// <param name="obj">The object to send. If not a byte array, it is converted to JSON.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public ValueTask SendAsync(object obj, CancellationToken cancellationToken, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
	{
		if (obj is not byte[] sendBuf)
		{
			var json = obj as string ?? ToJson(obj);
			_verboseLog?.Invoke("Send: '{0}'", json);
			sendBuf = Encoding.GetBytes(json);
		}

		return SendAsync(sendBuf, WebSocketMessageType.Text, cancellationToken, subId, pre);
	}

	/// <summary>
	/// Sends a byte array message to the server synchronously.
	/// </summary>
	/// <param name="sendBuf">The byte array to send.</param>
	/// <param name="type">The type of WebSocket message.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	public void Send(byte[] sendBuf, WebSocketMessageType type, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> AsyncHelper.Run(() => SendAsync(sendBuf, type, subId, pre));

	/// <summary>
	/// Asynchronously sends a byte array message to the server.
	/// </summary>
	/// <param name="sendBuf">The byte array to send.</param>
	/// <param name="type">The type of WebSocket message.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> SendAsync(sendBuf, type, _source?.Token ?? throw new InvalidOperationException("Connection was not established."), subId, pre);

	/// <summary>
	/// Asynchronously sends a byte array message to the server.
	/// </summary>
	/// <param name="sendBuf">The byte array to send.</param>
	/// <param name="type">The type of WebSocket message.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <param name="subId">The subscription identifier.</param>
	/// <param name="pre">A pre-send callback function.</param>
	/// <returns>A task that represents the asynchronous send operation.</returns>
	public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type, CancellationToken cancellationToken, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
	{
		if (_ws is not ClientWebSocket ws)
			throw new InvalidOperationException("WebSocket is not connected.");

		if (ws.State != WebSocketState.Open)
			throw new InvalidOperationException("WebSocket is not open.");

		if (subId > 0)
			_reConnectCommands.Add((subId, sendBuf, type, pre));
		else if (subId < 0) // unsubscribe
			RemoveResend(subId);

		return ws.SendAsync(new ArraySegment<byte>(sendBuf), type, true, cancellationToken).AsValueTask();
	}

	/// <summary>
	/// Asynchronously resends stored commands after reconnecting.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous resend operation.</returns>
	public async ValueTask ResendAsync(CancellationToken cancellationToken)
	{
		_infoLog("Reconnect commands: {0}", _reConnectCommands.Count);

		foreach (var (id, buf, type, pre) in _reConnectCommands.Cache)
		{
			try
			{
				if (_verboseLog is not null)
					_verboseLog("ReSend: '{0}'", Encoding.GetString(buf));

				if (pre is not null)
					await pre(id, cancellationToken).NoWait();

				await SendAsync(buf, type, cancellationToken).NoWait();
				await ResendInterval.Delay(cancellationToken).NoWait();
			}
			catch (Exception ex)
			{
				if (!cancellationToken.IsCancellationRequested)
					_error(ex);
			}
		}
	}

	/// <summary>
	/// Removes a resend command for the specified subscription identifier.
	/// </summary>
	/// <param name="subId">The subscription identifier.</param>
	public void RemoveResend(long subId)
	{
		subId = subId.Abs();

		using (_reConnectCommands.EnterScope())
			_reConnectCommands.RemoveWhere(t => t.subId == subId);
	}

	/// <summary>
	/// Removes all resend commands.
	/// </summary>
	public void RemoveResend()
		=> _reConnectCommands.Clear();

	/// <summary>
	/// Aborts the WebSocket connection immediately.
	/// </summary>
	public void Abort()
	{
		_ws?.Abort();
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_source?.Cancel();

		base.DisposeManaged();
	}

	// The following members are internal or private and hence not documented with XML comments for public API.

	private FieldInfo _innerSocketField;
	private PropertyInfo _socketProp;
	private Type _opCodeEnum;
	private MethodInfo _sendMethod;

	/// <summary>
	/// Sends an operation code directly.
	/// </summary>
	/// <param name="code">The operation code to send (default is 0x9 for ping).</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public ValueTask SendOpCode(byte code = 0x9 /* ping */)
	{
		if (_ws is not ClientWebSocket ws || ws.State != WebSocketState.Open)
			throw new InvalidOperationException("WebSocket is not connected.");

		try
		{
			_innerSocketField ??= typeof(ClientWebSocket).GetMember<FieldInfo>("_innerWebSocket");
			if (_innerSocketField is null)
				throw new NotSupportedException("Inner web socket field not found.");

			var handle = _innerSocketField.GetValue(ws) ?? throw new NotSupportedException("Inner web socket handle is null.");

			_socketProp ??= handle.GetType().GetMember<PropertyInfo>("WebSocket");
			if (_socketProp is null)
				throw new NotSupportedException("WebSocket property not found.");

			var socket = (WebSocket)_socketProp.GetValue(handle) ?? throw new NotSupportedException("Managed WebSocket instance is null.");

			_opCodeEnum ??= typeof(WebSocket).Assembly.GetType("System.Net.WebSockets.ManagedWebSocket+MessageOpcode");
			if (_opCodeEnum is null)
				throw new NotSupportedException("MessageOpcode enum type not found.");

			var opCode = Enum.ToObject(_opCodeEnum, code);

			_sendMethod ??= socket.GetType().GetMember<MethodInfo>("SendFrameLockAcquiredNonCancelableAsync");
			if (_sendMethod is null)
				throw new NotSupportedException("SendFrameLockAcquiredNonCancelableAsync method not found.");

			return (ValueTask)_sendMethod.Invoke(socket, [opCode, true, true, ReadOnlyMemory<byte>.Empty]);
		}
		catch (NotSupportedException)
		{
			throw;
		}
		catch (Exception ex)
		{
			throw new NotSupportedException("Sending custom opcode is not supported on this platform/runtime.", ex);
		}
	}
}
