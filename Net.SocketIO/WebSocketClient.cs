namespace Ecng.Net;

using System.Net.WebSockets;
using System.IO;
using System.Reflection;
using System.Text;
#if NET5_0_OR_GREATER
using System.Net.Security;

using Newtonsoft.Json;
#endif

using Ecng.Reflection;

public class WebSocketClient : Disposable
{
	private ClientWebSocket _ws;
	private CancellationTokenSource _source;
	private bool _expectedDisconnect;

	private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = [];

	private readonly Action<Exception> _error;
	private readonly Action _connected;
	private readonly Action<WebSocketDropReasons> _disconnected;
	private readonly Func<WebSocketClient, ArraySegment<byte>, CancellationToken, ValueTask> _process;
	private readonly Action<string, object> _infoLog;
	private readonly Action<string, object> _errorLog;
	private readonly Action<string, object> _verboseLog;

	private readonly CachedSynchronizedList<(long subId, byte[] buffer, WebSocketMessageType type, Func<long, CancellationToken, ValueTask> pre)> _reConnectCommands = [];

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error, Action<string> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
		: this(connected, disconnected, error, (c, s) => process(s), infoLog, errorLog, verbose, verbose2)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error, Action<object> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
		: this(connected, disconnected, error, (c, s) => process(s.DeserializeObject<object>()), infoLog, errorLog, verbose, verbose2)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error, Action<WebSocketClient, string> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
		: this(connected, disconnected, error, BytesToString(process, verbose2), infoLog, errorLog, verbose)
	{
	}

	private static Action<WebSocketClient, ArraySegment<byte>> BytesToString(Action<WebSocketClient, string> process, Action<string> verbose2)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		return (c, b) =>
		{
			var recv = c.GetString(b);
			verbose2(recv);
			process(c, recv);
		};
	}

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error, Action<ArraySegment<byte>> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose)
		: this(connected, disconnected, error, (c, b) => process(b), infoLog, errorLog, verbose)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error, Action<WebSocketClient, ArraySegment<byte>> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose)
		: this(connected, disconnected, error, (ws, buffer, token) =>
		{
			process(ws, buffer);
			return default;
		}, infoLog, errorLog, verbose)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
	}

	public WebSocketClient(Action connected, Action<WebSocketDropReasons> disconnected, Action<Exception> error,
		Func<WebSocketClient, ArraySegment<byte>, CancellationToken, ValueTask> process,
		Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose)
	{
		_connected = connected ?? throw new ArgumentNullException(nameof(connected));
		_disconnected = disconnected ?? throw new ArgumentNullException(nameof(disconnected));
		_error = error ?? throw new ArgumentNullException(nameof(error));
		_process = process ?? throw new ArgumentNullException(nameof(process));
		_infoLog = infoLog ?? throw new ArgumentNullException(nameof(infoLog));
		_errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
		_verboseLog = verbose ?? throw new ArgumentNullException(nameof(verbose));

		BufferSize = 1024 * 1024;
		BufferSizeUncompress = BufferSize * 10;
	}

	private Encoding _encoding = Encoding.UTF8;

	public Encoding Encoding
	{
		get => _encoding;
		set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
	}

	private string GetString(ArraySegment<byte> buffer)
		=> Encoding.GetString(buffer.Array, buffer.Offset, buffer.Count);

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

#if NET5_0_OR_GREATER

	public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

#endif

	private Uri _url;
	private bool _immediateConnect;
	private Action<ClientWebSocket> _init;

	public void Connect(string url, bool immediateConnect, Action<ClientWebSocket> init = null)
		=> AsyncHelper.Run(() => ConnectAsync(url, immediateConnect, init));

	public ValueTask ConnectAsync(string url, bool immediateConnect, Action<ClientWebSocket> init = null, CancellationToken cancellationToken = default)
	{
		_url = new Uri(url);
		_immediateConnect = immediateConnect;
		_init = init;

		_reConnectCommands.Clear();

		var source = new CancellationTokenSource();

		_expectedDisconnect = false;
		_source = source;

		_disconnectionStates[source] = _expectedDisconnect;

		return ConnectImpl(source, false, 0, cancellationToken == default ? source.Token : cancellationToken);
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

#if NET5_0_OR_GREATER

			ws.Options.RemoteCertificateValidationCallback = RemoteCertificateValidationCallback;

#endif

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

		if (_immediateConnect)
			_connected.Invoke();

		_ = Task.Run(() => OnReceive(source), token);

		if (reconnect && _reConnectCommands.Count > 0)
		{
			await ResendAsync(token);
		}
	}

	public bool IsConnected => _ws != null;

	public void Disconnect(bool expectedDisconnect = true)
	{
		if (_source is null)
			throw new InvalidOperationException("Not connected.");

		_expectedDisconnect = expectedDisconnect;
		_disconnectionStates[_source] = expectedDisconnect;
		_source.Cancel();

		if (expectedDisconnect)
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

						await _process(this, processBuf, token);

						errorCount = 0;
					}
					catch (Exception ex)
					{
						if (token.IsCancellationRequested)
							break;

						_error(new InvalidOperationException($"Error parsing string '{GetString(processBuf)}'.", ex));

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
				_disconnected(WebSocketDropReasons.Expected);
			else
			{
				if (attempts > 0 || attempts == -1)
				{
					_disconnected(WebSocketDropReasons.Reconnecting);

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

				_disconnected(WebSocketDropReasons.Unexpected);
			}
		}
		catch (Exception ex)
		{
			_error(ex);
		}
	}

	public bool Indent { get; set; } = true;

#if NET5_0_OR_GREATER
	public JsonSerializerSettings SendSettings { get; set; }

	private string ToJson(object obj)
		=> obj.ToJson(Indent, SendSettings);
#else
	private string ToJson(object obj)
		=> obj.ToJson(Indent);
#endif

	public void Send(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> AsyncHelper.Run(() => SendAsync(obj, subId, pre));

	public ValueTask SendAsync(object obj, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
		=> SendAsync(obj, _source.Token, subId, pre);

	public ValueTask SendAsync(object obj, CancellationToken cancellationToken, long subId = default, Func<long, CancellationToken, ValueTask> pre = default)
	{
		if (obj is not byte[] sendBuf)
		{
			var json = obj as string ?? ToJson(obj);
			_verboseLog("Send: '{0}'", json);

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
