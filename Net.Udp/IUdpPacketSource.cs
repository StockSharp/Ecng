namespace Ecng.Net.Udp;

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for providing UDP packets for replay or testing.
/// </summary>
public interface IUdpPacketSource
{
	/// <summary>
	/// Gets packets asynchronously.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>Async enumerable of packets with endpoint, payload, and timestamp.</returns>
	IAsyncEnumerable<(IPEndPoint EndPoint, Memory<byte> Payload, DateTime PacketTime)> GetPacketsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// In-memory packet source for testing.
/// </summary>
public class MemoryPacketSource : IUdpPacketSource
{
	private readonly IReadOnlyList<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)> _packets;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="packets">The packets to provide.</param>
	public MemoryPacketSource(IReadOnlyList<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)> packets)
	{
		_packets = packets ?? throw new ArgumentNullException(nameof(packets));
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<(IPEndPoint EndPoint, Memory<byte> Payload, DateTime PacketTime)> GetPacketsAsync(
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		foreach (var packet in _packets)
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return (packet.EndPoint, packet.Payload, packet.PacketTime);
		}

		await Task.CompletedTask;
	}
}
