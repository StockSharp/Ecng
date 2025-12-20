namespace Ecng.Tests.Common;

[TestClass]
public class DumpableStreamTests : BaseTestClass
{
	[TestMethod]
	public void GetReadDump_ReturnsEmptyArray_WhenNoDataRead()
	{
		using var ms = new MemoryStream([1, 2, 3, 4, 5]);
		using var dump = new DumpableStream(ms);

		var readDump = dump.GetReadDump();

		readDump.AssertNotNull();
		readDump.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetWriteDump_ReturnsEmptyArray_WhenNoDataWritten()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var writeDump = dump.GetWriteDump();

		writeDump.AssertNotNull();
		writeDump.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetReadDump_CapturesReadData()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[3];
		var bytesRead = dump.Read(buffer, 0, 3);

		bytesRead.AssertEqual(3);
		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(3);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
	}

	[TestMethod]
	public void GetWriteDump_CapturesWrittenData()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var testData = new byte[] { 10, 20, 30 };
		dump.Write(testData, 0, 3);

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(3);
		writeDump[0].AssertEqual((byte)10);
		writeDump[1].AssertEqual((byte)20);
		writeDump[2].AssertEqual((byte)30);
	}

	[TestMethod]
	public void GetReadDump_AccumulatesMultipleReads()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[3];
		var read1 = dump.Read(buffer, 0, 3); // Read 1,2,3
		var read2 = dump.Read(buffer, 0, 2); // Read 4,5

		read1.AssertEqual(3);
		read2.AssertEqual(2);

		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(5);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
		readDump[3].AssertEqual((byte)4);
		readDump[4].AssertEqual((byte)5);
	}

	[TestMethod]
	public void GetWriteDump_AccumulatesMultipleWrites()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.Write([10, 20, 30], 0, 3);
		dump.Write([40, 50], 0, 2);

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(5);
		writeDump[0].AssertEqual((byte)10);
		writeDump[1].AssertEqual((byte)20);
		writeDump[2].AssertEqual((byte)30);
		writeDump[3].AssertEqual((byte)40);
		writeDump[4].AssertEqual((byte)50);
	}

	[TestMethod]
	public void Read_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[5];
		var bytesRead = dump.Read(buffer, 0, 5);

		bytesRead.AssertEqual(5);
		buffer.AssertEqual(testData);
	}

	[TestMethod]
	public void Write_ProxiesToUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var testData = new byte[] { 1, 2, 3 };
		dump.Write(testData, 0, 3);

		ms.Position = 0;
		var result = ms.ToArray();
		result.Length.AssertEqual(3);
		result.AssertEqual(testData);
	}

	[TestMethod]
	public void Position_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		dump.Position.AssertEqual(0L);

		var buffer = new byte[3];
		var bytesRead = dump.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);

		dump.Position.AssertEqual(3L);

		dump.Position = 1;
		dump.Position.AssertEqual(1L);
		ms.Position.AssertEqual(1L);
	}

	[TestMethod]
	public void Seek_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var newPos = dump.Seek(2, SeekOrigin.Begin);
		newPos.AssertEqual(2L);
		dump.Position.AssertEqual(2L);

		var buffer = new byte[1];
		var bytesRead = dump.Read(buffer, 0, 1);
		bytesRead.AssertEqual(1);
		buffer[0].AssertEqual((byte)3); // Should read byte at position 2
	}

	[TestMethod]
	public void Length_ReflectsUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		dump.Length.AssertEqual(5L);
	}

	[TestMethod]
	public void CanRead_ReflectsUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.CanRead.AssertEqual(ms.CanRead);
	}

	[TestMethod]
	public void CanWrite_ReflectsUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.CanWrite.AssertEqual(ms.CanWrite);
	}

	[TestMethod]
	public void CanSeek_ReflectsUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.CanSeek.AssertEqual(ms.CanSeek);
	}

	[TestMethod]
	public void ReadWithOffset_CapturesCorrectData()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[10];
		var bytesRead = dump.Read(buffer, 5, 3); // Read into buffer starting at offset 5

		bytesRead.AssertEqual(3);
		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(3);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
	}

	[TestMethod]
	public void WriteWithOffset_CapturesCorrectData()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var testData = new byte[] { 99, 98, 10, 20, 30, 97 };
		dump.Write(testData, 2, 3); // Write only bytes 10, 20, 30

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(3);
		writeDump[0].AssertEqual((byte)10);
		writeDump[1].AssertEqual((byte)20);
		writeDump[2].AssertEqual((byte)30);
	}

	[TestMethod]
	public void Flush_CallsUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		// Should not throw
		dump.Flush();
	}

	[TestMethod]
	public void SetLength_ModifiesUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.SetLength(100);

		dump.Length.AssertEqual(100L);
		ms.Length.AssertEqual(100L);
	}

	[TestMethod]
	public void ReadAndWrite_BothDumpsAreIndependent()
	{
		using var ms = new MemoryStream([1, 2, 3]);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[3];
		var bytesRead = dump.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);

		ms.Position = 0;
		dump.Write([10, 20], 0, 2);

		var readDump = dump.GetReadDump();
		var writeDump = dump.GetWriteDump();

		readDump.Length.AssertEqual(3);
		writeDump.Length.AssertEqual(2);

		readDump[0].AssertEqual((byte)1);
		writeDump[0].AssertEqual((byte)10);
	}

	[TestMethod]
	public void LargeData_IsProperlyDumped()
	{
		var largeData = new byte[10000];
		for (var i = 0; i < largeData.Length; i++)
			largeData[i] = (byte)(i % 256);

		using var ms = new MemoryStream(largeData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[10000];
		var bytesRead = dump.Read(buffer, 0, 10000);

		bytesRead.AssertEqual(10000);
		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(10000);
		readDump.AssertEqual(largeData);
	}

	[TestMethod]
	public void EmptyRead_DoesNotAffectDump()
	{
		using var ms = new MemoryStream([1, 2, 3]);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[10];
		var bytesRead = dump.Read(buffer, 0, 0); // Read 0 bytes
		bytesRead.AssertEqual(0);

		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(0);
	}

	[TestMethod]
	public void EmptyWrite_DoesNotAffectDump()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		dump.Write([1, 2, 3], 0, 0); // Write 0 bytes

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ReadAsync_CapturesReadData()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[3];
		var bytesRead = await dump.ReadAsync(buffer.AsMemory(0, 3), CancellationToken);

		bytesRead.AssertEqual(3);
		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(3);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
	}

	[TestMethod]
	public async Task WriteAsync_CapturesWrittenData()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var testData = new byte[] { 10, 20, 30 };
		await dump.WriteAsync(testData.AsMemory(0, 3), CancellationToken);

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(3);
		writeDump[0].AssertEqual((byte)10);
		writeDump[1].AssertEqual((byte)20);
		writeDump[2].AssertEqual((byte)30);
	}

	[TestMethod]
	public async Task ReadAsync_AccumulatesMultipleReads()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[5];
		var read1 = await dump.ReadAsync(buffer.AsMemory(0, 3), CancellationToken);
		var read2 = await dump.ReadAsync(buffer.AsMemory(0, 2), CancellationToken);

		read1.AssertEqual(3);
		read2.AssertEqual(2);

		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(5);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
		readDump[3].AssertEqual((byte)4);
		readDump[4].AssertEqual((byte)5);
	}

	[TestMethod]
	public async Task WriteAsync_AccumulatesMultipleWrites()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		await dump.WriteAsync([10, 20, 30], 0, 3, CancellationToken);
		await dump.WriteAsync([40, 50], 0, 2, CancellationToken);

		var writeDump = dump.GetWriteDump();
		writeDump.Length.AssertEqual(5);
		writeDump[0].AssertEqual((byte)10);
		writeDump[1].AssertEqual((byte)20);
		writeDump[2].AssertEqual((byte)30);
		writeDump[3].AssertEqual((byte)40);
		writeDump[4].AssertEqual((byte)50);
	}

	[TestMethod]
	public async Task ReadAsync_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[5];
		var bytesRead = await dump.ReadAsync(buffer.AsMemory(0, 5), CancellationToken);

		bytesRead.AssertEqual(5);
		buffer.AssertEqual(testData);
	}

	[TestMethod]
	public async Task WriteAsync_ProxiesToUnderlyingStream()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		var testData = new byte[] { 1, 2, 3 };
		await dump.WriteAsync(testData.AsMemory(0, 3), CancellationToken);

		ms.Position = 0;
		var result = ms.ToArray();
		result.Length.AssertEqual(3);
		result.AssertEqual(testData);
	}

	[TestMethod]
	public async Task FlushAsync_DoesNotThrow()
	{
		using var ms = new MemoryStream();
		using var dump = new DumpableStream(ms);

		await dump.WriteAsync([1, 2, 3], 0, 3, CancellationToken);
		await dump.FlushAsync(CancellationToken);

		// Should not throw
	}

	[TestMethod]
	public async Task MixedSyncAndAsyncOperations()
	{
		using var ms = new MemoryStream([1, 2, 3, 4, 5, 6]);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[10];
		var read1 = dump.Read(buffer, 0, 2); // Sync read
		var read2 = await dump.ReadAsync(buffer.AsMemory(2, 2), CancellationToken); // Async read

		read1.AssertEqual(2);
		read2.AssertEqual(2);

		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(4);
		readDump[0].AssertEqual((byte)1);
		readDump[1].AssertEqual((byte)2);
		readDump[2].AssertEqual((byte)3);
		readDump[3].AssertEqual((byte)4);
	}

	[TestMethod]
	public async Task LargeDataAsync_IsProperlyDumped()
	{
		var largeData = new byte[10000];
		for (var i = 0; i < largeData.Length; i++)
			largeData[i] = (byte)(i % 256);

		using var ms = new MemoryStream(largeData);
		using var dump = new DumpableStream(ms);

		var buffer = new byte[10000];
		var bytesRead = await dump.ReadAsync(buffer.AsMemory(0, 10000), CancellationToken);

		bytesRead.AssertEqual(10000);
		var readDump = dump.GetReadDump();
		readDump.Length.AssertEqual(10000);
		readDump.AssertEqual(largeData);
	}

	[TestMethod]
	public async Task CancellationToken_IsRespected()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		using var dump = new DumpableStream(ms);
		using var cts = new CancellationTokenSource();

		var buffer = new byte[5];
		var bytesRead = await dump.ReadAsync(buffer.AsMemory(0, 5), cts.Token);

		bytesRead.AssertEqual(5);
	}

	[TestMethod]
	public void Dispose_DisposesUnderlyingStream()
	{
		// This test verifies that disposing DumpableStream also disposes underlying stream
		// Without the Dispose(bool) override, underlying stream was NOT disposed
		var ms = new MemoryStream([1, 2, 3]);
		var dump = new DumpableStream(ms);

		// Stream should be accessible before dispose
		ms.CanRead.AssertTrue();

		dump.Dispose();

		// After DumpableStream is disposed, underlying stream should also be disposed
		// Disposed MemoryStream throws ObjectDisposedException on access
		ThrowsExactly<ObjectDisposedException>(() => _ = ms.Length);
	}

	[TestMethod]
	public void Dispose_LeaveOpenTrue_DoesNotDisposeUnderlyingStream()
	{
		var ms = new MemoryStream([1, 2, 3]);
		var dump = new DumpableStream(ms, leaveOpen: true);

		dump.Dispose();

		// Underlying stream should still be accessible
		ms.CanRead.AssertTrue();
		ms.Length.AssertEqual(3);

		// Clean up
		ms.Dispose();
	}

	[TestMethod]
	public void Dispose_LeaveOpenFalse_DisposesUnderlyingStream()
	{
		var ms = new MemoryStream([1, 2, 3]);
		var dump = new DumpableStream(ms, leaveOpen: false);

		dump.Dispose();

		// Underlying stream should be disposed
		ThrowsExactly<ObjectDisposedException>(() => _ = ms.Length);
	}
}
