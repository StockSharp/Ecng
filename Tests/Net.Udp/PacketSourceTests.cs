namespace Ecng.Tests.Net.Udp;

using System.Net;

using Ecng.Net;

[TestClass]
public class PacketSourceTests : BaseTestClass
{
	[TestMethod]
	public async Task MemoryPacketSource_ReturnsAllPackets()
	{
		// Arrange
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(new IPEndPoint(IPAddress.Parse("224.0.0.1"), 5000), new byte[] { 1, 2, 3 }, DateTime.UtcNow),
			(new IPEndPoint(IPAddress.Parse("224.0.0.1"), 5000), new byte[] { 4, 5 }, DateTime.UtcNow.AddSeconds(1)),
			(new IPEndPoint(IPAddress.Parse("224.0.0.2"), 5001), new byte[] { 6 }, DateTime.UtcNow.AddSeconds(2)),
		};

		var source = new MemoryPacketSource(packets);

		// Act
		var received = new List<(IPEndPoint EndPoint, Memory<byte> Payload, DateTime PacketTime)>();
		await foreach (var packet in source.GetPacketsAsync().WithCancellation(CancellationToken))
		{
			received.Add(packet);
		}

		// Assert
		AreEqual(3, received.Count);

		AreEqual(packets[0].EndPoint, received[0].EndPoint);
		IsTrue(received[0].Payload.ToArray().SequenceEqual(packets[0].Payload));

		AreEqual(packets[1].EndPoint, received[1].EndPoint);
		IsTrue(received[1].Payload.ToArray().SequenceEqual(packets[1].Payload));

		AreEqual(packets[2].EndPoint, received[2].EndPoint);
		IsTrue(received[2].Payload.ToArray().SequenceEqual(packets[2].Payload));
	}

	[TestMethod]
	public async Task MemoryPacketSource_EmptyList_ReturnsNothing()
	{
		// Arrange
		var source = new MemoryPacketSource(new List<(IPEndPoint, byte[], DateTime)>());

		// Act
		var count = 0;
		await foreach (var _ in source.GetPacketsAsync().WithCancellation(CancellationToken))
		{
			count++;
		}

		// Assert
		AreEqual(0, count);
	}

	[TestMethod]
	public async Task MemoryPacketSource_RespectsCancellation_ViaParameter()
	{
		// Arrange
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 1 }, DateTime.UtcNow),
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 2 }, DateTime.UtcNow),
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 3 }, DateTime.UtcNow),
		};

		var source = new MemoryPacketSource(packets);
		var cts = new CancellationTokenSource();

		// Act
		var received = 0;
		try
		{
			await foreach (var _ in source.GetPacketsAsync().WithCancellation(cts.Token))
			{
				received++;
				if (received == 1)
					cts.Cancel();
			}
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		// Assert - should have received at least 1 but not all
		AreEqual(1, received);
	}

	[TestMethod]
	public async Task MemoryPacketSource_RespectsCancellation_ViaWithCancellation()
	{
		// Arrange
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 1 }, DateTime.UtcNow),
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 2 }, DateTime.UtcNow),
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 3 }, DateTime.UtcNow),
		};

		var source = new MemoryPacketSource(packets);
		var cts = new CancellationTokenSource();

		// Act
		var received = 0;
		try
		{
			await foreach (var _ in source.GetPacketsAsync().WithCancellation(cts.Token))
			{
				received++;
				if (received == 1)
					cts.Cancel();
			}
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		// Assert - should have received at least 1 but not all
		AreEqual(1, received);
	}

	[TestMethod]
	public void MemoryPacketSource_ThrowsOnNullPackets()
	{
		Throws<ArgumentNullException>(() => new MemoryPacketSource(null));
	}

	[TestMethod]
	public async Task MemoryPacketSource_PreservesTimestamps()
	{
		// Arrange
		var time1 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var time2 = new DateTime(2024, 1, 1, 12, 0, 1, DateTimeKind.Utc);
		var time3 = new DateTime(2024, 1, 1, 12, 0, 2, DateTimeKind.Utc);

		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(new IPEndPoint(IPAddress.Loopback, 1000), Array.Empty<byte>(), time1),
			(new IPEndPoint(IPAddress.Loopback, 1000), Array.Empty<byte>(), time2),
			(new IPEndPoint(IPAddress.Loopback, 1000), Array.Empty<byte>(), time3),
		};

		var source = new MemoryPacketSource(packets);

		// Act
		var times = new List<DateTime>();
		await foreach (var packet in source.GetPacketsAsync().WithCancellation(CancellationToken))
		{
			times.Add(packet.PacketTime);
		}

		// Assert
		AreEqual(time1, times[0]);
		AreEqual(time2, times[1]);
		AreEqual(time3, times[2]);
	}

	[TestMethod]
	public async Task MemoryPacketSource_PreservesEndpoints()
	{
		// Arrange
		var ep1 = new IPEndPoint(IPAddress.Parse("239.1.1.1"), 10000);
		var ep2 = new IPEndPoint(IPAddress.Parse("239.1.1.2"), 10001);
		var ep3 = new IPEndPoint(IPAddress.Parse("239.1.1.3"), 10002);

		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(ep1, new byte[] { 1 }, DateTime.UtcNow),
			(ep2, new byte[] { 2 }, DateTime.UtcNow),
			(ep3, new byte[] { 3 }, DateTime.UtcNow),
		};

		var source = new MemoryPacketSource(packets);

		// Act
		var endpoints = new List<IPEndPoint>();
		await foreach (var packet in source.GetPacketsAsync().WithCancellation(CancellationToken))
		{
			endpoints.Add(packet.EndPoint);
		}

		// Assert
		AreEqual(ep1, endpoints[0]);
		AreEqual(ep2, endpoints[1]);
		AreEqual(ep3, endpoints[2]);
	}

	[TestMethod]
	public async Task MemoryPacketSource_CanBeEnumeratedMultipleTimes()
	{
		// Arrange
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(new IPEndPoint(IPAddress.Loopback, 5000), new byte[] { 1, 2, 3 }, DateTime.UtcNow),
		};

		var source = new MemoryPacketSource(packets);

		// Act - enumerate twice
		var count1 = 0;
		await foreach (var _ in source.GetPacketsAsync().WithCancellation(CancellationToken))
			count1++;

		var count2 = 0;
		await foreach (var _ in source.GetPacketsAsync().WithCancellation(CancellationToken))
			count2++;

		// Assert
		AreEqual(1, count1);
		AreEqual(1, count2);
	}
}
