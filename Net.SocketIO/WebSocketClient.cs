namespace Ecng.Net
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;

	using Newtonsoft.Json;

	public class WebSocketClient : Disposable
	{
		private ClientWebSocket _ws;
		private CancellationTokenSource _source;
		private bool _expectedDisconnect;

		private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = new SynchronizedDictionary<CancellationTokenSource, bool>();

		private readonly SynchronizedList<Tuple<byte[], WebSocketMessageType, long>> _resendCommands = new SynchronizedList<Tuple<byte[], WebSocketMessageType, long>>();

		private readonly Action<Exception> _error;
		private readonly Action _connected;
		private readonly Action<bool> _disconnected;
		private readonly Action<WebSocketClient, byte[], int, int> _process;
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

		private static Action<WebSocketClient, byte[], int, int> BytesToString(Action<WebSocketClient, string> process, Action<string> verbose2)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));

			return (c, b, s, o) =>
			{
				var recv = Encoding.UTF8.GetString(b, s, o);
				verbose2(recv);
				process(c, recv);
			};
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<byte[], int, int> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose)
			: this(connected, disconnected, error, (c, b, s, o) => process(b, s, o), infoLog, errorLog, verbose)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, byte[], int, int> process,
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

		public event Func<byte[], int, int, byte[], int> PreProcess;

		private Uri _url;
		private bool _immediateConnect;
		private Action<ClientWebSocket> _init;

		public void Connect(string url, bool immediateConnect, Action<ClientWebSocket> init = null)
		{
			_url = new Uri(url);
			_immediateConnect = immediateConnect;
			_init = init;

			_resendCommands.Clear();

			var source = new CancellationTokenSource();

			_expectedDisconnect = false;
			_source = source;

			_disconnectionStates[source] = _expectedDisconnect;

			ConnectImpl(source, 0);
		}

		private void ConnectImpl(CancellationTokenSource source, int attempts)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			while (!source.IsCancellationRequested)
			{
				if (attempts > 0)
					attempts--;

				_ws = new ClientWebSocket();
				_init?.Invoke(_ws);

				try
				{
					_infoLog("Connecting to {0}...".Translate(), _url);
					_ws.ConnectAsync(_url, source.Token).Wait();
					break;
				}
				catch
				{
					if (attempts > 0 || attempts == -1)
					{
						_errorLog("Reconnect failed. Attemps left {0}.".Translate(), attempts);
						ResendInterval.Sleep();
						continue;
					}

					throw;
				}
			}

			if (source.IsCancellationRequested)
			{
				_infoLog("Connection {0} cannot be processed. Cancellation invoked.".Translate(), _url);
				return;
			}

			if (_immediateConnect)
				_connected.Invoke();

			ThreadingHelper.ThreadInvariant(() => OnReceive(source)).Launch();
		}

		public bool IsConnected => _ws != null;

		public void Disconnect(bool expectedDisconnect = true)
		{
			if (_source is null)
				throw new InvalidOperationException("Not connected.".Translate());

			_expectedDisconnect = expectedDisconnect;
			_disconnectionStates[_source] = expectedDisconnect;
			_source.Cancel();
		}

		public TimeSpan DisconnectTimeout = TimeSpan.FromSeconds(10);

		private int _bufferSize;

		public int BufferSize
		{
			get => _bufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				_bufferSize = value;
			}
		}

		private int _bufferSizeUncompress;

		public int BufferSizeUncompress
		{
			get => _bufferSizeUncompress;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				_bufferSizeUncompress = value;
			}
		}

		private void OnReceive(CancellationTokenSource source)
		{
			try
			{
				var buf = new byte[BufferSize];
				var pos = 0;

				var preProcess = PreProcess;
				var buf2 = preProcess != null ? new byte[BufferSizeUncompress] : null;

				var errorCount = 0;
				const int maxParsingErrors = 100;
				const int maxNetworkErrors = 10;

				var attempts = ReconnectAttempts;

				var needClose = true;

				_infoLog("Starting receive loop, {0} attempts", attempts);

				while (!source.IsCancellationRequested)
				{
					try
					{
						var task = _ws.ReceiveAsync(new ArraySegment<byte>(buf, pos, buf.Length - pos), source.Token);
						task.Wait(source.Token);

						if (source.IsCancellationRequested)
							break;

						var result = task.Result;

						if (result.CloseStatus != null)
						{
							if (task.Exception != null && !source.IsCancellationRequested)
								_error(task.Exception);

							_infoLog("Socket closed with status {0}.".Translate(), result.CloseStatus);

							needClose = false;
							break;
						}

						pos += result.Count;

						if (!result.EndOfMessage)
							continue;

						if (pos == 0)
							continue;

						string recv = null;
						var count = pos;

						pos = 0;

						var temp = buf;

						try
						{
							if (buf2 != null)
							{
								count = preProcess(buf, 0, count, buf2);
								buf = buf2;
							}

							_process(this, buf, 0, count);

							errorCount = 0;
						}
						catch (Exception ex)
						{
							_error(new InvalidOperationException("Error parsing string '{0}'.".Translate().Put(recv), ex));

							if (++errorCount < maxParsingErrors)
								continue;

							_errorLog("Max parsing error {0} limit reached.".Translate(), maxParsingErrors);
						}
						finally
						{
							buf = temp;
						}
					}
					catch (AggregateException ex)
					{
						if (!source.IsCancellationRequested)
							_error(ex);

						if (ex.InnerExceptions.FirstOrDefault() is WebSocketException)
							break;

						if (++errorCount < maxNetworkErrors)
						{
							if (!source.IsCancellationRequested)
								_errorLog("{0} errors", $"{errorCount}/{maxNetworkErrors}");
							continue;
						}

						_errorLog("Max network error {0} limit reached.".Translate(), maxNetworkErrors);
						break;
					}
					catch (Exception ex)
					{
						if (!source.IsCancellationRequested)
							_error(ex);
					}
				}

				try
				{
					if (needClose)
						_ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, new CancellationToken()).Wait((int)DisconnectTimeout.TotalMilliseconds);
				}
				catch (Exception ex)
				{
					if (!source.IsCancellationRequested)
						_error(ex);
				}

				try
				{
					_ws.Dispose();
				}
				catch { }

				_ws = null;

				var expected = _disconnectionStates.TryGetAndRemove(source);
				_infoLog("websocket disconnected, {0}", $"expected={expected}, attempts={attempts}");
				_disconnected(expected);

				if (!expected && (attempts > 0 || attempts == -1))
				{
					_infoLog("Socket re-connecting '{0}'.".Translate(), _url);
					ConnectImpl(source, attempts);
				}
			}
			catch (Exception ex)
			{
				_error(ex);
			}
		}

		public void Send(object obj, long id = default)
		{
			if (!(obj is byte[] sendBuf))
			{
				var json = obj as string ?? JsonConvert.SerializeObject(obj);
				_verboseLog("Send: '{0}'", json);

				sendBuf = Encoding.UTF8.GetBytes(json);
			}

			Send(sendBuf, WebSocketMessageType.Text, id);
		}

		public void Send(byte[] sendBuf, WebSocketMessageType type, long id = default)
		{
			if (id != default)
				_resendCommands.Add(Tuple.Create(sendBuf.ToArray(), type, id));

			_ws.SendAsync(new ArraySegment<byte>(sendBuf), type, true, _source.Token).Wait();
		}

		public void Resend()
		{
			var resendCommands = _resendCommands.CopyAndClear();

			_infoLog("Resending {0} commands.".Translate(), resendCommands.Length);

			foreach (var tuple in resendCommands)
			{
				Send(tuple.Item1, tuple.Item2, tuple.Item3);
				ResendInterval.Sleep();
			}
		}

		public void RemoveResend(long id)
		{
			_infoLog("Removing {0} from resend.".Translate(), id);

			lock (_resendCommands.SyncRoot)
				_resendCommands.RemoveWhere(t => t.Item3 == id);
		}

		protected override void DisposeManaged()
		{
			_source?.Cancel();

			base.DisposeManaged();
		}
	}
}