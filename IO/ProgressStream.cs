namespace Ecng.IO;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Stream wrapper that reports progress during read or write operations.
/// </summary>
public class ProgressStream : Stream
{
	private readonly Stream _inner;
	private readonly long _totalBytes;
	private readonly Action<int> _progress;
	private readonly bool _trackReads;
	private readonly bool _trackWrites;
	private readonly bool _leaveOpen;

	private long _processedBytes;
	private int _lastReportedPercent;

	/// <summary>
	/// Initializes a new instance of <see cref="ProgressStream"/>.
	/// </summary>
	/// <param name="inner">The underlying stream to wrap.</param>
	/// <param name="totalBytes">Total bytes expected (for percentage calculation).</param>
	/// <param name="progress">Callback invoked with progress percentage (0-100).</param>
	/// <param name="trackReads">Track progress on read operations.</param>
	/// <param name="trackWrites">Track progress on write operations.</param>
	/// <param name="leaveOpen">Whether to leave the inner stream open when disposed.</param>
	public ProgressStream(
		Stream inner,
		long totalBytes,
		Action<int> progress,
		bool trackReads = true,
		bool trackWrites = true,
		bool leaveOpen = false)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_totalBytes = totalBytes;
		_progress = progress ?? throw new ArgumentNullException(nameof(progress));
		_trackReads = trackReads;
		_trackWrites = trackWrites;
		_leaveOpen = leaveOpen;
	}

	/// <summary>
	/// Gets the last reported progress percentage.
	/// </summary>
	public int LastReportedPercent => _lastReportedPercent;

	/// <summary>
	/// Gets the total bytes processed so far.
	/// </summary>
	public long ProcessedBytes => _processedBytes;

	private void ReportProgress(long bytes)
	{
		if (bytes <= 0) return;

		_processedBytes += bytes;
		var percent = _totalBytes > 0 ? (int)(_processedBytes * 100 / _totalBytes) : 0;
		if (percent > 100) percent = 100;

		if (percent != _lastReportedPercent)
		{
			_progress(percent);
			_lastReportedPercent = percent;
		}
	}

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count)
	{
		var bytesRead = _inner.Read(buffer, offset, count);
		if (_trackReads)
			ReportProgress(bytesRead);
		return bytesRead;
	}

	/// <inheritdoc />
	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		var bytesRead = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
		if (_trackReads)
			ReportProgress(bytesRead);
		return bytesRead;
	}

#if NET6_0_OR_GREATER
	/// <inheritdoc />
	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		var bytesRead = await _inner.ReadAsync(buffer, cancellationToken);
		if (_trackReads)
			ReportProgress(bytesRead);
		return bytesRead;
	}

	/// <inheritdoc />
	public override int Read(Span<byte> buffer)
	{
		var bytesRead = _inner.Read(buffer);
		if (_trackReads)
			ReportProgress(bytesRead);
		return bytesRead;
	}
#endif

	/// <inheritdoc />
	public override int ReadByte()
	{
		var result = _inner.ReadByte();
		if (result >= 0 && _trackReads)
			ReportProgress(1);
		return result;
	}

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count)
	{
		_inner.Write(buffer, offset, count);
		if (_trackWrites)
			ReportProgress(count);
	}

	/// <inheritdoc />
	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		await _inner.WriteAsync(buffer, offset, count, cancellationToken);
		if (_trackWrites)
			ReportProgress(count);
	}

#if NET6_0_OR_GREATER
	/// <inheritdoc />
	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		await _inner.WriteAsync(buffer, cancellationToken);
		if (_trackWrites)
			ReportProgress(buffer.Length);
	}

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_inner.Write(buffer);
		if (_trackWrites)
			ReportProgress(buffer.Length);
	}
#endif

	/// <inheritdoc />
	public override void WriteByte(byte value)
	{
		_inner.WriteByte(value);
		if (_trackWrites)
			ReportProgress(1);
	}

	/// <inheritdoc />
	public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		var buffer = new byte[bufferSize];
		int bytesRead;
		while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
		{
			await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
		}
	}

	/// <inheritdoc />
	public override bool CanRead => _inner.CanRead;

	/// <inheritdoc />
	public override bool CanSeek => _inner.CanSeek;

	/// <inheritdoc />
	public override bool CanWrite => _inner.CanWrite;

	/// <inheritdoc />
	public override long Length => _inner.Length;

	/// <inheritdoc />
	public override long Position
	{
		get => _inner.Position;
		set => _inner.Position = value;
	}

	/// <inheritdoc />
	public override void Flush() => _inner.Flush();

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

	/// <inheritdoc />
	public override void SetLength(long value) => _inner.SetLength(value);

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing && !_leaveOpen)
			_inner.Dispose();

		base.Dispose(disposing);
	}
}
