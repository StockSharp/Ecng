namespace Ecng.Net.Transport
{
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;

	using Ecng.Common;

	public class SocketClient
	{
		public SocketClient(EndPoint serverAddress, bool autoCloseConnection)
		{
			ServerAddress = serverAddress ?? throw new ArgumentNullException(nameof(serverAddress));
			AutoCloseConnection = autoCloseConnection;

			//Error = delegate { };
		}

		public EndPoint ServerAddress { get; }
		public bool AutoCloseConnection { get; }
		//public Action<Exception> Error;

		public void ProcessRequest(Stream request, Action<Stream> handler)
		{
			if (request is null)
				throw new ArgumentNullException(nameof(request));

			if (handler is null)
				throw new ArgumentNullException(nameof(handler));

			var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			var e = new SocketAsyncEventArgs { RemoteEndPoint = ServerAddress };
			e.Completed += (sender1, e1) =>
			{
				//try
				//{
				if (AutoCloseConnection)
					e.Dispose();

				using (var stream = new NetworkStream(socket, AutoCloseConnection))
				{
					var buffer = request.To<byte[]>();
					stream.Write(buffer, 0, buffer.Length);
					handler(stream);
				}
				//}
				//catch (Exception ex)
				//{
				//    Error(ex);
				//    throw;
				//}

				//e = new SocketAsyncEventArgs();
				//e.SetBuffer(request, 0, request.Length);
				//e.Completed += (sender2, e2) =>
				//{
				//    try
				//    {
				//        Stream stream = new NetworkStream(socket);

				//        if (AutoCloseConnection)
				//        {
				//            stream = new MemoryStream(stream.Read<byte[]>());
				//            socket.Close();
				//        }

				//        handler(stream);
				//    }
				//    catch (Exception ex)
				//    {
				//        Error(ex);
				//        throw;
				//    }
				//};

				//socket.SendAsync(e);
			};

			socket.ConnectAsync(e);
		}
	}
}