namespace Ecng.Net
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Net.WebSockets;
	using System.Text;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Localization;

	using Newtonsoft.Json;

	public class WebSocketClient : Disposable
	{
		private ClientWebSocket _ws;
		private CancellationTokenSource _source = new CancellationTokenSource();
		private bool _expectedDisconnect;

		private readonly Action<Exception> _error;
		private readonly Action _connected;
		private readonly Action<bool> _disconnected;
		private readonly Action<string> _process;
		private readonly Action<string, object> _infoLog;
		private readonly Action<string, object> _errorLog;
		private readonly Action<string, object> _verboseLog;
		private readonly Action<string> _verbose2Log;

		public WebSocketClient(Action connected, Action<bool> disconnected, Action<Exception> error, Action<string> process,
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

		public event Func<byte[], int, Tuple<byte[], int>> PreProcess;

		public void Connect(string url, bool immediateConnect, Action<ClientWebSocket> init = null)
		{
			_expectedDisconnect = false;

			_ws = new ClientWebSocket();
			init?.Invoke(_ws);
			_ws.ConnectAsync(new Uri(url), _source.Token).Wait();
			
			if (immediateConnect)
				_connected.Invoke();

			ThreadingHelper.Thread(() => CultureInfo.InvariantCulture.DoInCulture(OnReceive)).Launch();
		}

		public void Disconnect(bool expectedDisconnect = true)
		{
			_expectedDisconnect = expectedDisconnect;

			_source.Cancel();
			_source = new CancellationTokenSource();
		}

		private void OnReceive()
		{
			try
			{
				var buf = new byte[1024 * 1024];
				var pos = 0;

				var errorCount = 0;
				const int maxErrorCount = 10;

				var source = _source;

				while (!source.IsCancellationRequested)
				{
					string recv = null;

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

							_infoLog("Socket closed with status {0}.", result.CloseStatus);
							break;
						}

						pos += result.Count;

						if (!result.EndOfMessage)
							continue;

						var preProcess = PreProcess;

						if (preProcess != null)
						{
							var t = preProcess(buf, pos);
							buf = t.Item1;
							pos = t.Item2;
						}

						recv = Encoding.UTF8.GetString(buf, 0, pos);
						_verbose2Log(recv);

						pos = 0;

						_process(recv);

						errorCount = 0;
					}
					catch (AggregateException ex)
					{
						if (!source.IsCancellationRequested)
							_error(ex);

						if (ex.InnerExceptions.FirstOrDefault() is WebSocketException)
							break;

						if (++errorCount < maxErrorCount)
							continue;

						_errorLog("Max error {0} limit reached.", maxErrorCount);
						break;
					}
					catch (Exception ex)
					{
						_error(new InvalidOperationException("Error parsing string '{0}'.".Translate().Put(recv), ex));
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

				_disconnected(_expectedDisconnect);
			}
			catch (Exception ex)
			{
				_error(ex);
			}
		}

		public void Send(object obj)
		{
			var json = obj as string ?? JsonConvert.SerializeObject(obj);
			_verboseLog("Send: '{0}'", json);

			var sendBuf = Encoding.UTF8.GetBytes(json);
			_ws.SendAsync(new ArraySegment<byte>(sendBuf), WebSocketMessageType.Text, true, _source.Token).Wait();
		}

		protected override void DisposeManaged()
		{
			if (_ws != null)
				_source.Cancel();

			base.DisposeManaged();
		}
	}
}