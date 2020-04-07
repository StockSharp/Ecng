namespace Ecng.Net.Transport
{
	using System;
	using System.IO.Pipes;

	using Ecng.Common;
	using Ecng.Serialization;

	public class PipeClient : Disposable
	{
		private readonly NamedPipeClientStream _core;

		public PipeClient(string pipeName)
		{
			_core = new NamedPipeClientStream(pipeName);
			_core.Connect();
		}

		public void Send<T>(T value)
		{
			_core.WriteEx(value);
		}

		public void SendAsync(byte[] buffer)
		{
			_core.BeginWrite(buffer, 0, buffer.Length, _core.EndWrite, null);
		}

		public T Receive<T>()
		{
			return _core.Read<T>();
		}

		public void ReceiveAsync<T>(Action<T> handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			throw new NotImplementedException();
		}

		protected override void DisposeManaged()
		{
			_core.Dispose();
			base.DisposeManaged();
		}
	}
}