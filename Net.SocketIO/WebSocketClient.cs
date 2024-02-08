namespace Ecng.Net
{
	using System.Net.WebSockets;
	using System.IO;
	using System.Reflection;

	using Ecng.Reflection;

#if NET5_0_OR_GREATER
	using System.Net.Security;
#endif

	public class WebSocketClient : Disposable
	{
		private ClientWebSocket _ws;
		private CancellationTokenSource _source;
		private bool _expectedDisconnect;

		private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = new();

		private readonly Action<Exception> _error;
		private readonly Action _connected;
		private readonly Action<bool> _disconnected;
		private readonly Action<WebSocketClient, ArraySegment<byte>> _process;
		private readonly Action<string, object> _infoLog;
		private readonly Action<string, object> _errorLog;
		private readonly Action<string, object> _verboseLog;

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<string> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, (c, s) => process(s), infoLog, errorLog, verbose, verbose2)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<object> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, (c, s) => process(s.DeserializeObject<object>()), infoLog, errorLog, verbose, verbose2)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, string> process,
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
				var recv = b.UTF8();
				verbose2(recv);
				process(c, recv);
			};
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<ArraySegment<byte>> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose)
			: this(connected, disconnected, error, (c, b) => process(b), infoLog, errorLog, verbose)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, ArraySegment<byte>> process,
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

		public int ReconnectAttempts { get; set; }

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

			var source = new CancellationTokenSource();

			_expectedDisconnect = false;
			_source = source;

			_disconnectionStates[source] = _expectedDisconnect;

			return ConnectImpl(source, 0, cancellationToken == default ? source.Token : cancellationToken);
		}

		private async ValueTask ConnectImpl(CancellationTokenSource source, int attempts, CancellationToken token)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			try
			{
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
							await Task.Delay(ResendInterval, token);
							continue;
						}

						throw;
					}
				}
			}
			catch (OperationCanceledException)
			{
				_infoLog("Connection {0} cannot be processed. Cancellation invoked.", _url);
				throw;
			}

			if (_immediateConnect)
				_connected.Invoke();

			ThreadingHelper.ThreadInvariant(() => OnReceive(source)).Launch();
		}

		public bool IsConnected => _ws != null;

		public void Disconnect(bool expectedDisconnect = true)
		{
			if (_source is null)
				throw new InvalidOperationException("Not connected.");

			_expectedDisconnect = expectedDisconnect;
			_disconnectionStates[_source] = expectedDisconnect;
			_source.Cancel();
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

		private void OnReceive(CancellationTokenSource source)
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
						var task = _ws?.ReceiveAsync(new(buf), token);

						if (task is null)
							break;

						task.Wait(token);

						if (token.IsCancellationRequested)
							break;

						var result = task.Result;

						if (result.CloseStatus != null)
						{
							if (task.Exception != null && !token.IsCancellationRequested)
								_error(task.Exception);

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

							_process(this, processBuf);

							errorCount = 0;
						}
						catch (Exception ex)
						{
							_error(new InvalidOperationException($"Error parsing string '{processBuf.UTF8()}'.", ex));

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
				_disconnected(expected);

				if (!expected && (attempts > 0 || attempts == -1))
				{
					_infoLog("Socket re-connecting '{0}'.", _url);

					try
					{
						AsyncHelper.Run(() => ConnectImpl(source, attempts, token));
					}
					catch (OperationCanceledException)
					{
					}
				}
			}
			catch (Exception ex)
			{
				_error(ex);
			}
		}

		public void Send(object obj)
			=> AsyncHelper.Run(() => SendAsync(obj));

		public ValueTask SendAsync(object obj) => SendAsync(obj, _source.Token);

		public ValueTask SendAsync(object obj, CancellationToken cancellationToken)
		{
			if (obj is not byte[] sendBuf)
			{
				var json = obj as string ?? obj.ToJson();
				_verboseLog("Send: '{0}'", json);

				sendBuf = json.UTF8();
			}

			return SendAsync(sendBuf, WebSocketMessageType.Text, cancellationToken);
		}

		public void Send(byte[] sendBuf, WebSocketMessageType type)
			=> AsyncHelper.Run(() => SendAsync(sendBuf, type));

		public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type)
			=> SendAsync(sendBuf, type, _source.Token);

		public ValueTask SendAsync(byte[] sendBuf, WebSocketMessageType type, CancellationToken cancellationToken)
			=> _ws?.SendAsync(new ArraySegment<byte>(sendBuf), type, true, cancellationToken).AsValueTask() ?? default;

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
			return (ValueTask)_sendMethod.Invoke(socket, new[] { opCode, true, true, ReadOnlyMemory<byte>.Empty });
		}
	}
}
