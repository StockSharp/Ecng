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
		private ClientWebSocket2 _ws;
		private CancellationTokenSource _source;
		private bool _expectedDisconnect;

		private readonly SynchronizedDictionary<CancellationTokenSource, bool> _disconnectionStates = new SynchronizedDictionary<CancellationTokenSource,bool>();

		private readonly SynchronizedList<Tuple<byte[], WebSocketMessageType>> _resendCommands = new SynchronizedList<Tuple<byte[], WebSocketMessageType>>();

		private readonly Action<Exception> _error;
		private readonly Action _connected;
		private readonly Action<bool> _disconnected;
		private readonly Action<WebSocketClient, string> _process;
		private readonly Action<WebSocketClient, byte[], int, int> _process2;
		private readonly Action<string, object> _infoLog;
		private readonly Action<string, object> _errorLog;
		private readonly Action<string, object> _verboseLog;
		private readonly Action<string> _verbose2Log;

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<string> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, (c, s) => process(s), infoLog, errorLog, verbose, verbose2)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, string> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
		{
			_connected = connected ?? throw new ArgumentNullException(nameof(connected));
			_disconnected = disconnected ?? throw new ArgumentNullException(nameof(disconnected));
			_error = error ?? throw new ArgumentNullException(nameof(error));
			_process = process ?? throw new ArgumentNullException(nameof(process));
			_infoLog = infoLog ?? throw new ArgumentNullException(nameof(infoLog));
			_errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
			_verboseLog = verbose ?? throw new ArgumentNullException(nameof(verbose));
			_verbose2Log = verbose2 ?? throw new ArgumentNullException(nameof(verbose2));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<byte[], int, int> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
			: this(connected, disconnected, error, (c, b, s, o) => process(b, s, o), infoLog, errorLog, verbose, verbose2)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));
		}

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<WebSocketClient, byte[], int, int> process,
			Action<string, object> infoLog, Action<string, object> errorLog, Action<string, object> verbose, Action<string> verbose2)
		{
			_connected = connected ?? throw new ArgumentNullException(nameof(connected));
			_disconnected = disconnected ?? throw new ArgumentNullException(nameof(disconnected));
			_error = error ?? throw new ArgumentNullException(nameof(error));
			_process2 = process ?? throw new ArgumentNullException(nameof(process));
			_infoLog = infoLog ?? throw new ArgumentNullException(nameof(infoLog));
			_errorLog = errorLog ?? throw new ArgumentNullException(nameof(errorLog));
			_verboseLog = verbose ?? throw new ArgumentNullException(nameof(verbose));
			_verbose2Log = verbose2 ?? throw new ArgumentNullException(nameof(verbose2));
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

		public event Func<byte[], int, Tuple<byte[], int>> PreProcess;

		public void Connect(string url, bool immediateConnect, Action<ClientWebSocket2> init = null)
		{
			_expectedDisconnect = false;
			_source = new CancellationTokenSource();

			_resendCommands.Clear();

			_disconnectionStates[_source] = _expectedDisconnect;

			_ws = new ClientWebSocket2();
			init?.Invoke(_ws);
			_ws.ConnectAsync(new Uri(url), _source.Token).Wait();
			
			if (immediateConnect)
				_connected.Invoke();

			ThreadingHelper.Thread(() => CultureInfo.InvariantCulture.DoInCulture(OnReceive)).Launch();
		}

		public void Disconnect(bool expectedDisconnect = true)
		{
			if (_source == null)
				throw new InvalidOperationException("Not connected.".Translate());

			_expectedDisconnect = expectedDisconnect;
			_disconnectionStates[_source] = _expectedDisconnect;
			_source.Cancel();
		}

		private void OnReceive()
		{
			try
			{
				var buf = new byte[1024 * 1024];
				var pos = 0;

				var errorCount = 0;
				const int maxParsingErrors = 100;
				const int maxNetworkErrors = 10;

				var source = _source;

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
							break;
						}

						pos += result.Count;

						if (!result.EndOfMessage)
							continue;

						if (pos == 0)
							continue;

						string recv = null;
						var pos2 = pos;

						try
						{
							if (_process != null)
							{
								var preProcess = PreProcess;

								if (preProcess != null)
								{
									var t = preProcess(buf, pos);
									buf = t.Item1;
									pos = t.Item2;
								}
							
								recv = Encoding.UTF8.GetString(buf, 0, pos);
								_verbose2Log(recv);
							}
						}
						finally
						{
							pos = 0;
						}

						try
						{
							if (_process != null)
								_process(this, recv);
							else
								_process2(this, buf, 0, pos2);

							errorCount = 0;
						}
						catch (Exception ex)
						{
							_error(new InvalidOperationException("Error parsing string '{0}'.".Translate().Put(recv), ex));
						
							if (++errorCount < maxParsingErrors)
								continue;

							_errorLog("Max parsing error {0} limit reached.".Translate(), maxParsingErrors);
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
					_ws.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, new CancellationToken()).Wait();
				}
				catch (Exception ex)
				{
					if (!source.IsCancellationRequested)
						_error(ex);
				}

				_ws.Dispose();
				_ws = null;

				var expected = _disconnectionStates.GetAndRemove(source);
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
			if (_ws != null)
				_source.Cancel();

			base.DisposeManaged();
		}
	}
}