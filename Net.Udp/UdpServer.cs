namespace Ecng.Net;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// UDP server that relays packets from a source to multicast groups.
/// </summary>
public class UdpServer : Disposable
{
	private readonly ConcurrentDictionary<IPEndPoint, UdpClient> _clients = new();

	/// <summary>
	/// Replays packets from the source to the configured multicast groups.
	/// </summary>
	/// <param name="multicastGroups">Dictionary of multicast endpoints to packet loss probability (0-1).</param>
	/// <param name="packetSource">The source of packets to replay.</param>
	/// <param name="speedMultiplier">Speed multiplier (1 = real-time, 2 = 2x speed, 0.5 = half speed, etc.).</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async ValueTask ReplayAsync(
		Dictionary<IPEndPoint, double> multicastGroups,
		IUdpPacketSource packetSource,
		double speedMultiplier,
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

		// Validate all parameters before creating any clients
		foreach (var entry in multicastGroups)
		{
			var packetLossProbability = entry.Value;
			if (packetLossProbability < 0 || packetLossProbability > 1)
				throw new ArgumentOutOfRangeException(nameof(multicastGroups), $"Packet loss probability must be between 0 and 1, got {packetLossProbability}");
		}

		var clients = new Dictionary<IPEndPoint, (UdpClient client, double probability)>();
		var createdEndpoints = new List<IPEndPoint>();

		try
		{
			foreach (var entry in multicastGroups)
			{
				var client = GetOrCreateClient(entry.Key, createdEndpoints);
				clients[entry.Key] = (client, entry.Value);
			}

			DateTime? lastPacketTime = null;
			var random = new Random();
			double accumulatedDelay = 0;

			await foreach (var (targetDest, payload, packetTime) in packetSource.GetPacketsAsync().WithCancellation(cancellationToken))
			{
				if (!clients.TryGetValue(targetDest, out var t))
					continue;

				// Simulate packet loss
				if (random.NextDouble() < t.probability)
					continue;

				// Simulate timing with accumulated fractional delays
				if (lastPacketTime.HasValue)
				{
					var delay = (packetTime - lastPacketTime.Value).TotalMilliseconds / speedMultiplier;

					// Ignore negative delays (out-of-order timestamps)
					if (delay > 0)
					{
						accumulatedDelay += delay;

						if (accumulatedDelay >= 1)
						{
							var delayMs = (int)accumulatedDelay;
							accumulatedDelay -= delayMs;
							await Task.Delay(delayMs, cancellationToken);
						}
					}
				}

				lastPacketTime = packetTime;
				await t.client.SendAsync(payload, targetDest, cancellationToken);
			}
		}
		catch
		{
			// Remove and dispose newly created clients on error
			foreach (var endpoint in createdEndpoints)
			{
				if (_clients.TryRemove(endpoint, out var client))
				{
					try
					{
						client.Dispose();
					}
					catch
					{
						// Ignore disposal errors
					}
				}
			}

			throw;
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

		var client = GetOrCreateClient(endpoint, null);
		await client.SendAsync(data, endpoint, cancellationToken);
	}

	private UdpClient GetOrCreateClient(IPEndPoint endpoint, List<IPEndPoint> trackNewEndpoints)
	{
		return _clients.GetOrAdd(endpoint, ep =>
		{
			var client = new UdpClient(ep.AddressFamily);

			if (ep.Address.IsMulticastAddress())
				client.JoinMulticastGroup(ep.Address);

			trackNewEndpoints?.Add(ep);
			return client;
		});
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		foreach (var kvp in _clients)
		{
			if (_clients.TryRemove(kvp.Key, out var client))
			{
				try
				{
					client.Dispose();
				}
				catch
				{
					// Ignore disposal errors
				}
			}
		}

		base.DisposeManaged();
	}
}