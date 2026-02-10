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
/// Defines behavior when the packet queue is full.
/// </summary>
public enum PacketQueueFullModes
{
	/// <summary>
	/// Drop the newest (incoming) packet when queue is full.
	/// </summary>
	DropNewest,

	/// <summary>
	/// Drop the oldest packet in queue to make room for the new one.
	/// </summary>
	DropOldest,

	/// <summary>
	/// Wait (block the sender) until space becomes available in the queue.
	/// </summary>
	Wait,
}

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
	/// <param name="fullMode">Behavior when the packet queue is full.</param>
	/// <returns>The packet receiver.</returns>
	IPacketReceiver Create(IPacketProcessor processor, MulticastSourceAddress address, ILogReceiver logs, PacketQueueFullModes fullMode);
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
/// <param name="fullMode">Behavior when the packet queue is full.</param>
public class RealPacketReceiver(
	IPacketProcessor processor,
	MulticastSourceAddress address,
	IUdpSocketFactory socketFactory,
	ILogReceiver logs,
	PacketQueueFullModes fullMode) : Disposable, IPacketReceiver
{
	private readonly IPacketProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));
	private readonly MulticastSourceAddress _address = address ?? throw new ArgumentNullException(nameof(address));
	private readonly IUdpSocketFactory _socketFactory = socketFactory ?? throw new ArgumentNullException(nameof(socketFactory));
    private readonly ILogReceiver _logs = logs ?? throw new ArgumentNullException(nameof(logs));
	private readonly PacketQueueFullModes _fullMode = fullMode;
	private Channel<(IMemoryOwner<byte> packet, int length)> _packetsQueue;
	private int _dropped;

	/// <inheritdoc />
	public Task RunAsync(CancellationToken token)
	{
		_packetsQueue = Channel.CreateBounded<(IMemoryOwner<byte>, int)>(new BoundedChannelOptions(_processor.MaxIncomingQueueSize)
		{
			SingleReader = _fullMode != PacketQueueFullModes.DropOldest,
			SingleWriter = true,
			FullMode = BoundedChannelFullMode.Wait
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
			// stop the receiver so it doesn't keep allocating packets
			_packetsQueue.Writer.TryComplete();

			// drain remaining packets from the channel
			var drained = 0;
			while (reader.TryRead(out var remaining))
			{
				remaining.packet.Dispose();
				drained++;
			}

			if (drained > 0)
				_logs.LogInfo("Drained {0} packets from channel.", drained);

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
		var writer = _packetsQueue.Writer;
		var reader = _packetsQueue.Reader;

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

			while (!token.IsCancellationRequested && !reader.Completion.IsCompleted)
			{
				IMemoryOwner<byte> packet = null;

				try
				{
					packet = _processor.AllocatePacket(packetSize);
					var len = await socket.ReceiveAsync(packet.Memory, SocketFlags.None, token);

					if (len <= 0)
					{
						_logs.LogError($"{_address} returned 0 bytes.");
						packet.Dispose();
						packet = null;
						break;
					}

					if (reader.Completion.IsCompleted)
					{
						_processor.DisposePacket(packet, "writer completed");
						packet = null;
						break;
					}

					if (_fullMode == PacketQueueFullModes.Wait)
					{
						await writer.WriteAsync((packet, len), token);
					}
					else if (!writer.TryWrite((packet, len)))
					{
						_dropped++;

						if (_fullMode == PacketQueueFullModes.DropOldest && reader.TryRead(out var oldest))
						{
							_processor.DisposePacket(oldest.packet, "queue overflow (oldest)");
							writer.TryWrite((packet, len));
						}
						else
						{
							_processor.DisposePacket(packet, "queue limit");
						}

						if (_dropped % 1000 == 0)
							_logs.LogWarning("Dropped packets total={0}", _dropped);
					}

					packet = null;
					errorCount = 0;
				}
				catch (Exception ex)
				{
					packet?.Dispose();

					if (!token.IsCancellationRequested)
						_processor.ErrorHandler(ex, ++errorCount, false);
				}
			}
		}
		finally
		{
			writer.TryComplete();
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
	public IPacketReceiver Create(IPacketProcessor processor, MulticastSourceAddress address, ILogReceiver logs, PacketQueueFullModes fullMode)
		=> new RealPacketReceiver(processor, address, _socketFactory, logs, fullMode);
}
