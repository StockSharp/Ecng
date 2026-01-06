namespace Ecng.Tests.Net.Udp;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

using Ecng.Net;

[TestClass]
public class UdpServerTests : BaseTestClass
{
	[TestMethod]
	public void UdpServer_CanCreate()
	{
		using var server = new UdpServer();
		IsNotNull(server);
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_ThrowsOnNullGroups()
	{
		using var server = new UdpServer();
		var source = new MemoryPacketSource([]);

		await ThrowsAsync<ArgumentNullException>(
			() => server.ReplayAsync(null, source, 1, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_ThrowsOnEmptyGroups()
	{
		using var server = new UdpServer();
		var source = new MemoryPacketSource([]);

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(new Dictionary<IPEndPoint, double>(), source, 1, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_ThrowsOnNullSource()
	{
		using var server = new UdpServer();
		var groups = new Dictionary<IPEndPoint, double>
		{
			{ new IPEndPoint(IPAddress.Parse("239.0.0.1"), 5000), 0 }
		};

		await ThrowsAsync<ArgumentNullException>(
			() => server.ReplayAsync(groups, null, 1, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_ThrowsOnInvalidSpeedMultiplier()
	{
		using var server = new UdpServer();
		var source = new MemoryPacketSource([]);
		var groups = new Dictionary<IPEndPoint, double>
		{
			{ new IPEndPoint(IPAddress.Parse("239.0.0.1"), 5000), 0 }
		};

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(groups, source, 0, CancellationToken).AsTask());

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(groups, source, -1, CancellationToken).AsTask());

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(groups, source, -0.5, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_AcceptsFractionalSpeed()
	{
		// Arrange
		var port = GetAvailablePort();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		using var receiver = new UdpClient(port);
		using var server = new UdpServer();

		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(endpoint, new byte[] { 1 }, DateTime.UtcNow),
		};

		var source = new MemoryPacketSource(packets);
		var groups = new Dictionary<IPEndPoint, double> { { endpoint, 0 } };

		// Act - should not throw with fractional speed
		var receiveTask = receiver.ReceiveAsync(CancellationToken);
		await Task.Delay(50);
		await server.ReplayAsync(groups, source, 0.5, CancellationToken);

		var received = await receiveTask;

		// Assert
		AreEqual(1, received.Buffer.Length);
	}

	[TestMethod]
	public async Task UdpServer_ReplayAsync_ThrowsOnInvalidPacketLoss()
	{
		using var server = new UdpServer();
		var source = new MemoryPacketSource([]);

		var groupsNegative = new Dictionary<IPEndPoint, double>
		{
			{ new IPEndPoint(IPAddress.Parse("239.0.0.1"), 5000), -0.1 }
		};

		var groupsOver1 = new Dictionary<IPEndPoint, double>
		{
			{ new IPEndPoint(IPAddress.Parse("239.0.0.1"), 5000), 1.1 }
		};

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(groupsNegative, source, 1, CancellationToken).AsTask());

		await ThrowsAsync<ArgumentOutOfRangeException>(
			() => server.ReplayAsync(groupsOver1, source, 1, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_SendAsync_ThrowsOnNullEndpoint()
	{
		using var server = new UdpServer();

		await ThrowsAsync<ArgumentNullException>(
			() => server.SendAsync(null, new byte[] { 1, 2, 3 }).AsTask());
	}

	[TestMethod]
	public async Task UdpServer_SendAsync_SendsPacket()
	{
		// Arrange
		var port = GetAvailablePort();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		using var receiver = new UdpClient(port);
		using var server = new UdpServer();

		var testData = new byte[] { 0xAA, 0xBB, 0xCC };

		// Act
		var receiveTask = receiver.ReceiveAsync();
		await Task.Delay(50);
		await server.SendAsync(endpoint, testData);

		var received = await receiveTask.WaitAsync(CancellationToken);

		// Assert
		AreEqual(testData.Length, received.Buffer.Length);
		AreEqual((byte)0xAA, received.Buffer[0]);
		AreEqual((byte)0xBB, received.Buffer[1]);
		AreEqual((byte)0xCC, received.Buffer[2]);
	}

	[TestMethod]
	public async Task UdpServer_SendAsync_ConcurrentCalls_NoRaceCondition()
	{
		// Arrange
		var port = GetAvailablePort();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		using var receiver = new UdpClient(port);
		using var server = new UdpServer();

		var receivedPackets = new ConcurrentBag<byte[]>();
		const int packetCount = 100;

		// Act - receive packets
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var receiveTask = Task.Run(async () =>
		{
			try
			{
				while (receivedPackets.Count < packetCount && !cts.Token.IsCancellationRequested)
				{
					var result = await receiver.ReceiveAsync(cts.Token);
					receivedPackets.Add(result.Buffer);
				}
			}
			catch (OperationCanceledException) { }
		});

		await Task.Delay(100);

		// Send packets concurrently from multiple threads
		var sendTasks = Enumerable.Range(0, packetCount)
			.Select(i => Task.Run(async () =>
			{
				await server.SendAsync(endpoint, new byte[] { (byte)i });
			}))
			.ToArray();

		await Task.WhenAll(sendTasks);
		await receiveTask;

		// Assert - all packets should be received without exceptions
		AreEqual(packetCount, receivedPackets.Count);
	}

	[TestMethod]
	public async Task UdpServer_ClientServer_Integration()
	{
		// Arrange - setup receiver
		var port = GetAvailablePort();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		using var receiver = new UdpClient(port);
		var receivedPackets = new List<byte[]>();

		// Packets to send
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(endpoint, new byte[] { 1, 2, 3 }, DateTime.UtcNow),
			(endpoint, new byte[] { 4, 5, 6, 7 }, DateTime.UtcNow.AddMilliseconds(10)),
			(endpoint, new byte[] { 8, 9 }, DateTime.UtcNow.AddMilliseconds(20)),
		};

		var source = new MemoryPacketSource(packets);
		var groups = new Dictionary<IPEndPoint, double>
		{
			{ endpoint, 0 } // No packet loss
		};

		using var server = new UdpServer();

		// Act - start receiving
		var receiveTask = Task.Run(async () =>
		{
			for (int i = 0; i < 3; i++)
			{
				var result = await receiver.ReceiveAsync(CancellationToken);
				receivedPackets.Add(result.Buffer);
			}
		});

		await Task.Delay(100);

		// Replay packets
		await server.ReplayAsync(groups, source, 100, CancellationToken); // 100x speed

		await receiveTask;

		// Assert
		AreEqual(3, receivedPackets.Count);

		AreEqual(3, receivedPackets[0].Length);
		AreEqual((byte)1, receivedPackets[0][0]);

		AreEqual(4, receivedPackets[1].Length);
		AreEqual((byte)4, receivedPackets[1][0]);

		AreEqual(2, receivedPackets[2].Length);
		AreEqual((byte)8, receivedPackets[2][0]);
	}

	[TestMethod]
	public async Task UdpServer_PacketLoss_DropsPackets()
	{
		// Arrange
		var port = GetAvailablePort();
		var endpoint = new IPEndPoint(IPAddress.Loopback, port);

		using var receiver = new UdpClient(port);
		var receivedCount = 0;

		// Send many packets with 100% loss
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>();
		for (int i = 0; i < 100; i++)
		{
			packets.Add((endpoint, new byte[] { (byte)i }, DateTime.UtcNow.AddMilliseconds(i)));
		}

		var source = new MemoryPacketSource(packets);
		var groups = new Dictionary<IPEndPoint, double>
		{
			{ endpoint, 1.0 } // 100% packet loss
		};

		using var server = new UdpServer();

		// Act
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var receiveTask = Task.Run(async () =>
		{
			try
			{
				while (!cts.Token.IsCancellationRequested)
				{
					await receiver.ReceiveAsync(cts.Token);
					receivedCount++;
				}
			}
			catch (OperationCanceledException) { }
		});

		await server.ReplayAsync(groups, source, 1000, CancellationToken);
		await receiveTask;

		// Assert - with 100% packet loss, nothing should be received
		AreEqual(0, receivedCount);
	}

	[TestMethod]
	public async Task UdpServer_IgnoresUnknownEndpoints()
	{
		// Arrange
		var knownPort = GetAvailablePort();
		var unknownPort = GetAvailablePort();

		var knownEndpoint = new IPEndPoint(IPAddress.Loopback, knownPort);
		var unknownEndpoint = new IPEndPoint(IPAddress.Loopback, unknownPort);

		using var receiver = new UdpClient(knownPort);
		var receivedCount = 0;

		// Mix of known and unknown endpoints
		var packets = new List<(IPEndPoint EndPoint, byte[] Payload, DateTime PacketTime)>
		{
			(knownEndpoint, new byte[] { 1 }, DateTime.UtcNow),
			(unknownEndpoint, new byte[] { 2 }, DateTime.UtcNow.AddMilliseconds(1)), // Should be ignored
			(knownEndpoint, new byte[] { 3 }, DateTime.UtcNow.AddMilliseconds(2)),
			(unknownEndpoint, new byte[] { 4 }, DateTime.UtcNow.AddMilliseconds(3)), // Should be ignored
		};

		var source = new MemoryPacketSource(packets);
		var groups = new Dictionary<IPEndPoint, double>
		{
			{ knownEndpoint, 0 } // Only known endpoint
		};

		using var server = new UdpServer();

		// Act
		var receiveTask = Task.Run(async () =>
		{
			try
			{
				while (receivedCount < 2)
				{
					await receiver.ReceiveAsync(CancellationToken);
					receivedCount++;
				}
			}
			catch (OperationCanceledException) { }
		});

		await Task.Delay(100);
		await server.ReplayAsync(groups, source, 100, CancellationToken);
		await receiveTask;

		// Assert - only 2 packets to known endpoint
		AreEqual(2, receivedCount);
	}

	[TestMethod]
	public void UdpServer_IsMulticastAddress_IPv4_Multicast()
	{
		// Multicast range: 224.0.0.0 - 239.255.255.255
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("224.0.0.0")));
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("224.0.0.1")));
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("239.255.255.255")));
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("230.1.2.3")));
	}

	[TestMethod]
	public void UdpServer_IsMulticastAddress_IPv4_NotMulticast()
	{
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("223.255.255.255")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("240.0.0.0")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("127.0.0.1")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("192.168.1.1")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("10.0.0.1")));
	}

	[TestMethod]
	public void UdpServer_IsMulticastAddress_IPv6_Multicast()
	{
		// IPv6 multicast addresses start with ff
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("ff02::1")));
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("ff05::1")));
		IsTrue(UdpServer.IsMulticastAddress(IPAddress.Parse("ff0e::1")));
	}

	[TestMethod]
	public void UdpServer_IsMulticastAddress_IPv6_NotMulticast()
	{
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("::1")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("fe80::1")));
		IsFalse(UdpServer.IsMulticastAddress(IPAddress.Parse("2001:db8::1")));
	}

	[TestMethod]
	public void UdpServer_IsMulticastAddress_ThrowsOnNull()
	{
		Throws<ArgumentNullException>(() => UdpServer.IsMulticastAddress(null));
	}

	private static int GetAvailablePort()
	{
		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		return ((IPEndPoint)socket.LocalEndPoint).Port;
	}
}
