namespace Ecng.Net
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Net.Transport;

	public abstract class SocketBehaviorClient<TId> : Disposable
	{
		private readonly SocketClient _socketClient;
		private readonly byte[] _ticket;
		private readonly Func<SocketBehaviorClient<TId>, object, IEnumerable<byte>> _argsSerializer;

		protected SocketBehaviorClient(EndPoint serverAddress, byte[] ticket, Func<SocketBehaviorClient<TId>, object, IEnumerable<byte>> argsSerializer)
		{
			if (serverAddress == null)
				throw new ArgumentNullException("serverAddress");

			if (ticket == null)
				throw new ArgumentNullException("ticket");

			if (argsSerializer == null)
				throw new ArgumentNullException("argsSerializer");

			_socketClient = new SocketClient(serverAddress, true);
			_ticket = ticket;
			_argsSerializer = argsSerializer;
		}

		protected void ProcessRequest(TId id, IEnumerable<object> requestArgs, Action<Stream> handler)
		{
			System.Diagnostics.Debug.WriteLine("client call: " + id);

			if (requestArgs == null)
				throw new ArgumentNullException("requestArgs");

			if (handler == null)
				throw new ArgumentNullException("handler");

			var requestData = _ticket.Concat(id.To<byte[]>());
			foreach (var requestArg in requestArgs)
				requestData = requestData.Concat(_argsSerializer(this, requestArg));

			_socketClient.ProcessRequest(requestData.ToArray(), handler);
		}
	}
}