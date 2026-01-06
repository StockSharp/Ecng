namespace Ecng.Net;

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
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of packets with endpoint, payload, and timestamp.</returns>
	IAsyncEnumerable<(IPEndPoint EndPoint, Memory<byte> Payload, DateTime PacketTime)> GetPacketsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory packet source for testing.
/// </summary>
/// <remarks>
/// Initializes a new instance.
/// </remarks>
/// <param name="packets">The packets to provide.</param>
public class MemoryPacketSource(IReadOnlyList<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)> packets) : IUdpPacketSource
{
	private readonly IReadOnlyList<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)> _packets = packets ?? throw new ArgumentNullException(nameof(packets));

	/// <inheritdoc />
	public IAsyncEnumerable<(IPEndPoint EndPoint, Memory<byte> Payload, DateTime PacketTime)> GetPacketsAsync(CancellationToken cancellationToken = default)
	{
		return Enumerate(_packets, cancellationToken);

		static async IAsyncEnumerable<(IPEndPoint, Memory<byte>, DateTime)> Enumerate(
			IReadOnlyList<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)> packets,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			foreach (var packet in packets)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return (packet.EndPoint, packet.Payload, packet.PacketTime);
			}

			await Task.CompletedTask; // Satisfy async requirement
		}
	}
}
