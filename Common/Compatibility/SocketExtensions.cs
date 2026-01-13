#if NETSTANDARD2_0
namespace System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="Socket"/>.
/// </summary>
public static class SocketExtensions
{
	/// <summary>
	/// Establishes a connection to a remote host.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="remoteEP">An EndPoint that represents the remote device.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));
		if (remoteEP is null)
			throw new ArgumentNullException(nameof(remoteEP));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => CloseSocket(socket)))
		{
			try
			{
				var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				var args = new SocketAsyncEventArgs { RemoteEndPoint = remoteEP };
				args.Completed += (s, e) =>
				{
					if (e.SocketError == SocketError.Success)
						tcs.TrySetResult(true);
					else
						tcs.TrySetException(new SocketException((int)e.SocketError));
					e.Dispose();
				};

				if (!socket.ConnectAsync(args))
				{
					// Completed synchronously
					if (args.SocketError == SocketError.Success)
						tcs.TrySetResult(true);
					else
						tcs.TrySetException(new SocketException((int)args.SocketError));
					args.Dispose();
				}

				await tcs.Task.NoWait();
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Establishes a connection to a remote host.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="address">The IP address of the remote host.</param>
	/// <param name="port">The port number of the remote host.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static Task ConnectAsync(this Socket socket, IPAddress address, int port, CancellationToken cancellationToken)
	{
		return socket.ConnectAsync(new IPEndPoint(address, port), cancellationToken);
	}

	/// <summary>
	/// Establishes a connection to a remote host.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="host">The host name of the remote host.</param>
	/// <param name="port">The port number of the remote host.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public static async Task ConnectAsync(this Socket socket, string host, int port, CancellationToken cancellationToken)
	{
		if (host is null)
			throw new ArgumentNullException(nameof(host));

		var addresses = await Dns.GetHostAddressesAsync(host).NoWait();
		if (addresses.Length == 0)
			throw new SocketException((int)SocketError.HostNotFound);

		await socket.ConnectAsync(addresses[0], port, cancellationToken).NoWait();
	}

	/// <summary>
	/// Sends data on a connected socket.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="socketFlags">A bitwise combination of the SocketFlags values.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The value contains the number of bytes sent.</returns>
	public static async Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => CloseSocket(socket)))
		{
			try
			{
				return await socket.SendAsync(buffer, socketFlags).NoWait();
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Sends data on a connected socket.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="buffer">The buffer containing the data to send.</param>
	/// <param name="socketFlags">A bitwise combination of the SocketFlags values.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The value contains the number of bytes sent.</returns>
	public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			return await socket.SendAsync(segment, socketFlags, cancellationToken).NoWait();
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				buffer.Span.CopyTo(tempBuffer);
				return await socket.SendAsync(new ArraySegment<byte>(tempBuffer, 0, buffer.Length), socketFlags, cancellationToken).NoWait();
			}
			finally
			{
				System.Buffers.ArrayPool<byte>.Shared.Return(tempBuffer);
			}
		}
	}

	/// <summary>
	/// Receives data from a connected socket.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="buffer">The buffer for the received data.</param>
	/// <param name="socketFlags">A bitwise combination of the SocketFlags values.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The value contains the number of bytes received.</returns>
	public static async Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		cancellationToken.ThrowIfCancellationRequested();

		using (cancellationToken.Register(() => CloseSocket(socket)))
		{
			try
			{
				return await socket.ReceiveAsync(buffer, socketFlags).NoWait();
			}
			catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
			catch (SocketException) when (cancellationToken.IsCancellationRequested)
			{
				throw new OperationCanceledException(cancellationToken);
			}
		}
	}

	/// <summary>
	/// Receives data from a connected socket.
	/// </summary>
	/// <param name="socket">The socket.</param>
	/// <param name="buffer">The buffer for the received data.</param>
	/// <param name="socketFlags">A bitwise combination of the SocketFlags values.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
	/// <returns>A task that represents the asynchronous operation. The value contains the number of bytes received.</returns>
	public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (socket is null)
			throw new ArgumentNullException(nameof(socket));

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			return await socket.ReceiveAsync(segment, socketFlags, cancellationToken).NoWait();
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				var bytesRead = await socket.ReceiveAsync(new ArraySegment<byte>(tempBuffer, 0, buffer.Length), socketFlags, cancellationToken).NoWait();
				tempBuffer.AsSpan(0, bytesRead).CopyTo(buffer.Span);
				return bytesRead;
			}
			finally
			{
				System.Buffers.ArrayPool<byte>.Shared.Return(tempBuffer);
			}
		}
	}

	private static void CloseSocket(Socket socket)
	{
		try
		{
			socket.Dispose();
		}
		catch
		{
			// Ignore errors during cancellation cleanup
		}
	}
}
#endif
