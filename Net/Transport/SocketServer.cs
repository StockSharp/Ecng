namespace Ecng.Net.Transport
{
	using System;
	using System.Net;
	using System.Net.Sockets;

	using Ecng.Common;

	public class SocketServer : Disposable
	{
		private readonly Action<TcpClient> _handler;
		private readonly Action<Exception> _error;
		private readonly TcpListener _listener;

		public SocketServer(IPAddress address, int port, Action<TcpClient> handler, Action<Exception> error)
		{
			_handler = handler ?? throw new ArgumentNullException(nameof(handler));
			_error = error ?? throw new ArgumentNullException(nameof(error));

			_listener = new TcpListener(address, port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(OnAccept, null);
		}

		public void OnAccept(IAsyncResult result)
		{
			try
			{
				var client = _listener.EndAcceptTcpClient(result);
				_handler(client);
			}
			catch (Exception ex)
			{
				_error(ex);
			}

			_listener.BeginAcceptTcpClient(OnAccept, null);
		}

		protected override void DisposeManaged()
		{
			_listener.Stop();
			base.DisposeManaged();
		}
	}
}