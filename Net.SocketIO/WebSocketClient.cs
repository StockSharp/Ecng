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

		private readonly SynchronizedList<Tuple<byte[], WebSocketMessageType>> _resendCommands = new SynchronizedList<Tuple<byte[], WebSocketMessageType>>();

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
			if (process == null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<object> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, (c, s) => process(s.DeserializeObject<object>()), infoLog, errorLog, verbose, verbose2)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, string> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, BytesToString(process, verbose2), infoLog, errorLog, verbose)
		{
		}

		private static Action<WebSocketClient, byte[], int, int> BytesToString(Action<WebSocketClient, string> process, Action<string> verbose2)
		{
			if (process == null)
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
			if (process == null)
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

		public bool AutoResend { get; set; }

		public event Func<byte[], int, int, byte[], int> PreProcess;

		public void Connect(string url, bool immediateConnect, Action<ClientWebSocket> init = null)
		{
			var source = new CancellationTokenSource();

			_expectedDisconnect = false;
			_source = source;

			_resendCommands.Clear();

			_disconnectionStates[source] = _expectedDisconnect;

			_ws = new ClientWebSocket();
			init?.Invoke(_ws);
			_ws.ConnectAsync(new Uri(url), source.Token).Wait();

			if (immediateConnect)
				_connected.Invoke();

			ThreadingHelper.Thread(() => CultureInfo.InvariantCulture.DoInCulture(() => OnReceive(source))).Launch();
		}

		public bool IsConnected => _ws != null;

		public void Disconnect(bool expectedDisconnect = true)
		{
			if (_source == null)
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
							_disconnected(_disconnectionStates.TryGetAndRemove(source));

							try
							{
								_ws.Dispose();
							}
							catch { }

							_ws = null;
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
							continue;

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
				_disconnected(expected);

				if (!expected && AutoResend)
					Resend();
			}
			catch (Exception ex)
			{
				_error(ex);
			}
		}

		public void Send(object obj, bool resendIfDisconnect = false)
		{
			if (!(obj is byte[] sendBuf))
			{
				var json = obj as string ?? JsonConvert.SerializeObject(obj);
				_verboseLog("Send: '{0}'", json);

				sendBuf = Encoding.UTF8.GetBytes(json);
			}

			Send(sendBuf, WebSocketMessageType.Text, resendIfDisconnect);
		}

		public void Send(byte[] sendBuf, WebSocketMessageType type, bool resendIfDisconnect = false)
		{
			if (resendIfDisconnect)
			{
				_resendCommands.Add(Tuple.Create(sendBuf.ToArray(), type));
			}

			_ws.SendAsync(new ArraySegment<byte>(sendBuf), type, true, _source.Token).Wait();
		}

		public void Resend()
		{
			var resendCommands = _resendCommands.CopyAndClear();

			foreach (var resendCommand in resendCommands)
			{
				Send(resendCommand.Item1, resendCommand.Item2, true);
				ResendInterval.Sleep();
			}
		}

		protected override void DisposeManaged()
		{
			_source?.Cancel();

			base.DisposeManaged();
		}
	}
}