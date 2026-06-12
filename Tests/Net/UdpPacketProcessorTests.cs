namespace Ecng.Tests.Net;

using System.Buffers;

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

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs, PacketQueueFullModes.DropNewest);

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

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs, PacketQueueFullModes.DropNewest);

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

		var receiver = new RealPacketReceiver(processor, address, socketFactory, logs, PacketQueueFullModes.DropNewest);

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

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);
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

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);
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

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);
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

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropOldest);
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

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);
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

	[TestMethod]
	public async Task RealPacketReceiver_QueueFull_Wait_AllProcessed()
	{
		const int queueSize = 5;
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = queueSize,
			SendOutDelay = TimeSpan.FromMilliseconds(50),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.Wait);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		const int packetCount = 30;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		// all packets must be processed (no drops)
		var received = await processor.WaitForProcessedAsync(packetCount, TimeSpan.FromSeconds(10));
		received.AssertTrue();

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		AreEqual(packetCount, processor.Processed);
		AreEqual(processor.Allocated, processor.Disposed);
		AreEqual(0, processor.LiveCount);
	}

	[TestMethod]
	public async Task RealPacketReceiver_QueueFull_Wait_NoneDropped()
	{
		const int queueSize = 3;
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = queueSize,
			SendOutDelay = TimeSpan.FromMilliseconds(100),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.Wait);
		using var cts = new CancellationTokenSource();
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		const int packetCount = 20;
		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		var received = await processor.WaitForProcessedAsync(packetCount, TimeSpan.FromSeconds(15));
		received.AssertTrue();

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		// Wait mode: all allocated == all processed (no DisposePacket calls for drops)
		AreEqual(packetCount, processor.Processed);
		AreEqual(processor.Allocated, processor.Disposed);
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
		public int? StopAfterCount { get; set; }

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
			var count = Interlocked.Increment(ref _processed);

			lock (_timings)
				_timings.Add((disposedTicks, processedTicks));

			return !StopAfterCount.HasValue || count < StopAfterCount.Value;
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
	public async Task RealPacketReceiver_ProcessorStops_RemainingPacketsDrained()
	{
		// When processor returns false mid-stream, packets still in the channel
		// must be disposed. Without drain in ProcessPackets' finally, they leak.
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			StopAfterCount = 3,
			SendOutDelay = TimeSpan.FromMilliseconds(100),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		// send many packets quickly - queue fills while processor is slow
		const int totalPackets = 30;
		for (var i = 0; i < totalPackets; i++)
			socket.EnqueuePacket([(byte)i]);

		// wait for processor to process StopAfterCount packets and return false
		(await processor.WaitForProcessedAsync(3, TimeSpan.FromSeconds(5))).AssertTrue();

		// cancel to stop receiver, then wait for full shutdown
		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		// processor handled exactly 3 packets
		AreEqual(3, processor.Processed);
		// more packets were allocated than processed (they were queued)
		IsTrue(processor.Allocated > processor.Processed,
			$"Expected more allocations ({processor.Allocated}) than processed ({processor.Processed})");
		// KEY: every allocated packet must be disposed - no leaks
		AreEqual(processor.Allocated, processor.Disposed,
			$"Leaked {processor.Allocated - processor.Disposed} packets (allocated={processor.Allocated}, disposed={processor.Disposed})");
		AreEqual(0, processor.LiveCount);
	}

	[TestMethod]
	public async Task RealPacketReceiver_ReceiverExits_WhenProcessorCompletesChannel()
	{
		// Receiver loop must exit when processor completes the channel.
		// Without reader.Completion.IsCompleted check, receiver loops forever
		// and RunAsync never completes (only CancellationToken would stop it).
		var processor = new TrackingPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			StopAfterCount = 1,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);

		// long timeout - we intentionally do NOT cancel to prove receiver stops on its own
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(50, CancellationToken);

		// one packet triggers processor stop (returns false)
		socket.EnqueuePacket([1, 2, 3]);

		// wait for processor to stop
		(await processor.WaitForProcessedAsync(1, TimeSpan.FromSeconds(5))).AssertTrue();

		// allow ProcessPackets' finally to complete (TryComplete on channel)
		await Task.Delay(200, CancellationToken);

		// send more packets to unblock receiver's ReceiveAsync so it can see the completion
		for (var i = 0; i < 5; i++)
		{
			socket.EnqueuePacket([(byte)(10 + i)]);
			await Task.Delay(50, CancellationToken);
		}

		// receiver should exit on its own - RunAsync must complete WITHOUT cts cancellation
		var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken));
		var receiverStopped = ReferenceEquals(completedTask, runTask);

		cts.Cancel();
		try { await runTask; } catch { }
		receiver.Dispose();

		IsTrue(receiverStopped,
			"Receiver did not stop after processor completed the channel. " +
			"Without reader.Completion.IsCompleted check, receiver loops forever.");
		AreEqual(1, processor.Processed);
		AreEqual(processor.Allocated, processor.Disposed);
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

		var receiver = new RealPacketReceiver(processor, ipv6Address, socketFactory, logs, PacketQueueFullModes.DropNewest);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

		try
		{
			await receiver.RunAsync(cts.Token);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex) when (
			ex is SocketException { SocketErrorCode: SocketError.AddressNotAvailable or SocketError.NetworkUnreachable } ||
			ex.InnerException is SocketException { SocketErrorCode: SocketError.AddressNotAvailable or SocketError.NetworkUnreachable })
		{
			// After the receiver-swallow fix the failed multicast join propagates instead of
			// being logged; classify it by the typed SocketErrorCode (the host cannot join an
			// IPv6 multicast group) rather than by message text, and skip on such environments.
			receiver.Dispose();
			Inconclusive("IPv6 multicast not available in this environment (CI/container limitation).");
			return;
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

	/// <summary>
	/// BUG: RunThread (Net.Udp\IPacketReceiver.cs:107) logs non-cancellation exceptions
	/// but does NOT rethrow them; only OperationCanceledException raised while the token is
	/// cancelled propagates. A fatal startup failure (e.g. socket.Bind throwing
	/// "address already in use", or JoinMulticast failing) therefore gets swallowed: the
	/// receive thread ends, the channel writer completes, ProcessPackets drains and ends,
	/// and the Task returned by RunAsync completes SUCCESSFULLY - so a receiver that never
	/// started is indistinguishable from an orderly shutdown.
	/// Expected: when a fatal (non-cancellation) error kills the receiver, the Task returned
	/// by RunAsync must FAULT so the caller can observe the failure.
	/// Actual: RunAsync completes without faulting.
	/// </summary>
	[TestMethod]
	public async Task RunAsync_FatalStartupError_ShouldFaultTask()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};

		var bindError = new InvalidOperationException("address already in use");
		var socketFactory = new FailingSocketFactory(new FailingUdpSocket { BindException = bindError });
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);

		// The token is NOT cancelled - this is a genuine fatal failure, not a cancellation.
		// RunAsync must surface it as a faulted task instead of completing successfully.
		await ThrowsAsync<Exception>(() => receiver.RunAsync(CancellationToken));

		receiver.Dispose();
	}

	/// <summary>
	/// BUG: ReceivePackets (Net.Udp\IPacketReceiver.cs:204) treats len &lt;= 0 from
	/// socket.ReceiveAsync as fatal: it logs an error and breaks out of the receive loop,
	/// which completes the channel writer and tears the whole receiver down. A zero-length
	/// UDP datagram is a perfectly legal packet (recv returning 0 bytes for UDP means an empty
	/// datagram, NOT a closed connection), so a single empty datagram permanently stops the feed.
	/// Expected: an empty datagram must be tolerated - the receiver keeps running and continues
	/// processing subsequent packets.
	/// Actual: the receiver stops on the empty datagram and never processes the following packet.
	/// </summary>
	[TestMethod]
	public async Task ReceivePackets_EmptyDatagram_ShouldNotStopReceiver()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(100, CancellationToken);

		// First an empty (zero-length) datagram, then a real one.
		socket.EnqueuePacket([]);
		socket.EnqueuePacket([1, 2, 3]);

		// The real packet must still be processed despite the preceding empty datagram.
		var received = await processor.WaitForPacketsAsync(1, TimeSpan.FromSeconds(5));

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		received.AssertTrue();
		IsTrue(processor.ProcessedPackets.Any(p => p.data.SequenceEqual(new byte[] { 1, 2, 3 })),
			"The non-empty datagram following an empty one was not processed - " +
			"the empty datagram stopped the receiver.");
	}

	/// <summary>
	/// BUG: ProcessPackets (Net.Udp\IPacketReceiver.cs:134) wraps ProcessNewPacket in a
	/// try/catch that only calls ErrorHandler. When ProcessNewPacket throws (e.g. a parse error
	/// before the processor takes ownership of the buffer), the rented IMemoryOwner&lt;byte&gt;
	/// is never released - it is neither processed nor disposed, so the pooled buffer leaks.
	/// Every other failure path in the class disposes the packet; this one does not.
	/// Expected: the buffer is released on the exception path - allocated buffers == disposed buffers.
	/// Actual: each throwing packet leaks one rented buffer (allocated &gt; disposed).
	/// </summary>
	[TestMethod]
	public async Task ProcessPackets_ProcessorThrows_ShouldNotLeakBuffer()
	{
		const int packetCount = 20;
		var processor = new LeakTrackingProcessor
		{
			MaxIncomingQueueSize = 1000,
			ThrowOnProcess = new InvalidOperationException("parse error"),
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		var runTask = receiver.RunAsync(cts.Token);

		await Task.Delay(100, CancellationToken);

		for (var i = 0; i < packetCount; i++)
			socket.EnqueuePacket([(byte)i]);

		// Every packet reaches ProcessNewPacket and throws -> one ErrorHandler call each.
		await processor.WaitForErrorsAsync(packetCount, TimeSpan.FromSeconds(10));

		cts.Cancel();
		try { await runTask; } catch (OperationCanceledException) { }
		receiver.Dispose();

		IsGreater(processor.Errors, 0);
		// KEY invariant: no rented buffer may leak, even when ProcessNewPacket throws.
		AreEqual(processor.Allocated, processor.Disposed,
			$"Leaked {processor.Allocated - processor.Disposed} buffers on the processing-exception path " +
			$"(allocated={processor.Allocated}, disposed={processor.Disposed}).");
	}

	/// <summary>
	/// BUG: DisposeManaged (Net.Udp\IPacketReceiver.cs:167) only completes the channel writer;
	/// it does not interrupt a receive that is currently blocked in socket.ReceiveAsync. The
	/// receive loop only checks reader.Completion.IsCompleted BETWEEN receives, so on a quiet
	/// network the loop stays blocked forever after Dispose: the socket remains open and
	/// group-joined, and the Task returned by RunAsync never completes unless the caller also
	/// cancels the token.
	/// Expected: Dispose() (without cancelling the token) interrupts the blocked receive so the
	/// receiver shuts down and RunAsync completes promptly.
	/// Actual: RunAsync stays pending after Dispose because the blocked ReceiveAsync is never cancelled.
	/// </summary>
	[TestMethod]
	public async Task Dispose_WithoutCancel_ShouldCompleteRunAsync()
	{
		var processor = new MockPacketProcessor
		{
			MaxIncomingQueueSize = 100,
			MaxUdpDatagramSize = 65535,
		};
		var socketFactory = new MockUdpSocketFactory();
		var socket = new MockUdpSocket();
		socketFactory.NextSocket = socket;
		var logs = new MockLogReceiver();

		var receiver = new RealPacketReceiver(processor, CreateTestAddress(), socketFactory, logs, PacketQueueFullModes.DropNewest);

		// Safety net only - the test must pass via Dispose, NOT via this cancellation.
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var runTask = receiver.RunAsync(cts.Token);

		// Let the receive loop reach its blocking ReceiveAsync (no packets are ever enqueued).
		await Task.Delay(200, CancellationToken);

		// Dispose WITHOUT cancelling the token - this alone must stop the receiver.
		receiver.Dispose();

		var completed = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(3), CancellationToken));

		var stoppedByDispose = ReferenceEquals(completed, runTask);

		cts.Cancel();
		try { await runTask; } catch { }

		IsTrue(stoppedByDispose,
			"RunAsync did not complete after Dispose(): the receiver stayed blocked in " +
			"ReceiveAsync because Dispose does not interrupt the in-progress receive.");
	}

	/// <summary>
	/// UDP socket that fails on Bind to simulate a fatal startup error (e.g. address in use).
	/// </summary>
	private class FailingUdpSocket : Disposable, IUdpSocket
	{
		public Exception BindException { get; set; }

		public void Bind(EndPoint localEP)
		{
			if (BindException != null)
				throw BindException;
		}

		public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
		{
		}

		public void JoinMulticast(MulticastSourceAddress address)
		{
		}

		public void LeaveMulticast(MulticastSourceAddress address)
		{
		}

		public void LeaveMulticast(IPAddress groupAddress)
		{
		}

		public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
			=> throw new InvalidOperationException("socket not started");

		public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken)
			=> throw new InvalidOperationException("socket not started");
	}

	/// <summary>
	/// Socket factory returning a single pre-configured failing socket.
	/// </summary>
	private class FailingSocketFactory(IUdpSocket socket) : IUdpSocketFactory
	{
		public IUdpSocket Create(AddressFamily addressFamily = AddressFamily.InterNetwork)
			=> socket;
	}

	/// <summary>
	/// Packet processor that tracks rented-buffer allocation/disposal and throws from
	/// ProcessNewPacket before taking ownership, to expose buffer leaks on the exception path.
	/// </summary>
	private class LeakTrackingProcessor : IPacketProcessor
	{
		private int _allocated;
		private int _disposed;
		private int _errors;

		public int Allocated => Volatile.Read(ref _allocated);
		public int Disposed => Volatile.Read(ref _disposed);
		public int Errors => Volatile.Read(ref _errors);

		public Exception ThrowOnProcess { get; set; }

		public int MaxIncomingQueueSize { get; set; } = 10000;
		public int MaxUdpDatagramSize { get; set; } = 65535;
		public string Name => "LeakTracking";

		public IMemoryOwner<byte> AllocatePacket(int size)
		{
			Interlocked.Increment(ref _allocated);
			return new TrackingMemoryOwner(this, new byte[size]);
		}

		public ValueTask<bool> ProcessNewPacket(IMemoryOwner<byte> packet, int length, CancellationToken cancellationToken)
		{
			// Throw before taking ownership of the buffer (simulating a parse error).
			if (ThrowOnProcess != null)
				throw ThrowOnProcess;

			packet.Dispose();
			return new(true);
		}

		public void DisposePacket(IMemoryOwner<byte> packet, string reason)
			=> packet.Dispose();

		public void ErrorHandler(Exception ex, int errorCount, bool isFatal)
			=> Interlocked.Increment(ref _errors);

		public async Task<bool> WaitForErrorsAsync(int count, TimeSpan timeout)
		{
			using var cts = new CancellationTokenSource(timeout);
			try
			{
				while (Volatile.Read(ref _errors) < count && !cts.Token.IsCancellationRequested)
					await Task.Delay(10, cts.Token);

				return Volatile.Read(ref _errors) >= count;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
		}

		private class TrackingMemoryOwner(LeakTrackingProcessor owner, byte[] buffer) : IMemoryOwner<byte>
		{
			private int _disposed;

			public Memory<byte> Memory => buffer;

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _disposed, 1) == 0)
					Interlocked.Increment(ref owner._disposed);
			}
		}
	}
}
