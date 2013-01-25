namespace Ecng.Net.Transport
{
	using System;
	using System.IO;
	using System.IO.Pipes;
	using System.Security.AccessControl;

	using Ecng.Common;

	public class PipeServer : Disposable
	{
		private readonly NamedPipeServerStream _pipe;

		public PipeServer(string pipeName, string userName, Action<Stream> handler, Action<Exception> error)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			if (error == null)
				throw new ArgumentNullException("error");

			PipeSecurity security;

			// http://social.msdn.microsoft.com/forums/en-US/netfxbcl/thread/5966ab37-afec-4b96-8106-4de0fbc70040
			using (var seedPipe = new NamedPipeServerStream("seedPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 1000, 1000))
			{
				// just get a copy of the security roles and close this pipe.
				security = seedPipe.GetAccessControl();
			}

			security.AddAccessRule(new PipeAccessRule(userName, PipeAccessRights.ReadWrite, AccessControlType.Allow));

			_pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 0, 0, security);

			((Action)delegate
			{
				while (!IsDisposed)
				{
					_pipe.WaitForConnection();
					handler(_pipe);
					_pipe.Disconnect();
				}
			}).DoAsync(error);
		}

		protected override void DisposeManaged()
		{
			_pipe.Dispose();
			base.DisposeManaged();
		}
	}
}