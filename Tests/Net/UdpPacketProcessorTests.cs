namespace Ecng.Tests.Net;

using System.Net;

using Ecng.Net;

/// <summary>
/// Tests for UDP packet processor layer.
/// </summary>
[TestClass]
public class UdpPacketProcessorTests : BaseTestClass
{
	[TestMethod]
	public void MockProcessor_ProcessNewPacket_StoresPacketData()
	{
		var processor = new MockPacketProcessor();
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		var packet = processor.AllocatePacket(testData.Length);
		testData.CopyTo(packet.Memory.Span);

		var result = processor.ProcessNewPacket(packet, testData.Length);

		result.AssertTrue();
		processor.ProcessedCount.AssertEqual(1);
		processor.ProcessedPackets[0].data.SequenceEqual(testData).AssertTrue();
		processor.ProcessedPackets[0].length.AssertEqual(testData.Length);
	}

	[TestMethod]
	public void MockProcessor_ProcessNewPacket_ReturnsFalseWhenConfigured()
	{
		var processor = new MockPacketProcessor();
		processor.ContinueProcessing = false;
		var packet = processor.AllocatePacket(10);

		var result = processor.ProcessNewPacket(packet, 10);

		result.AssertFalse();
	}

	[TestMethod]
	public void MockProcessor_ProcessNewPacket_ThrowsConfiguredException()
	{
		var processor = new MockPacketProcessor();
		processor.ProcessException = new InvalidOperationException("Test exception");
		var packet = processor.AllocatePacket(10);

		ThrowsExactly<InvalidOperationException>(() =>
			processor.ProcessNewPacket(packet, 10));
	}

	[TestMethod]
	public void MockProcessor_DisposePacket_RecordsDisposal()
	{
		var processor = new MockPacketProcessor();
		var packet = processor.AllocatePacket(10);

		processor.DisposePacket(packet, "test reason");

		processor.DisposedPackets.Count.AssertEqual(1);
		processor.DisposedPackets[0].reason.AssertEqual("test reason");
	}

	[TestMethod]
	public void MockProcessor_ErrorHandler_RecordsErrors()
	{
		var processor = new MockPacketProcessor();
		var ex = new InvalidOperationException("Test error");

		processor.ErrorHandler(ex, 5, true);

		processor.Errors.Count.AssertEqual(1);
		processor.Errors[0].ex.AssertEqual(ex);
		processor.Errors[0].errorCount.AssertEqual(5);
		processor.Errors[0].isFatal.AssertTrue();
	}

	[TestMethod]
	public async Task MockProcessor_WaitForPackets_ReturnsWhenCountReached()
	{
		var processor = new MockPacketProcessor();

		// Process packets in background
		_ = Task.Run(async () =>
		{
			for (var i = 0; i < 5; i++)
			{
				await Task.Delay(20, CancellationToken);
				var packet = processor.AllocatePacket(10);
				processor.ProcessNewPacket(packet, 10);
			}
		}, CancellationToken);

		var result = await processor.WaitForPacketsAsync(5, TimeSpan.FromSeconds(5));

		result.AssertTrue();
		processor.ProcessedCount.AssertEqual(5);
	}

	[TestMethod]
	public async Task MockProcessor_WaitForPackets_ReturnsFalseOnTimeout()
	{
		var processor = new MockPacketProcessor();

		var result = await processor.WaitForPacketsAsync(5, TimeSpan.FromMilliseconds(100));

		result.AssertFalse();
	}

	[TestMethod]
	public void MockProcessor_AllocatePacket_ReturnsCorrectSize()
	{
		var processor = new MockPacketProcessor();

		var packet = processor.AllocatePacket(1024);

		packet.AssertNotNull();
		packet.Memory.Length.AssertEqual(1024);
	}

	[TestMethod]
	public async Task RealPacketReceiver_RunsReceiverAndProcessor()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;

		// Create minimal address - will be used for bind only in mock
		var address = CreateTestAddress();
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs);

		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		// Wait for receiver to start
		await Task.Delay(100, CancellationToken);

		// Enqueue test packets
		var testPackets = new[]
		{
			new byte[] { 1, 2, 3 },
			[4, 5, 6, 7],
			[8, 9, 10, 11, 12]
		};

		foreach (var packet in testPackets)
			socket.EnqueuePacket(packet);

		// Wait for packets to be processed
		var received = await processor.WaitForPacketsAsync(3, TimeSpan.FromSeconds(5));
		received.AssertTrue();

		// Cancel and wait for completion
		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }

		// Verify packets were processed
		processor.ProcessedCount.AssertEqual(3);
		processor.ProcessedPackets[0].data.SequenceEqual(testPackets[0]).AssertTrue();
		processor.ProcessedPackets[1].data.SequenceEqual(testPackets[1]).AssertTrue();
		processor.ProcessedPackets[2].data.SequenceEqual(testPackets[2]).AssertTrue();

		receiver.Dispose();
	}

	[TestMethod]
	public async Task RealPacketReceiver_StopsWhenProcessorReturnsFalse()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;

		var address = CreateTestAddress();
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs);

		// Configure processor to stop after first packet
		processor.ContinueProcessing = false;

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(100, CancellationToken);

		// Send packet
		socket.EnqueuePacket([1, 2, 3]);

		// Wait for processing to stop
		await Task.Delay(200, CancellationToken);

		// Send another packet - it should not be processed
		socket.EnqueuePacket([4, 5, 6]);

		await Task.Delay(200, CancellationToken);

		// Verify only one packet was processed
		processor.ProcessedCount.AssertEqual(1);

		cts.Cancel();
		try { await runTask; } catch { }

		receiver.Dispose();
	}

	[TestMethod]
	public async Task RealPacketReceiver_HandlesProcessorException()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;

		var address = CreateTestAddress();
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs);

		// Configure processor to throw on first packet
		processor.ProcessException = new InvalidOperationException("Test error");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(100, CancellationToken);

		// Send packets
		socket.EnqueuePacket([1, 2, 3]);
		socket.EnqueuePacket([4, 5, 6]);

		await Task.Delay(200, CancellationToken);

		// Verify error was recorded
		IsGreater(processor.Errors.Count, 0);

		cts.Cancel();
		try { await runTask; } catch { }

		receiver.Dispose();
	}

	private static MulticastSourceAddress CreateTestAddress()
	{
		// Create test multicast address
		return new MulticastSourceAddress
		{
			GroupAddress = "239.255.0.1".To<IPAddress>(),
			Port = 12345,
			IsEnabled = true
		};
	}

	/// <summary>
	/// BUG: RealPacketReceiver creates socket without considering AddressFamily from GroupAddress.
	/// It always creates IPv4 socket (default) and binds to IPAddress.Any.
	/// When GroupAddress is IPv6, the socket creation/binding will fail or not receive packets.
	///
	/// Expected: Should detect IPv6 address and create IPv6 socket, bind to IPv6Any.
	/// Actual: Creates IPv4 socket, fails for IPv6 multicast.
	/// </summary>
	[TestMethod]
	public async Task RealPacketReceiver_IPv6Multicast_ShouldWorkCorrectly()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};

		// Use REAL socket factory, not mock - to expose the bug
		var socketFactory = new RealUdpSocketFactory();

		// IPv6 multicast address
		var ipv6Address = new MulticastSourceAddress
		{
			GroupAddress = IPAddress.Parse("ff02::1"), // IPv6 link-local multicast
			Port = 54321,
			IsEnabled = true
		};
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, ipv6Address, socketFactory, logs);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		// BUG: This should throw or fail because:
		// 1. Socket is created with default AddressFamily (IPv4)
		// 2. Bind is called with IPAddress.Any (IPv4)
		// 3. IPv6 multicast group join will fail

		try
		{
			await receiver.RunAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
			// Expected - timeout
		}

		receiver.Dispose();

		// The bug manifests in two possible ways:
		// 1. Error in ErrorHandler (receive loop errors)
		// 2. Error in logs (JoinMulticast failure during startup)
		//
		// Check both for the bug confirmation
		if (processor.Errors.Count > 0)
		{
			var error = processor.Errors[0];
			Fail($"IPv6 multicast failed with processor error: {error.ex.GetType().Name}: {error.ex.Message}. " +
				"BUG: RealPacketReceiver doesn't handle IPv6 addresses correctly.");
		}

		if (logs.HasErrors)
		{
			var error = logs.Errors.First();

			// Skip test if IPv6 multicast is not available in the environment (e.g., CI runners)
			// Error 49 = EADDRNOTAVAIL on macOS, "Can't assign requested address"
			if (error.Message.Contains("Can't assign requested address") ||
				error.Message.Contains("EADDRNOTAVAIL") ||
				error.Message.Contains("Network is unreachable"))
			{
				Inconclusive("IPv6 multicast not available in this environment (CI/container limitation).");
				return;
			}

			Fail($"IPv6 multicast failed with logged error: {error.Message}. " +
				"BUG: RealPacketReceiver creates IPv4 socket but tries to join IPv6 multicast group. " +
				"JoinMulticast uses SocketOptionLevel.IP instead of SocketOptionLevel.IPv6.");
		}
	}
}
