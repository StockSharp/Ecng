namespace Ecng.Net;

using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for packet processor that handles received packets.
/// </summary>
public interface IPacketProcessor
{
	/// <summary>
	/// Gets or sets the maximum incoming queue size.
	/// </summary>
	int MaxIncomingQueueSize { get; }

	/// <summary>
	/// Gets or sets the maximum UDP datagram size.
	/// </summary>
	int MaxUdpDatagramSize { get; }

	/// <summary>
	/// Gets or sets the client name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Processes a new packet.
	/// </summary>
	/// <param name="packet">The packet data.</param>
	/// <param name="length">The packet length.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True to continue processing, false to stop.</returns>
	ValueTask<bool> ProcessNewPacket(IMemoryOwner<byte> packet, int length, CancellationToken cancellationToken);

	/// <summary>
	/// Allocates a packet buffer.
	/// </summary>
	/// <param name="size">The size of the buffer.</param>
	/// <returns>The allocated memory.</returns>
	IMemoryOwner<byte> AllocatePacket(int size);

	/// <summary>
	/// Disposes the packet with the specified reason.
	/// </summary>
	/// <param name="packet">The packet.</param>
	/// <param name="reason">The reason for disposal.</param>
	void DisposePacket(IMemoryOwner<byte> packet, string reason);

	/// <summary>
	/// Handles errors that occur during packet processing.
	/// </summary>
	/// <param name="ex">The exception.</param>
	/// <param name="errorCount">The current error count.</param>
	/// <param name="isFatal">Whether the error is fatal.</param>
	void ErrorHandler(Exception ex, int errorCount, bool isFatal);
}
