namespace Ecng.Tests.Net;

using System.Buffers;
using System.Net;

using Ecng.Net;

/// <summary>
/// Tests for UDP packet processor layer.
/// </summary>
[TestClass]
public class UdpPacketProcessorTests : BaseTestClass
{
	[TestMethod]
	public async Task MockProcessor_ProcessNewPacket_StoresPacketData()
	{
		var processor = new MockPacketProcessor();
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		var packet = processor.AllocatePacket(testData.Length);
		testData.CopyTo(packet.Memory.Span);

		var result = await processor.ProcessNewPacket(packet, testData.Length, CancellationToken);

		result.AssertTrue();
		processor.ProcessedCount.AssertEqual(1);
		processor.ProcessedPackets[0].data.SequenceEqual(testData).AssertTrue();
		processor.ProcessedPackets[0].length.AssertEqual(testData.Length);
	}

	[TestMethod]
	public async Task MockProcessor_ProcessNewPacket_ReturnsFalseWhenConfigured()
	{
		var processor = new MockPacketProcessor();
		processor.ContinueProcessing = false;
		var packet = processor.AllocatePacket(10);

		var result = await processor.ProcessNewPacket(packet, 10, CancellationToken);

		result.AssertFalse();
	}

	[TestMethod]
	public async Task MockProcessor_ProcessNewPacket_ThrowsConfiguredException()
	{
		var processor = new MockPacketProcessor();
		processor.ProcessException = new InvalidOperationException("Test exception");
		var packet = processor.AllocatePacket(10);

		await ThrowsExactlyAsync<InvalidOperationException>(async () =>
			await processor.ProcessNewPacket(packet, 10, CancellationToken));
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
				await processor.ProcessNewPacket(packet, 10, CancellationToken);
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

	[TestMethod]
	public async Task RealPacketReceiver_AllPacketsDisposed_AfterProcessing()
	{
		var processor = new TrackingPacketProcessor { MaxIncomingQueueSize = 1000 };
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		const int packetCount = 500;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)(i & 0xFF), (byte)(i >> 8)]);

		await processor.WaitForProcessedAsync(packetCount, TimeSpan.FromSeconds(10));

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		AreEqual(packetCount, processor.Processed);
		// receiver may allocate one extra packet on shutdown (cancelled ReceiveAsync)
		IsTrue(processor.Allocated >= packetCount);
		AreEqual(processor.Allocated, processor.Disposed);
		AreEqual(0, processor.LiveCount);
	}

	[TestMethod]
	public async Task RealPacketReceiver_PacketDisposedBeforeSendOut()
	{
		// Simulate: parsing is fast (packet disposed immediately), SendOut is slow
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = 1000,
			SendOutDelay = TimeSpan.FromMilliseconds(20),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		const int packetCount = 50;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		await processor.WaitForProcessedAsync(packetCount, TimeSpan.FromSeconds(30));

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		AreEqual(packetCount, processor.Processed);
		AreEqual(processor.Allocated, processor.Disposed);
		AreEqual(0, processor.LiveCount);

		// verify packet was disposed at parse time, not after SendOut delay
		foreach (var (disposedAt, processedAt) in processor.Timings)
			IsTrue(disposedAt <= processedAt);
	}

	[TestMethod]
	public async Task RealPacketReceiver_QueueOverflow_DropNewest_AllDisposed()
	{
		const int queueSize = 10;
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = queueSize,
			SendOutDelay = TimeSpan.FromMilliseconds(50),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullMode.DropNewest);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		// send many packets at once — queue will overflow
		const int packetCount = 100;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		// wait for some processing
		await Task.Delay(3000, CancellationToken);

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		// every allocated packet must be disposed (either processed or dropped)
		AreEqual(processor.Allocated, processor.Disposed);
		AreEqual(0, processor.LiveCount);
	}

	[TestMethod]
	public async Task RealPacketReceiver_QueueOverflow_DropOldest_AllDisposed()
	{
		const int queueSize = 10;
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = queueSize,
			SendOutDelay = TimeSpan.FromMilliseconds(50),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullMode.DropOldest);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		const int packetCount = 100;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		await Task.Delay(3000, CancellationToken);

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		// every allocated packet must be disposed
		AreEqual(processor.Allocated, processor.Disposed);
		AreEqual(0, processor.LiveCount);
	}

	[TestMethod]
	public async Task RealPacketReceiver_LiveCount_BoundedByQueueSize()
	{
		const int queueSize = 20;
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = queueSize,
			SendOutDelay = TimeSpan.FromMilliseconds(100),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		// send a burst
		for (var i = 0; i < 200; i++)
			socket.EnqueuePacket([(byte)i]);

		// give receiver time to fill queue
		await Task.Delay(500, CancellationToken);

		// live count should never exceed queue size + a few in-flight
		var maxLive = processor.MaxLiveCount;
		IsTrue(maxLive <= queueSize + 5);

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();
	}

	private static MulticastSourceAddress CreateTestAddress()
	{
		return new MulticastSourceAddress
		{
			GroupAddress = "239.255.0.1".To<IPAddress>(),
			Port = 12345,
			IsEnabled = true
		};
	}

	/// <summary>
	/// Packet processor that tracks allocation/disposal counts (like the real CNT debug counter).
	/// Simulates real behavior: packet is disposed during parsing, BEFORE SendOutMessageAsync.
	/// </summary>
	private class TrackingPacketProcessor : IPacketProcessor
	{
		private int _allocated;
		private int _disposed;
		private int _processed;
		private int _maxLive;

		private readonly List<(long disposedTicks, long processedTicks)> _timings = [];

		public int LiveCount => _allocated - _disposed;
		public int Processed => _processed;
		public int Allocated => _allocated;
		public int Disposed => _disposed;
		public int MaxLiveCount => _maxLive;

		public IReadOnlyList<(long disposedAt, long processedAt)> Timings => _timings;

		public TimeSpan SendOutDelay { get; set; }

		public int MaxIncomingQueueSize { get; set; } = 10000;
		public int MaxUdpDatagramSize { get; set; } = 65535;
		public string Name => "Tracking";

		public IMemoryOwner<byte> AllocatePacket(int size)
		{
			Interlocked.Increment(ref _allocated);

			// track max live
			int live;
			int prevMax;
			do
			{
				live = _allocated - _disposed;
				prevMax = _maxLive;
			}
			while (live > prevMax && Interlocked.CompareExchange(ref _maxLive, live, prevMax) != prevMax);

			return new TrackingMemoryOwner(this, new byte[size]);
		}

		public async ValueTask<bool> ProcessNewPacket(IMemoryOwner<byte> packet, int length, CancellationToken ct)
		{
			// simulate real _processMessage: parse data, then DISPOSE packet
			_ = packet.Memory.Span[..length].ToArray();
			packet.Dispose();
			var disposedTicks = Environment.TickCount64;

			// simulate SendOutMessageAsync (packet already disposed at this point)
			if (SendOutDelay > TimeSpan.Zero)
				await Task.Delay(SendOutDelay, ct);

			var processedTicks = Environment.TickCount64;
			Interlocked.Increment(ref _processed);

			lock (_timings)
				_timings.Add((disposedTicks, processedTicks));

			return true;
		}

		public void DisposePacket(IMemoryOwner<byte> packet, string reason)
		{
			packet.Dispose();
		}

		public void ErrorHandler(Exception ex, int errorCount, bool isFatal) { }

		public async Task<bool> WaitForProcessedAsync(int count, TimeSpan timeout)
		{
			using var cts = new CancellationTokenSource(timeout);
			try
			{
				while (Volatile.Read(ref _processed) < count && !cts.Token.IsCancellationRequested)
					await Task.Delay(10, cts.Token);
				return Volatile.Read(ref _processed) >= count;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}

		private class TrackingMemoryOwner(TrackingPacketProcessor tracker, byte[] buffer) : IMemoryOwner<byte>
		{
			private int _disposed;
			public Memory<byte> Memory => buffer;
			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) == 0)
					Interlocked.Increment(ref tracker._disposed);
			}
		}
	}

	[TestMethod]
	public async Task RealPacketReceiver_IPv6Multicast_ShouldWorkCorrectly()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};

		var socketFactory = new RealUdpSocketFactory();

		var ipv6Address = new MulticastSourceAddress
		{
			GroupAddress = IPAddress.Parse("ff02::1"),
			Port = 54321,
			IsEnabled = true
		};
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, ipv6Address, socketFactory, logs);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		try
		{
			await receiver.RunAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
		}

		receiver.Dispose();

		if (processor.Errors.Count > 0)
		{
			var error = processor.Errors[0];
			Fail($"IPv6 multicast failed with processor error: {error.ex.GetType().Name}: {error.ex.Message}. " +
				"BUG: RealPacketReceiver doesn't handle IPv6 addresses correctly.");
		}

		if (logs.HasErrors)
		{
			var error = logs.Errors.First();

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
