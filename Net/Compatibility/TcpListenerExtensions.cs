#if NETSTANDARD2_0
namespace System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="TcpListener"/>.
/// </summary>
public static class TcpListenerExtensions
{
	/// <summary>
	/// Accepts a pending connection request as an asynchronous operation.
	/// </summary>
	/// <param name="listener">The TCP listener.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The value of the task parameter contains the <see cref="TcpClient"/> used to send and receive data.</returns>
	public static async Task<TcpClient> AcceptTcpClientAsync(this TcpListener listener, CancellationToken cancellationToken)
	{
		if (listener is null)
			throw new ArgumentNullException(nameof(listener));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => listener.Stop()))
		{
			try
			{
				return await listener.AcceptTcpClientAsync().NoWait();
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}
}
#endif
