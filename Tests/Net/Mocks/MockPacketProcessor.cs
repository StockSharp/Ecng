namespace Ecng.Tests.Net;

using System.Buffers;

using Ecng.Net;

/// <summary>
/// Mock packet processor for testing.
/// </summary>
class MockPacketProcessor : IPacketProcessor
{
	private readonly List<(byte[] data, int length)> _processedPackets = [];
	private readonly List<(IMemoryOwner<byte> packet, string reason)> _disposedPackets = [];
	private readonly List<(Exception ex, int errorCount, bool isFatal)> _errors = [];

	/// <summary>
	/// Gets the processed packets.
	/// </summary>
	public IReadOnlyList<(byte[] data, int length)> ProcessedPackets => _processedPackets;

	/// <summary>
	/// Gets the disposed packets.
	/// </summary>
	public IReadOnlyList<(IMemoryOwner<byte> packet, string reason)> DisposedPackets => _disposedPackets;

	/// <summary>
	/// Gets the errors.
	/// </summary>
	public IReadOnlyList<(Exception ex, int errorCount, bool isFatal)> Errors => _errors;

	/// <summary>
	/// Gets or sets whether to continue processing after receiving a packet.
	/// </summary>
	public bool ContinueProcessing { get; set; } = true;

	/// <summary>
	/// Gets or sets the exception to throw during processing.
	/// </summary>
	public Exception ProcessException { get; set; }

	/// <summary>
	/// Gets or sets the packet size for allocation.
	/// </summary>
	public int PacketSize { get; set; } = 65535;

	/// <summary>
	/// Gets the number of packets processed.
	/// </summary>
	public int ProcessedCount => _processedPackets.Count;

	public int MaxIncomingQueueSize { get; set; } = 10000;

	public int MaxUdpDatagramSize { get; set; } = 65535;

	public string Name { get; } = nameof(MockPacketProcessor);

	/// <summary>
	/// Waits until the specified number of packets are processed.
	/// </summary>
	/// <param name="count">The number of packets to wait for.</param>
	/// <param name="timeout">The timeout.</param>
	/// <returns>True if the count was reached, false if timed out.</returns>
	public async Task<bool> WaitForPacketsAsync(int count, TimeSpan timeout)
	{
		var cts = new CancellationTokenSource(timeout);
		try
		{
			while (_processedPackets.Count < count && !cts.Token.IsCancellationRequested)
				await Task.Delay(10, cts.Token);

			return _processedPackets.Count >= count;
		}
		catch (OperationCanceledException)
		{
			return false;
		}
	}

	/// <inheritdoc />
	public ValueTask<bool> ProcessNewPacket(IMemoryOwner<byte> packet, int length, CancellationToken cancellationToken)
	{
		if (ProcessException != null)
			throw ProcessException;

		var data = packet.Memory.Span[..length].ToArray();
		_processedPackets.Add((data, length));

		return new(ContinueProcessing);
	}

	/// <inheritdoc />
	public IMemoryOwner<byte> AllocatePacket(int size)
	{
		return new SimpleMemoryOwner(new byte[size]);
	}

	/// <inheritdoc />
	public void DisposePacket(IMemoryOwner<byte> packet, string reason)
	{
		_disposedPackets.Add((packet, reason));
		packet.Dispose();
	}

	/// <inheritdoc />
	public void ErrorHandler(Exception ex, int errorCount, bool isFatal)
	{
		_errors.Add((ex, errorCount, isFatal));
	}

	/// <summary>
	/// Simple memory owner implementation for testing.
	/// </summary>
	private class SimpleMemoryOwner(byte[] buffer) : IMemoryOwner<byte>
	{
		public Memory<byte> Memory => buffer;
		public void Dispose() { }
	}
}
