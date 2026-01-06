namespace Ecng.Tests.Net;

using System.Net;
using System.Net.Sockets;

using Ecng.Net;

using Nito.AsyncEx;

/// <summary>
/// Mock UDP socket for testing.
/// </summary>
class MockUdpSocket : Disposable, IUdpSocket
{
	private readonly AsyncLock _lock = new();
	private readonly Queue<byte[]> _pendingPackets = new();
	private readonly List<byte[]> _receivedPackets = [];
	private TaskCompletionSource<bool> _packetAvailable = new();

	/// <summary>
	/// Gets whether the socket is bound.
	/// </summary>
	public bool IsBound { get; private set; }

	/// <summary>
	/// Gets the local endpoint the socket is bound to.
	/// </summary>
	public EndPoint BoundEndPoint { get; private set; }

	/// <summary>
	/// Gets the joined multicast addresses.
	/// </summary>
	public List<MulticastSourceAddress> JoinedMulticastGroups { get; } = [];

	/// <summary>
	/// Gets the socket options that were set.
	/// </summary>
	public Dictionary<(SocketOptionLevel, SocketOptionName), bool> SocketOptions { get; } = [];

	/// <summary>
	/// Gets or sets the delay before returning packets.
	/// </summary>
	public TimeSpan ReceiveDelay { get; set; } = TimeSpan.Zero;

	/// <summary>
	/// Gets or sets whether to throw an exception on receive.
	/// </summary>
	public Exception ReceiveException { get; set; }

	/// <inheritdoc />
	public void Bind(EndPoint localEP)
	{
		ThrowIfDisposed();
		BoundEndPoint = localEP;
		IsBound = true;
	}

	/// <inheritdoc />
	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
	{
		ThrowIfDisposed();
		SocketOptions[(optionLevel, optionName)] = optionValue;
	}

	/// <inheritdoc />
	public void JoinMulticast(MulticastSourceAddress address)
	{
		ThrowIfDisposed();
		JoinedMulticastGroups.Add(address);
	}

	/// <inheritdoc />
	public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();

		if (ReceiveException != null)
			throw ReceiveException;

		while (!cancellationToken.IsCancellationRequested)
		{
			using (await _lock.LockAsync(cancellationToken))
			{
				if (_pendingPackets.Count > 0)
				{
					var packet = _pendingPackets.Dequeue();
					_receivedPackets.Add(packet);
					packet.AsSpan().CopyTo(buffer.Span);

					if (ReceiveDelay > TimeSpan.Zero)
						await Task.Delay(ReceiveDelay, cancellationToken);

					return packet.Length;
				}

				// Reset the task completion source for next wait
				_packetAvailable = new TaskCompletionSource<bool>();
			}

			try
			{
				await _packetAvailable.Task.WaitAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
		}

		cancellationToken.ThrowIfCancellationRequested();
		return 0;
	}

	/// <inheritdoc />
	public async ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken)
	{
		var received = await ReceiveAsync(buffer, socketFlags, cancellationToken);

		return new()
		{
			ReceivedBytes = received,
			RemoteEndPoint = remoteEndPoint
		};
	}

	/// <summary>
	/// Gets or sets the remote endpoint to return from ReceiveFromAsync.
	/// </summary>
	public EndPoint RemoteEndPoint { get; set; }

	/// <inheritdoc />
	public void LeaveMulticast(MulticastSourceAddress address)
	{
		ThrowIfDisposed();
		JoinedMulticastGroups.RemoveAll(g =>
			g.GroupAddress?.Equals(address.GroupAddress) == true &&
			g.Port == address.Port);
	}

	/// <inheritdoc />
	public void LeaveMulticast(IPAddress groupAddress)
	{
		ThrowIfDisposed();
		JoinedMulticastGroups.RemoveAll(g => g.GroupAddress?.Equals(groupAddress) == true);
	}

	/// <summary>
	/// Enqueues a packet to be received by the socket.
	/// </summary>
	/// <param name="data">The packet data.</param>
	public void EnqueuePacket(byte[] data)
	{
		lock (_pendingPackets)
		{
			_pendingPackets.Enqueue(data);
			_packetAvailable.TrySetResult(true);
		}
	}

	/// <summary>
	/// Enqueues multiple packets to be received.
	/// </summary>
	/// <param name="packets">The packets to enqueue.</param>
	public void EnqueuePackets(IEnumerable<byte[]> packets)
	{
		lock (_pendingPackets)
		{
			foreach (var packet in packets)
				_pendingPackets.Enqueue(packet);

			_packetAvailable.TrySetResult(true);
		}
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (IsDisposed)
			return;

		base.DisposeManaged();
	}
}

/// <summary>
/// Factory for creating mock UDP sockets.
/// </summary>
class MockUdpSocketFactory : IUdpSocketFactory
{
	private readonly List<MockUdpSocket> _createdSockets = [];

	/// <summary>
	/// Gets the sockets created by this factory.
	/// </summary>
	public IReadOnlyList<MockUdpSocket> CreatedSockets => _createdSockets;

	/// <summary>
	/// Gets the next socket to be returned, if set.
	/// </summary>
	public MockUdpSocket NextSocket { get; set; }

	/// <inheritdoc />
	public IUdpSocket Create(AddressFamily addressFamily)
	{
		var socket = NextSocket ?? new MockUdpSocket();
		NextSocket = null;
		_createdSockets.Add(socket);
		return socket;
	}
}
