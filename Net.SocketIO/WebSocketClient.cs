namespace Ecng.Net;

using System.Net.WebSockets;

using Ecng.Reflection;
using Ecng.ComponentModel;

public class WebSocketClient : Disposable
{
	private ClientWebSocket _ws;
	private CancellationTokenSource _source;

	private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = [];

	private readonly Action<Exception> _error;
	private readonly Action<ConnectionStates> _stateChanged;
	private readonly Func<WebSocketClient, WebSocketMessage, CancellationToken, ValueTask> _process;
	private readonly Action<string, object> _infoLog;
	private readonly Action<string, object> _errorLog;
	private readonly Action<string, object> _verboseLog;

	private readonly CachedSynchronizedList<(long subId, byte[] buffer, WebSocketMessageType type, Func<long, CancellationToken, ValueTask> pre)> _reConnectCommands = [];

	public WebSocketClient(Action<ConnectionStates> stateChanged, Action<Exception> error,
		Func<WebSocketMessage, CancellationToken, ValueTask> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verboseLog)
		: this(stateChanged, error, (cl, msg, t) => process(msg, t), infoLog, errorLog, verboseLog)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	public WebSocketClient(Action<ConnectionStates> stateChanged, Action<Exception> error,
		Func<WebSocketClient, WebSocketMessage, CancellationToken, ValueTask> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verboseLog)
	{
		_stateChanged = stateChanged ?? throw new ArgumentNullException(nameof(stateChanged));
		_error = error ?? throw new ArgumentNullException(nameof(error));
		_process = process ?? throw new ArgumentNullException(nameof(process));
		_infoLog = infoLog ?? throw new ArgumentNullException(nameof(infoLog));
		_errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
		_verboseLog = verboseLog/* ?? throw new ArgumentNullException(nameof(verboseLog))*/;

		BufferSize = 1024 * 1024;
		BufferSizeUncompress = BufferSize * 10;
	}

	private Encoding _encoding = Encoding.UTF8;

	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	private TimeSpan _reconnectInterval = TimeSpan.FromSeconds(2);

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
	/// -1 means infinite.
	/// 0 means no reconnect.
	/// </summary>
	public int ReconnectAttempts
	{
		get => _reconnectAttempts;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, "Invalid value.");

			_reconnectAttempts = value;
		}
	}

	public event Func<ArraySegment<byte>, byte[], int> PreProcess;

	private Uri _url;
	private Action<ClientWebSocket> _init;

	public void Connect(string url, Action<ClientWebSocket> init = null)
		=> AsyncHelper.Run(() => ConnectAsync(url, init));

	public ValueTask ConnectAsync(string url, Action<ClientWebSocket> init = null, CancellationToken cancellationToken = default)
	{
		RaiseStateChanged(ConnectionStates.Connecting);

		_url = new(url);
		_init = init;

		_reConnectCommands.Clear();

		var source = new CancellationTokenSource();

		_source = source;

		_disconnectionStates[source] = false;

		return ConnectImpl(source, false, 0, cancellationToken == default ? source.Token : cancellationToken);
	}

	private void RaiseStateChanged(ConnectionStates state)
	{
		if (state == ConnectionStates.Failed)
			_errorLog("{0}", state);
		else
			_infoLog("{0}", state);

		_stateChanged(state);
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

			_init?.Invoke(ws);

			try
			{
				_infoLog("Connecting to {0}...", _url);
				await ws.ConnectAsync(_url, token);
				break;
			}
			catch
			{
				if (attempts > 0 || attempts == -1)
				{
					_errorLog("Reconnect failed. Attemps left {0}.", attempts);
					await ReconnectInterval.Delay(token);
					continue;
				}

				throw;
			}
		}

		RaiseStateChanged(reconnect ? ConnectionStates.Restored : ConnectionStates.Connected);

		_ = Task.Run(() => OnReceive(source), token);

		if (reconnect && _reConnectCommands.Count > 0)
		{
			await ResendAsync(token);
		}
	}

	public bool IsConnected => _ws != null;

	public void Disconnect()
	{
		if (_source is null)
			throw new InvalidOperationException("Not connected.");

		RaiseStateChanged(ConnectionStates.Disconnecting);

		_disconnectionStates[_source] = true;
		_source.Cancel();
		
		_reConnectCommands.Clear();
	}

	public TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(10);

	private int _bufferSize;

	public int BufferSize
	{
		get => _bufferSize;
		set => _bufferSize = value <= 0 ? throw new ArgumentOutOfRangeException(nameof(value)) : value;
	}

	private int _bufferSizeUncompress;

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

			var buf = new byte[BufferSize];
			var responseBody = new MemoryStream();

			var preProcess = PreProcess;
			var preProcessBuf = preProcess != null ? new byte[BufferSizeUncompress] : null;

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

					WebSocketReceiveResult result;

					try
					{
						result = await ws.ReceiveAsync(new(buf), token);
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

					if (result.CloseStatus != null)
					{
						_infoLog("Socket closed with status {0}.", result.CloseStatus);

						needClose = false;
						break;
					}

					responseBody.Write(buf, 0, result.Count);

					if (!result.EndOfMessage)
						continue;

					if (responseBody.Length == 0)
						continue;

					var processBuf = responseBody.GetActualBuffer();

					try
					{
						responseBody.Position = 0;

						if (preProcessBuf != null)
						{
							var count = preProcess(processBuf, preProcessBuf);
							processBuf = new(preProcessBuf, 0, count);
						}

						if (_verboseLog is not null)
							_verboseLog("{0}", Encoding.GetString(processBuf));

						await _process(this, new(Encoding, processBuf), token);

						errorCount = 0;
					}
					catch (Exception ex)
					{
						if (token.IsCancellationRequested)
							break;

						_error(new InvalidOperationException($"Error parsing string '{Encoding.GetString(processBuf)}'.", ex));

						if (++errorCount < maxParsingErrors)
							continue;

						_errorLog("Max parsing error {0} limit reached.", maxParsingErrors);
					}
					finally
					{
						responseBody.SetLength(0);
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
				if (needClose)
					_ws?.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, default).Wait((int)DisconnectTimeout.TotalMilliseconds);
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

			var expected = _disconnectionStates.TryGetAndRemove(source);
			_infoLog("websocket disconnected, {0}", $"expected={expected}, attempts={attempts}");

			if (expected)
				RaiseStateChanged(ConnectionStates.Disconnected);
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
			}
		}
		catch (Exception ex)
		{
			_error(ex);
		}
	}

	public bool Indent { get; set; } = true;

	public JsonSerializerSettings SendSettings { get; set; }

	private string ToJson(object obj)
		=> obj.ToJson(Indent, SendSettings);

	public void Send(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> AsyncHelper.Run(() => SendAsync(obj, subId, pre));

	public ValueTask SendAsync(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> SendAsync(obj, _source.Token, subId, pre);

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

	public void Send(byte[] sendBuf, WebSocketMessageType type, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> AsyncHelper.Run(() => SendAsync(sendBuf, type, subId, pre));

	public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> SendAsync(sendBuf, type, _source.Token, subId, pre);

	public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type, CancellationToken cancellationToken, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
	{
		if (_ws is not ClientWebSocket ws)
			return default;

		if (subId > 0)
			_reConnectCommands.Add((subId, sendBuf.ToArray(), type, pre));
		else if (subId < 0) // unsubscribe
			RemoveResend(subId);

		return ws.SendAsync(new ArraySegment<byte>(sendBuf), type, true, cancellationToken).AsValueTask();
	}

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
					await pre(id, cancellationToken);

				await SendAsync(buf, type, cancellationToken);
				await ResendInterval.Delay(cancellationToken);
			}
			catch (Exception ex)
			{
				if (!cancellationToken.IsCancellationRequested)
					_error(ex);
			}
		}
	}

	public void RemoveResend(long subId)
	{
		subId = subId.Abs();

		lock (_reConnectCommands.SyncRoot)
			_reConnectCommands.RemoveWhere(t => t.subId == subId);
	}

	public void RemoveResend()
		=> _reConnectCommands.Clear();

	protected override void DisposeManaged()
	{
		_source?.Cancel();

		base.DisposeManaged();
	}

	private FieldInfo _innerSocketField;
	private PropertyInfo _socketProp;
	private Type _opCodeEnum;
	private MethodInfo _sendMethod;

	/// <summary>
	/// This hack sends direct op codes (like ping frames instead of pong).
	/// Right now not uses anywhere.
	/// </summary>
	/// <returns></returns>
	public ValueTask SendOpCode(byte code = 0x9 /* ping */)
	{
		_innerSocketField ??= typeof(ClientWebSocket).GetMember<FieldInfo>("_innerWebSocket");
		var handle = _innerSocketField.GetValue(_ws);

		_socketProp ??= handle.GetType().GetMember<PropertyInfo>("WebSocket");
		var socket = (WebSocket)_socketProp.GetValue(handle);

		_opCodeEnum ??= typeof(WebSocket).Assembly.GetType("System.Net.WebSockets.ManagedWebSocket+MessageOpcode");
		var opCode = Enum.ToObject(_opCodeEnum, code);

		_sendMethod ??= socket.GetType().GetMember<MethodInfo>("SendFrameLockAcquiredNonCancelableAsync");
		return (ValueTask)_sendMethod.Invoke(socket, [opCode, true, true, ReadOnlyMemory<byte>.Empty]);
	}
}
