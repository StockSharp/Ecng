namespace Ecng.Net;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

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
/// Default implementation using real <see cref="Socket"/>.
/// </summary>
public class RealUdpSocket : Disposable, IUdpSocket
{
	private readonly Socket _socket;

	/// <summary>
	/// Initializes a new instance for IPv4.
	/// </summary>
	public RealUdpSocket()
		: this(AddressFamily.InterNetwork)
	{
	}

	/// <summary>
	/// Initializes a new instance with specified address family.
	/// </summary>
	/// <param name="addressFamily">The address family (IPv4 or IPv6).</param>
	public RealUdpSocket(AddressFamily addressFamily)
	{
		if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
			throw new ArgumentOutOfRangeException(nameof(addressFamily), "Only IPv4 and IPv6 are supported");

		_socket = new(addressFamily, SocketType.Dgram, ProtocolType.Udp);
	}

	/// <inheritdoc />
	public void Bind(EndPoint localEP)
	{
		ThrowIfDisposed();
		_socket.Bind(localEP);
	}

	/// <inheritdoc />
	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
	{
		ThrowIfDisposed();
		_socket.SetSocketOption(optionLevel, optionName, optionValue);
	}

	/// <inheritdoc />
	public void JoinMulticast(MulticastSourceAddress address)
	{
		ThrowIfDisposed();
		_socket.JoinMulticast(address);
	}

	/// <inheritdoc />
	public void LeaveMulticast(MulticastSourceAddress address)
	{
		ThrowIfDisposed();
		_socket.LeaveMulticast(address);
	}

	/// <inheritdoc />
	public void LeaveMulticast(IPAddress groupAddress)
	{
		ThrowIfDisposed();
		_socket.LeaveMulticast(groupAddress);
	}

	/// <inheritdoc />
	public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		return _socket.ReceiveAsync(buffer, socketFlags, cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
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
