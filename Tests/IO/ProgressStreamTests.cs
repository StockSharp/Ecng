namespace Ecng.Tests.IO;

using Ecng.IO;

#pragma warning disable CA2022 // Avoid inexact read - MemoryStream always returns requested bytes

[TestClass]
public class ProgressStreamTests : BaseTestClass
{
	[TestMethod]
	public void Read_ReportsProgress()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		var buffer = new byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public async Task ReadAsync_ReportsProgress()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		var buffer = new byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public void Write_ReportsProgress()
	{
		using var inner = new MemoryStream();
		var data = new byte[1000];
		RandomGen.GetBytes(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		for (var i = 0; i < data.Length; i += 100)
			stream.Write(data, i, Math.Min(100, data.Length - i));

		inner.ToArray().AssertEqual(data);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public async Task WriteAsync_ReportsProgress()
	{
		using var inner = new MemoryStream();
		var data = new byte[1000];
		RandomGen.GetBytes(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		for (var i = 0; i < data.Length; i += 100)
			await stream.WriteAsync(data, i, Math.Min(100, data.Length - i), CancellationToken);

		inner.ToArray().AssertEqual(data);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public void TrackReads_False_NoProgressOnRead()
	{
		var data = new byte[100];
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p), trackReads: false);

		var buffer = new byte[100];
		stream.Read(buffer, 0, buffer.Length);

		progressCalls.Count.AssertEqual(0, "No progress should be reported when trackReads is false");
	}

	[TestMethod]
	public void TrackWrites_False_NoProgressOnWrite()
	{
		using var inner = new MemoryStream();
		var data = new byte[100];

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p), trackWrites: false);

		stream.Write(data, 0, data.Length);

		progressCalls.Count.AssertEqual(0, "No progress should be reported when trackWrites is false");
	}

	[TestMethod]
	public void LeaveOpen_True_InnerStreamNotDisposed()
	{
		var inner = new MemoryStream(new byte[100]);
		var stream = new ProgressStream(inner, 100, _ => { }, leaveOpen: true);
		stream.Dispose();

		// Should not throw - inner stream is still open
		inner.Position.AssertEqual(0);
		inner.Dispose();
	}

	[TestMethod]
	public void LeaveOpen_False_InnerStreamDisposed()
	{
		var inner = new MemoryStream(new byte[100]);
		var stream = new ProgressStream(inner, 100, _ => { }, leaveOpen: false);
		stream.Dispose();

		// Should throw - inner stream is disposed
		ThrowsExactly<ObjectDisposedException>(() => _ = inner.Position);
	}

	[TestMethod]
	public void ProcessedBytes_TracksTotal()
	{
		var data = new byte[500];
		using var inner = new MemoryStream(data);
		using var stream = new ProgressStream(inner, data.Length, _ => { });

		var buffer = new byte[100];
		stream.Read(buffer, 0, 100);
		stream.ProcessedBytes.AssertEqual(100);

		stream.Read(buffer, 0, 100);
		stream.ProcessedBytes.AssertEqual(200);
	}

	[TestMethod]
	public void ProgressNotReportedTwice_ForSamePercent()
	{
		var data = new byte[1000];
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		// Read 10 bytes at a time (1% each)
		var buffer = new byte[10];
		for (var i = 0; i < 100; i++)
			stream.Read(buffer, 0, buffer.Length);

		// Each percentage should only be reported once
		var uniquePercents = progressCalls.Distinct().Count();
		uniquePercents.AssertEqual(progressCalls.Count, "Each percent should only be reported once");
	}

	[TestMethod]
	public void ProgressCappedAt100()
	{
		// Report less total than actual data
		var data = new byte[200];
		using var inner = new MemoryStream(data);

		var maxProgress = 0;
		using var stream = new ProgressStream(inner, 100, p => maxProgress = Math.Max(maxProgress, p));

		var buffer = new byte[200];
		stream.Read(buffer, 0, 200);

		maxProgress.AssertEqual(100, "Progress should be capped at 100");
	}

	[TestMethod]
	public void StreamProperties_PassThrough()
	{
		using var inner = new MemoryStream(new byte[100]);
		using var stream = new ProgressStream(inner, 100, _ => { });

		stream.CanRead.AssertEqual(inner.CanRead);
		stream.CanWrite.AssertEqual(inner.CanWrite);
		stream.CanSeek.AssertEqual(inner.CanSeek);
		stream.Length.AssertEqual(inner.Length);

		stream.Position = 50;
		stream.Position.AssertEqual(50);
		inner.Position.AssertEqual(50);
	}

	[TestMethod]
	public void Seek_Works()
	{
		using var inner = new MemoryStream(new byte[100]);
		using var stream = new ProgressStream(inner, 100, _ => { });

		stream.Seek(50, SeekOrigin.Begin);
		stream.Position.AssertEqual(50);

		stream.Seek(10, SeekOrigin.Current);
		stream.Position.AssertEqual(60);

		stream.Seek(-10, SeekOrigin.End);
		stream.Position.AssertEqual(90);
	}

#if NET6_0_OR_GREATER
	[TestMethod]
	public async Task ReadAsyncMemory_ReportsProgress()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		var buffer = new byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), CancellationToken)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public void ReadSpan_ReportsProgress()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		Span<byte> buffer = stackalloc byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = stream.Read(buffer)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}
#endif

	[TestMethod]
	public void ReadByte_ReportsProgress()
	{
		var data = new byte[100];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		for (var i = 0; i < data.Length; i++)
		{
			var b = stream.ReadByte();
			b.AssertEqual(data[i]);
		}

		stream.ReadByte().AssertEqual(-1); // EOF
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

#if NET6_0_OR_GREATER
	[TestMethod]
	public async Task WriteAsyncMemory_ReportsProgress()
	{
		using var inner = new MemoryStream();
		var data = new byte[1000];
		RandomGen.GetBytes(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		for (var i = 0; i < data.Length; i += 100)
		{
			var chunk = data.AsMemory(i, Math.Min(100, data.Length - i));
			await stream.WriteAsync(chunk, CancellationToken);
		}

		inner.ToArray().AssertEqual(data);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public void WriteSpan_ReportsProgress()
	{
		using var inner = new MemoryStream();
		var data = new byte[1000];
		RandomGen.GetBytes(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		for (var i = 0; i < data.Length; i += 100)
		{
			var chunk = data.AsSpan(i, Math.Min(100, data.Length - i));
			stream.Write(chunk);
		}

		inner.ToArray().AssertEqual(data);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}
#endif

	[TestMethod]
	public void WriteByte_ReportsProgress()
	{
		using var inner = new MemoryStream();
		var data = new byte[100];
		RandomGen.GetBytes(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		foreach (var b in data)
			stream.WriteByte(b);

		inner.ToArray().AssertEqual(data);
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public async Task CopyToAsync_ReportsProgress()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new MemoryStream(data);
		using var dest = new MemoryStream();

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		await stream.CopyToAsync(dest, 100, CancellationToken);

		dest.ToArray().AssertEqual(data);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public void NonSeekableStream_Works()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new NonSeekableStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		stream.CanSeek.AssertFalse("Should report non-seekable");

		var buffer = new byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		(progressCalls.Count > 0).AssertTrue("Progress should be reported");
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	[TestMethod]
	public async Task NonSeekableStream_AsyncWorks()
	{
		var data = new byte[1000];
		RandomGen.GetBytes(data);
		using var inner = new NonSeekableStream(data);

		var progressCalls = new List<int>();
		using var stream = new ProgressStream(inner, data.Length, p => progressCalls.Add(p));

		var buffer = new byte[100];
		var totalRead = 0;
		int bytesRead;
		while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken)) > 0)
			totalRead += bytesRead;

		totalRead.AssertEqual(data.Length);
		progressCalls.Last().AssertEqual(100, "Final progress should be 100");
	}

	private sealed class NonSeekableStream : Stream
	{
		private readonly byte[] _data;
		private int _position;

		public NonSeekableStream(byte[] data) => _data = data;

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var bytesToRead = Math.Min(count, _data.Length - _position);
			if (bytesToRead <= 0) return 0;

			Array.Copy(_data, _position, buffer, offset, bytesToRead);
			_position += bytesToRead;
			return bytesToRead;
		}

		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}
}
