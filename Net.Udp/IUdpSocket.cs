namespace Ecng.Net.Udp;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Net;

/// <summary>
/// Result of ReceiveFromAsync operation.
/// </summary>
public readonly struct SocketReceiveFromResult
{
	/// <summary>
	/// The number of bytes received.
	/// </summary>
	public int ReceivedBytes { get; init; }

	/// <summary>
	/// The remote endpoint from which the data was received.
	/// </summary>
	public EndPoint RemoteEndPoint { get; init; }
}

/// <summary>
/// Interface for UDP socket operations, enabling testing without real network.
/// </summary>
public interface IUdpSocket : IDisposable
{
	/// <summary>
	/// Binds the socket to the specified endpoint.
	/// </summary>
	/// <param name="localEP">The local endpoint.</param>
	void Bind(EndPoint localEP);

	/// <summary>
	/// Sets a socket option.
	/// </summary>
	/// <param name="optionLevel">The option level.</param>
	/// <param name="optionName">The option name.</param>
	/// <param name="optionValue">The option value.</param>
	void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue);

	/// <summary>
	/// Joins a multicast group.
	/// </summary>
	/// <param name="address">The multicast source address.</param>
	void JoinMulticast(MulticastSourceAddress address);

	/// <summary>
	/// Leaves a multicast group.
	/// </summary>
	/// <param name="address">The multicast source address.</param>
	void LeaveMulticast(MulticastSourceAddress address);

	/// <summary>
	/// Leaves a multicast group by IP address only.
	/// </summary>
	/// <param name="groupAddress">The multicast group address.</param>
	void LeaveMulticast(IPAddress groupAddress);

	/// <summary>
	/// Receives data asynchronously.
	/// </summary>
	/// <param name="buffer">The buffer to receive data into.</param>
	/// <param name="socketFlags">The socket flags.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The number of bytes received.</returns>
	ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken);

	/// <summary>
	/// Receives data asynchronously and returns the remote endpoint.
	/// </summary>
	/// <param name="buffer">The buffer to receive data into.</param>
	/// <param name="socketFlags">The socket flags.</param>
	/// <param name="remoteEndPoint">The remote endpoint to receive from.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The result containing received bytes and remote endpoint.</returns>
	ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken);
}

/// <summary>
/// Factory for creating UDP sockets.
/// </summary>
public interface IUdpSocketFactory
{
	/// <summary>
	/// Creates a new UDP socket.
	/// </summary>
	/// <returns>The UDP socket.</returns>
	IUdpSocket Create();
}

/// <summary>
/// Default implementation using real <see cref="Socket"/>.
/// </summary>
public class RealUdpSocket : Disposable, IUdpSocket
{
	private readonly Socket _socket;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	public RealUdpSocket()
	{
		_socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
	}

	/// <inheritdoc />
	public void Bind(EndPoint localEP) => _socket.Bind(localEP);

	/// <inheritdoc />
	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
		=> _socket.SetSocketOption(optionLevel, optionName, optionValue);

	/// <inheritdoc />
	public void JoinMulticast(MulticastSourceAddress address) => _socket.JoinMulticast(address);

	/// <inheritdoc />
	public void LeaveMulticast(MulticastSourceAddress address) => _socket.LeaveMulticast(address);

	/// <inheritdoc />
	public void LeaveMulticast(IPAddress groupAddress) => _socket.LeaveMulticast(groupAddress);

	/// <inheritdoc />
	public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
		=> _socket.ReceiveAsync(buffer, socketFlags, cancellationToken);

	/// <inheritdoc />
	public async ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		var result = await _socket.ReceiveFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);

		return new()
		{
			ReceivedBytes = result.ReceivedBytes,
			RemoteEndPoint = result.RemoteEndPoint
		};
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_socket.Dispose();
		base.DisposeManaged();
	}
}

/// <summary>
/// Default factory creating real sockets.
/// </summary>
public class RealUdpSocketFactory : IUdpSocketFactory
{
	/// <inheritdoc />
	public IUdpSocket Create() => new RealUdpSocket();
}