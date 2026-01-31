namespace Ecng.Net;

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Logging;

/// <summary>
/// Interface for receiving UDP packets.
/// </summary>
public interface IPacketReceiver : IDisposable
{
	/// <summary>
	/// Runs the packet receiver asynchronously.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Factory for creating packet receivers.
/// </summary>
public interface IPacketReceiverFactory
{
	/// <summary>
	/// Creates a packet receiver.
	/// </summary>
	/// <param name="processor">The packet processor.</param>
	/// <param name="address">The multicast address.</param>
	/// <param name="logs">The log receiver to use.</param>
	/// <returns>The packet receiver.</returns>
	IPacketReceiver Create(IPacketProcessor processor, MulticastSourceAddress address, ILogReceiver logs);
}

/// <summary>
/// Default packet receiver using real UDP sockets.
/// </summary>
/// <remarks>
/// Initializes a new instance.
/// </remarks>
/// <param name="processor">The packet processor.</param>
/// <param name="address">The multicast address.</param>
/// <param name="socketFactory">The socket factory.</param>
/// <param name="logs">The log receiver to use.</param>
public class RealPacketReceiver(
	IPacketProcessor processor,
	MulticastSourceAddress address,
	IUdpSocketFactory socketFactory,
	ILogReceiver logs) : Disposable, IPacketReceiver
{
	private readonly IPacketProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
	private readonly MulticastSourceAddress _address = address ?? throw new ArgumentNullException(nameof(address));
	private readonly IUdpSocketFactory _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
    private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));
	private Channel<(IMemoryOwner<byte> packet, int length)> _packetsQueue;
	private int _dropped;

	/// <inheritdoc />
	public Task RunAsync(CancellationToken token)
	{
		_packetsQueue = Channel.CreateBounded<(IMemoryOwner<byte>, int)>(new BoundedChannelOptions(_processor.MaxIncomingQueueSize)
		{
			SingleReader = true,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.DropOldest
		});

		var rt = RunThread(ReceivePackets, _processor.Name + "-receiver", token);
		var pt = RunThread(ProcessPackets, _processor.Name + "-processor", token);

		return Task.WhenAll(rt, pt);
	}

	private async Task RunThread(Func<CancellationToken, Task> action, string name, CancellationToken token)
	{
		try
		{
			_logs.LogInfo("Thread {0} started.", name);
			await action(token);
		}
		catch (Exception ex)
		{
			if (!token.IsCancellationRequested)
				_logs.LogError(ex);
			else
				throw;
		}
		finally
		{
			_logs.LogInfo("Thread {0} stopped.", name);
		}
	}

	private async Task ProcessPackets(CancellationToken token)
	{
		var errorCount = 0;
		var reader = _packetsQueue.Reader;

		try
		{
			await foreach (var (packet, length) in reader.ReadAllAsync(token).WithEnforcedCancellation(token))
			{
				try
				{
					if (!await _processor.ProcessNewPacket(packet, length, token))
						break;

					errorCount = 0;
				}
				catch (Exception ex)
				{
					if (!token.IsCancellationRequested)
						_processor.ErrorHandler(ex, ++errorCount, false);
				}
			}
		}
		catch (Exception ex)
		{
			if (!token.IsCancellationRequested)
				_logs.LogError(ex);
		}
		finally
		{
			_logs.LogInfo("Ending packet processor.");
		}
	}

	/// <inheritdoc/>
	protected override void DisposeManaged()
	{
		base.DisposeManaged();
		_packetsQueue?.Writer.TryComplete();
	}

	private async Task ReceivePackets(CancellationToken token)
	{
		var addressFamily = _address.GroupAddress.AddressFamily;
		using var socket = _socketFactory.Create(addressFamily);

		try
		{
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			_logs.LogInfo("{0} joining...", _address);

			var bindAddress = addressFamily == AddressFamily.InterNetworkV6
				? IPAddress.IPv6Any
				: IPAddress.Any;
			socket.Bind(new IPEndPoint(bindAddress, _address.Port));
			socket.JoinMulticast(_address);

			var errorCount = 0;
			var packetSize = _processor.MaxUdpDatagramSize;
			var writer = _packetsQueue.Writer;

			while (!token.IsCancellationRequested)
			{
				try
				{
					var packet = _processor.AllocatePacket(packetSize);
					var len = await socket.ReceiveAsync(packet.Memory, SocketFlags.None, token);

					if (len <= 0)
					{
						_logs.LogError($"{_address} returned 0 bytes.");
						packet.Dispose();
						break;
					}

					if (!writer.TryWrite((packet, len)))
					{
						_dropped++;
						_processor.DisposePacket(packet, "queue limit");

						if (_dropped % 1000 == 0)
							_logs.LogWarning("Dropped packets total={0}", _dropped);
					}

					errorCount = 0;
				}
				catch (Exception ex)
				{
					if (!token.IsCancellationRequested)
						_processor.ErrorHandler(ex, ++errorCount, false);
				}
			}
		}
		finally
		{
			_packetsQueue.Writer.TryComplete();
			_logs.LogInfo("{0} leaving...", _address);
		}
	}
}

/// <summary>
/// Default factory creating real packet receivers.
/// </summary>
/// <remarks>
/// Initializes a new instance.
/// </remarks>
/// <param name="socketFactory">The socket factory to use.</param>
public class RealPacketReceiverFactory(IUdpSocketFactory socketFactory) : IPacketReceiverFactory
{
	private readonly IUdpSocketFactory _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));

	/// <inheritdoc />
	public IPacketReceiver Create(IPacketProcessor processor, MulticastSourceAddress address, ILogReceiver logs)
		=> new RealPacketReceiver(processor, address, _socketFactory, logs);
}
