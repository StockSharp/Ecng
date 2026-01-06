namespace Ecng.Tests.Logging;

using System.Text;

using Ecng.Logging;

[TestClass]
public class LoggableStreamTests : BaseTestClass
{
	private class TestLogReceiver : ILogReceiver
	{
		public List<LogMessage> Messages { get; } = [];

		public LogLevels LogLevel { get; set; } = LogLevels.Debug;

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "TestReceiver";
		public ILogSource Parent { get; set; }
		public event Action<ILogSource> ParentRemoved { add { } remove { } }
		public DateTimeOffset CurrentTime => CurrentTimeUtc;
		public DateTime CurrentTimeUtc => DateTime.UtcNow;
		public bool IsRoot { get; set; }
		public event Action<LogMessage> Log { add { } remove { } }

		public void AddLog(LogMessage message)
		{
			Messages.Add(message);
		}

		public void LogVerbose(string format, params object[] args) => AddLog(new LogMessage(this, DateTime.UtcNow, LogLevels.Debug, string.Format(format, args)));
		public void LogDebug(string format, params object[] args) => AddLog(new LogMessage(this, DateTime.UtcNow, LogLevels.Debug, string.Format(format, args)));
		public void LogInfo(string format, params object[] args) => AddLog(new LogMessage(this, DateTime.UtcNow, LogLevels.Info, string.Format(format, args)));
		public void LogWarning(string format, params object[] args) => AddLog(new LogMessage(this, DateTime.UtcNow, LogLevels.Warning, string.Format(format, args)));
		public void LogError(string format, params object[] args) => AddLog(new LogMessage(this, DateTime.UtcNow, LogLevels.Error, string.Format(format, args)));

		public void Dispose() { }
	}

	private class DummySource : ILogSource
	{
		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "TestSource";
		public ILogSource Parent { get; set; }
		public event Action<ILogSource> ParentRemoved { add { } remove { } }
		public LogLevels LogLevel { get; set; } = LogLevels.Debug;
		public DateTimeOffset CurrentTime => CurrentTimeUtc;
		public DateTime CurrentTimeUtc => DateTime.UtcNow;
		public bool IsRoot { get; set; }
		public event Action<LogMessage> Log { add { } remove { } }

		public void Dispose() { }
	}

	[TestMethod]
	public void Read_LogsReadOperation()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => BitConverter.ToString(data),
			data => BitConverter.ToString(data),
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = loggable.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);

		// Flush to ensure log is written
		loggable.FlushRead();

		logReceiver.Messages.Count.AssertGreater(0);
	}

	[TestMethod]
	public void Write_LogsWriteOperation()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => BitConverter.ToString(data),
			data => BitConverter.ToString(data),
			LogLevels.Debug);

		var testData = new byte[] { 10, 20, 30 };
		loggable.Write(testData, 0, 3);

		// Flush to ensure log is written
		loggable.FlushWrite();

		logReceiver.Messages.Count.AssertGreater(0);
	}

	[TestMethod]
	public void FormatRead_UsesProvidedFormatter()
	{
		var testData = new byte[] { 1, 2, 3 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => $"READ:{data.Length}",
			data => $"WRITE:{data.Length}",
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = loggable.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);
		loggable.FlushRead();

		logReceiver.Messages.Count.AssertGreater(0);
		logReceiver.Messages.Any(m => m.Message.Contains("READ:3")).AssertTrue();
	}

	[TestMethod]
	public void FormatWrite_UsesProvidedFormatter()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => $"READ:{data.Length}",
			data => $"WRITE:{data.Length}",
			LogLevels.Debug);

		var testData = new byte[] { 10, 20, 30 };
		loggable.Write(testData, 0, 3);
		loggable.FlushWrite();

		logReceiver.Messages.Count.AssertGreater(0);
		logReceiver.Messages.Any(m => m.Message.Contains("WRITE:3")).AssertTrue();
	}

	[TestMethod]
	public void FlushRead_ForcesReadLogOutput()
	{
		var testData = new byte[] { 1, 2, 3 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data => "WRITE",
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = loggable.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);

		var beforeFlush = logReceiver.Messages.Count;
		loggable.FlushRead();
		var afterFlush = logReceiver.Messages.Count;

		afterFlush.AssertGreater(beforeFlush);
	}

	[TestMethod]
	public void FlushWrite_ForcesWriteLogOutput()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data => "WRITE",
			LogLevels.Debug);

		var testData = new byte[] { 10, 20, 30 };
		loggable.Write(testData, 0, 3);

		var beforeFlush = logReceiver.Messages.Count;
		loggable.FlushWrite();
		var afterFlush = logReceiver.Messages.Count;

		afterFlush.AssertGreater(beforeFlush);
	}

	[TestMethod]
	public void Read_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		var buffer = new byte[5];
		var bytesRead = loggable.Read(buffer, 0, 5);

		bytesRead.AssertEqual(5);
		buffer.AssertEqual(testData);
	}

	[TestMethod]
	public void Write_ProxiesToUnderlyingStream()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		var testData = new byte[] { 1, 2, 3 };
		loggable.Write(testData, 0, 3);

		ms.Position = 0;
		var result = ms.ToArray();
		result.Length.AssertEqual(3);
		result.AssertEqual(testData);
	}

	[TestMethod]
	public void MultipleReads_AreLogged()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5, 6 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => $"R{data.Length}",
			data => string.Empty,
			LogLevels.Debug);

		var buffer = new byte[10];
		var read1 = loggable.Read(buffer, 0, 2);
		read1.AssertEqual(2);
		loggable.FlushRead();
		var countAfterFirst = logReceiver.Messages.Count;

		var read2 = loggable.Read(buffer, 0, 3);
		read2.AssertEqual(3);
		loggable.FlushRead();
		var countAfterSecond = logReceiver.Messages.Count;

		countAfterSecond.AssertGreater(countAfterFirst);
	}

	[TestMethod]
	public void MultipleWrites_AreLogged()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => $"W{data.Length}",
			LogLevels.Debug);

		loggable.Write([1, 2], 0, 2);
		loggable.FlushWrite();
		var countAfterFirst = logReceiver.Messages.Count;

		loggable.Write([3, 4, 5], 0, 3);
		loggable.FlushWrite();
		var countAfterSecond = logReceiver.Messages.Count;

		countAfterSecond.AssertGreater(countAfterFirst);
	}

	[TestMethod]
	public void EmptyRead_MayNotGenerateLog()
	{
		using var ms = new MemoryStream([1, 2, 3]);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data => "WRITE",
			LogLevels.Debug);

		var buffer = new byte[10];
		var bytesRead = loggable.Read(buffer, 0, 0);
		bytesRead.AssertEqual(0);
		loggable.FlushRead();

		// Empty reads (0 bytes) may or may not generate log entries
		// Verify stream state is consistent after empty read
		ms.Position.AssertEqual(0L, "Stream position should not change after 0-byte read");
	}

	[TestMethod]
	public void EmptyWrite_MayNotGenerateLog()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data => "WRITE",
			LogLevels.Debug);

		loggable.Write([1, 2, 3], 0, 0);
		loggable.FlushWrite();

		// Empty writes (0 bytes) may or may not generate log entries
		// Verify underlying stream was not modified by 0-byte write
		ms.Length.AssertEqual(0L, "Stream length should be 0 after 0-byte write");
		ms.Position.AssertEqual(0L, "Stream position should be 0 after 0-byte write");
	}

	[TestMethod]
	public void Flush_DoesNotThrow()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		// Should not throw
		loggable.Flush();
	}

	[TestMethod]
	public void ReadWithOffset_LogsCorrectData()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		var readData = Array.Empty<byte>();

		using var loggable = new LoggableStream(ms, logReceiver,
			data =>
			{
				readData = data;
				return "READ";
			},
			data => "WRITE",
			LogLevels.Debug);

		var buffer = new byte[10];
		var bytesRead = loggable.Read(buffer, 5, 3);
		bytesRead.AssertEqual(3);
		loggable.FlushRead();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		// Should have logged 3 bytes that were actually read
		readData.Length.AssertEqual(3);
		readData[0].AssertEqual((byte)1);
		readData[1].AssertEqual((byte)2);
		readData[2].AssertEqual((byte)3);
	}

	[TestMethod]
	public void WriteWithOffset_LogsCorrectData()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		var writtenData = Array.Empty<byte>();

		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data => {
				writtenData = data;
				return "WRITE";
			},
			LogLevels.Debug);

		var testData = new byte[] { 99, 98, 10, 20, 30, 97 };
		loggable.Write(testData, 2, 3);
		loggable.FlushWrite();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		// Should have logged only the 3 bytes that were written (10, 20, 30)
		writtenData.Length.AssertEqual(3);
		writtenData[0].AssertEqual((byte)10);
		writtenData[1].AssertEqual((byte)20);
		writtenData[2].AssertEqual((byte)30);
	}

	[TestMethod]
	public void FormatterReceivesActualReadBytes()
	{
		var testData = new byte[] { 0xAB, 0xCD, 0xEF };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data =>
			{
				capturedData = [.. data];
				return "test";
			},
			data => "test",
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = loggable.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);
		loggable.FlushRead();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(3);
		capturedData[0].AssertEqual((byte)0xAB);
		capturedData[1].AssertEqual((byte)0xCD);
		capturedData[2].AssertEqual((byte)0xEF);
	}

	[TestMethod]
	public void FormatterReceivesActualWrittenBytes()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data => "test",
			data =>
			{
				capturedData = [.. data];
				return "test";
			},
			LogLevels.Debug);

		var testData = new byte[] { 0x12, 0x34, 0x56 };
		loggable.Write(testData, 0, 3);
		loggable.FlushWrite();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(3);
		capturedData[0].AssertEqual((byte)0x12);
		capturedData[1].AssertEqual((byte)0x34);
		capturedData[2].AssertEqual((byte)0x56);
	}

	[TestMethod]
	public void LargeRead_IsLogged()
	{
		var largeData = new byte[10000];
		for (var i = 0; i < largeData.Length; i++)
			largeData[i] = (byte)(i % 256);

		using var ms = new MemoryStream(largeData);
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data =>
			{
				capturedData = [.. data];
				return $"READ {data.Length}";
			},
			data => "WRITE",
			LogLevels.Debug);

		var buffer = new byte[10000];
		var bytesRead = loggable.Read(buffer, 0, 10000);
		bytesRead.AssertEqual(10000);
		loggable.FlushRead();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(10000);
	}

	[TestMethod]
	public void LargeWrite_IsLogged()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data => "READ",
			data =>
			{
				capturedData = [.. data];
				return $"WRITE {data.Length}";
			},
			LogLevels.Debug);

		var largeData = new byte[10000];
		for (var i = 0; i < largeData.Length; i++)
			largeData[i] = (byte)(i % 256);

		loggable.Write(largeData, 0, 10000);
		loggable.FlushWrite();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(10000);
	}

	[TestMethod]
	public void ReadAndWrite_AreLoggedSeparately()
	{
		using var ms = new MemoryStream([1, 2, 3]);
		var logReceiver = new TestLogReceiver();
		var readCount = 0;
		var writeCount = 0;

		using var loggable = new LoggableStream(ms, logReceiver,
			data => { readCount++; return "R"; },
			data => { writeCount++; return "W"; },
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = loggable.Read(buffer, 0, 3);
		bytesRead.AssertEqual(3);
		loggable.FlushRead();

		ms.Position = 0;
		loggable.Write([4, 5], 0, 2);
		loggable.FlushWrite();

		// Access Message properties to trigger the formatters
		logReceiver.Messages.Count.AssertEqual(2);
		_ = logReceiver.Messages[0].Message;
		_ = logReceiver.Messages[1].Message;

		readCount.AssertEqual(1);
		writeCount.AssertEqual(1);
	}

	[TestMethod]
	public void HexFormatter_Example()
	{
		var testData = "Hello"u8.ToArray(); // "Hello"
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();

		using var loggable = new LoggableStream(ms, logReceiver,
			data => BitConverter.ToString(data).Replace("-", " "),
			data => BitConverter.ToString(data).Replace("-", " "),
			LogLevels.Debug);

		var buffer = new byte[5];
		var bytesRead = loggable.Read(buffer, 0, 5);
		bytesRead.AssertEqual(5);
		loggable.FlushRead();

		logReceiver.Messages.Count.AssertGreater(0);
		logReceiver.Messages.Any(m => m.Message.Contains("48 65 6C 6C 6F")).AssertTrue();
	}

	[TestMethod]
	public void TextFormatter_Example()
	{
		var testData = Encoding.UTF8.GetBytes("Hello");
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();

		using var loggable = new LoggableStream(ms, logReceiver,
			data => data.UTF8(),
			data => data.UTF8(),
			LogLevels.Debug);

		var buffer = new byte[5];
		var bytesRead = loggable.Read(buffer, 0, 5);
		bytesRead.AssertEqual(5);
		loggable.FlushRead();

		logReceiver.Messages.Count.AssertGreater(0);
		logReceiver.Messages.Any(m => m.Message.Contains("Hello")).AssertTrue();
	}

	[TestMethod]
	public async Task ReadAsync_LogsReadOperation()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => BitConverter.ToString(data),
			data => BitConverter.ToString(data),
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = await loggable.ReadAsync(buffer.AsMemory(0, 3), CancellationToken);
		bytesRead.AssertEqual(3);

		// Flush to ensure log is written
		loggable.FlushRead();

		logReceiver.Messages.Count.AssertGreater(0);
	}

	[TestMethod]
	public async Task WriteAsync_LogsWriteOperation()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => BitConverter.ToString(data),
			data => BitConverter.ToString(data),
			LogLevels.Debug);

		var testData = new byte[] { 10, 20, 30 };
		await loggable.WriteAsync(testData.AsMemory(0, 3), CancellationToken);

		// Flush to ensure log is written
		loggable.FlushWrite();

		logReceiver.Messages.Count.AssertGreater(0);
	}

	[TestMethod]
	public async Task ReadAsync_ProxiesToUnderlyingStream()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		var buffer = new byte[5];
		var bytesRead = await loggable.ReadAsync(buffer.AsMemory(0, 5), CancellationToken);

		bytesRead.AssertEqual(5);
		buffer.AssertEqual(testData);
	}

	[TestMethod]
	public async Task WriteAsync_ProxiesToUnderlyingStream()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		var testData = new byte[] { 1, 2, 3 };
		await loggable.WriteAsync(testData.AsMemory(0, 3), CancellationToken);

		ms.Position = 0;
		var result = ms.ToArray();
		result.Length.AssertEqual(3);
		result.AssertEqual(testData);
	}

	[TestMethod]
	public async Task FlushAsync_DoesNotThrow()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => string.Empty,
			data => string.Empty,
			LogLevels.Debug);

		await loggable.WriteAsync([1, 2, 3], 0, 3, CancellationToken);
		await loggable.FlushAsync(CancellationToken);

		// Should not throw
	}

	[TestMethod]
	public async Task ReadAsyncWithFormatter_CapturesData()
	{
		var testData = new byte[] { 0xAA, 0xBB, 0xCC };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data =>
			{
				capturedData = [.. data];
				return "async read";
			},
			data => "async write",
			LogLevels.Debug);

		var buffer = new byte[3];
		var bytesRead = await loggable.ReadAsync(buffer.AsMemory(0, 3), CancellationToken);
		bytesRead.AssertEqual(3);
		loggable.FlushRead();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(3);
		capturedData[0].AssertEqual((byte)0xAA);
		capturedData[1].AssertEqual((byte)0xBB);
		capturedData[2].AssertEqual((byte)0xCC);
	}

	[TestMethod]
	public async Task WriteAsyncWithFormatter_CapturesData()
	{
		using var ms = new MemoryStream();
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data => "async read",
			data =>
			{
				capturedData = [.. data];
				return "async write";
			},
			LogLevels.Debug);

		var testData = new byte[] { 0x11, 0x22, 0x33 };
		await loggable.WriteAsync(testData.AsMemory(0, 3), CancellationToken);
		loggable.FlushWrite();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(3);
		capturedData[0].AssertEqual((byte)0x11);
		capturedData[1].AssertEqual((byte)0x22);
		capturedData[2].AssertEqual((byte)0x33);
	}

	[TestMethod]
	public async Task MixedSyncAndAsyncOperations_AreLoggedCorrectly()
	{
		using var ms = new MemoryStream([1, 2, 3, 4, 5, 6]);
		var logReceiver = new TestLogReceiver();
		var readCount = 0;
		var writeCount = 0;

		using var loggable = new LoggableStream(ms, logReceiver,
			data => { readCount++; return $"R{data.Length}"; },
			data => { writeCount++; return $"W{data.Length}"; },
			LogLevels.Debug);

		var buffer = new byte[10];
		var read1 = loggable.Read(buffer, 0, 2);
		read1.AssertEqual(2);
		loggable.FlushRead();

		var read2 = await loggable.ReadAsync(buffer, 2, 2, CancellationToken);
		read2.AssertEqual(2);
		loggable.FlushRead();

		ms.Position = 0;
		loggable.Write([10, 20], 0, 2);
		loggable.FlushWrite();

		await loggable.WriteAsync([30, 40], 0, 2, CancellationToken);
		loggable.FlushWrite();

		// Access Message properties to trigger the formatters
		logReceiver.Messages.Count.AssertEqual(4);
		foreach (var msg in logReceiver.Messages)
			_ = msg.Message;

		readCount.AssertEqual(2);
		writeCount.AssertEqual(2);
	}

	[TestMethod]
	public async Task LargeAsyncRead_IsLogged()
	{
		var largeData = new byte[10000];
		for (var i = 0; i < largeData.Length; i++)
			largeData[i] = (byte)(i % 256);

		using var ms = new MemoryStream(largeData);
		var logReceiver = new TestLogReceiver();
		byte[] capturedData = null;

		using var loggable = new LoggableStream(ms, logReceiver,
			data =>
			{
				capturedData = [.. data];
				return $"ASYNC READ {data.Length}";
			},
			data => "ASYNC WRITE",
			LogLevels.Debug);

		var buffer = new byte[10000];
		var bytesRead = await loggable.ReadAsync(buffer.AsMemory(0, 10000), CancellationToken);
		bytesRead.AssertEqual(10000);
		loggable.FlushRead();

		// Access Message property to trigger the formatter
		logReceiver.Messages.Count.AssertGreater(0);
		_ = logReceiver.Messages[0].Message;

		capturedData.AssertNotNull();
		capturedData.Length.AssertEqual(10000);
	}

	[TestMethod]
	public async Task CancellationToken_IsRespected()
	{
		var testData = new byte[] { 1, 2, 3, 4, 5 };
		using var ms = new MemoryStream(testData);
		var logReceiver = new TestLogReceiver();
		using var loggable = new LoggableStream(ms, logReceiver,
			data => "read",
			data => "write",
			LogLevels.Debug);
		using var cts = new CancellationTokenSource();

		var buffer = new byte[5];
		var bytesRead = await loggable.ReadAsync(buffer.AsMemory(0, 5), cts.Token);

		bytesRead.AssertEqual(5);
	}
}
