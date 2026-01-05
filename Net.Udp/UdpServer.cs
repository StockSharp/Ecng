namespace Ecng.Net.Udp;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// UDP server that relays packets from a source to multicast groups.
/// </summary>
public class UdpServer : IDisposable
{
	private readonly Dictionary<IPEndPoint, UdpClient> _clients = new();
	private bool _isDisposed;

	/// <summary>
	/// Replays packets from the source to the configured multicast groups.
	/// </summary>
	/// <param name="multicastGroups">Dictionary of multicast endpoints to packet loss probability (0-1).</param>
	/// <param name="packetSource">The source of packets to replay.</param>
	/// <param name="speedMultiplier">Speed multiplier (1 = real-time, 2 = 2x speed, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async ValueTask ReplayAsync(
		Dictionary<IPEndPoint, double> multicastGroups,
		IUdpPacketSource packetSource,
		int speedMultiplier,
		CancellationToken cancellationToken)
	{
		if (multicastGroups is null)
			throw new ArgumentNullException(nameof(multicastGroups));
		if (multicastGroups.Count == 0)
			throw new ArgumentOutOfRangeException(nameof(multicastGroups));
		if (packetSource is null)
			throw new ArgumentNullException(nameof(packetSource));
		if (speedMultiplier <= 0)
			throw new ArgumentOutOfRangeException(nameof(speedMultiplier));

		var clients = new Dictionary<IPEndPoint, (UdpClient client, double probability)>();

		foreach (var entry in multicastGroups)
		{
			var packetLossProbability = entry.Value;

			if (packetLossProbability < 0 || packetLossProbability > 1)
				throw new ArgumentOutOfRangeException(nameof(multicastGroups), $"Packet loss probability must be between 0 and 1, got {packetLossProbability}");

			var client = new UdpClient();

			// Only join multicast group for multicast addresses
			if (IsMulticastAddress(entry.Key.Address))
				client.JoinMulticastGroup(entry.Key.Address);

			clients[entry.Key] = (client, packetLossProbability);
			_clients[entry.Key] = client;
		}

		DateTime? lastPacketTime = null;
		var random = new Random();

		await foreach (var (targetDest, payload, packetTime) in packetSource.GetPacketsAsync(cancellationToken))
		{
			if (!clients.TryGetValue(targetDest, out var t))
				continue;

			// Simulate packet loss
			if (random.NextDouble() < t.probability)
				continue;

			// Simulate timing
			if (lastPacketTime.HasValue)
			{
				var delay = (packetTime - lastPacketTime.Value).TotalMilliseconds / speedMultiplier;

				if (delay > 0)
					await Task.Delay((int)delay, cancellationToken);
			}

			lastPacketTime = packetTime;
			await t.client.SendAsync(payload, targetDest, cancellationToken);
		}
	}

	/// <summary>
	/// Sends a single packet to the specified endpoint.
	/// </summary>
	/// <param name="endpoint">The target endpoint.</param>
	/// <param name="data">The data to send.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async ValueTask SendAsync(IPEndPoint endpoint, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		if (endpoint is null)
			throw new ArgumentNullException(nameof(endpoint));

		if (!_clients.TryGetValue(endpoint, out var client))
		{
			client = new UdpClient();

			// Only join multicast group for multicast addresses
			if (IsMulticastAddress(endpoint.Address))
				client.JoinMulticastGroup(endpoint.Address);

			_clients[endpoint] = client;
		}

		await client.SendAsync(data, endpoint, cancellationToken);
	}

	private static bool IsMulticastAddress(System.Net.IPAddress address)
	{
		var bytes = address.GetAddressBytes();
		// IPv4 multicast: 224.0.0.0 - 239.255.255.255
		return bytes.Length == 4 && bytes[0] >= 224 && bytes[0] <= 239;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;

		foreach (var client in _clients.Values)
		{
			try
			{
				client.Close();
				client.Dispose();
			}
			catch
			{
				// Ignore disposal errors
			}
		}

		_clients.Clear();
	}
}
