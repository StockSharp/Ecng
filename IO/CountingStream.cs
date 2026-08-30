namespace Ecng.IO;

/// <summary>
/// Represents a stream wrapper that counts the bytes read from and written to the underlying stream.
/// </summary>
/// <remarks>
/// For a caller that has to say how much a connection has carried: the bytes are counted where they
/// pass, rather than added up by whoever happens to look at a buffer. Nothing is retained - use
/// <see cref="DumpableStream"/> when the data itself is wanted.
/// </remarks>
/// <param name="underlying">The underlying stream to wrap.</param>
/// <param name="leaveOpen">true to leave the underlying stream open after disposing; otherwise, false.</param>
public class CountingStream(Stream underlying, bool leaveOpen = false) : Stream
{
	private readonly Stream _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
	private readonly bool _leaveOpen = leaveOpen;

	private long _readCount;
	private long _writeCount;

	/// <summary>
	/// Gets the total number of bytes read from the underlying stream.
	/// </summary>
	public long ReadCount => Interlocked.Read(ref _readCount);

	/// <summary>
	/// Gets the total number of bytes written to the underlying stream.
	/// </summary>
	public long WriteCount => Interlocked.Read(ref _writeCount);

	/// <inheritdoc />
	public override bool CanRead => _underlying.CanRead;

	/// <inheritdoc />
	public override bool CanSeek => _underlying.CanSeek;

	/// <inheritdoc />
	public override bool CanWrite => _underlying.CanWrite;

	/// <inheritdoc />
	public override long Length => _underlying.Length;

	/// <inheritdoc />
	public override long Position
	{
		get => _underlying.Position;
		set => _underlying.Position = value;
	}

	/// <inheritdoc />
	public override int ReadTimeout
	{
		get => _underlying.ReadTimeout;
		set => _underlying.ReadTimeout = value;
	}

	/// <inheritdoc />
	public override int WriteTimeout
	{
		get => _underlying.WriteTimeout;
		set => _underlying.WriteTimeout = value;
	}

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count)
		=> CountRead(_underlying.Read(buffer, offset, count));

	/// <inheritdoc />
	public override int Read(Span<byte> buffer)
		=> CountRead(_underlying.Read(buffer));

	/// <inheritdoc />
	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> CountRead(await _underlying.ReadAsync(buffer.AsMemory(offset, count), cancellationToken));

	/// <inheritdoc />
	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		=> CountRead(await _underlying.ReadAsync(buffer, cancellationToken));

	/// <inheritdoc />
	public override int ReadByte()
	{
		var value = _underlying.ReadByte();

		if (value >= 0)
			CountRead(1);

		return value;
	}

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count)
	{
		_underlying.Write(buffer, offset, count);
		CountWrite(count);
	}

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_underlying.Write(buffer);
		CountWrite(buffer.Length);
	}

	/// <inheritdoc />
	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		await _underlying.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
		CountWrite(count);
	}

	/// <inheritdoc />
	public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		await _underlying.WriteAsync(buffer, cancellationToken);
		CountWrite(buffer.Length);
	}

	/// <inheritdoc />
	public override void WriteByte(byte value)
	{
		_underlying.WriteByte(value);
		CountWrite(1);
	}

	/// <inheritdoc />
	public override void Flush() => _underlying.Flush();

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken cancellationToken) => _underlying.FlushAsync(cancellationToken);

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) => _underlying.Seek(offset, origin);

	/// <inheritdoc />
	public override void SetLength(long value) => _underlying.SetLength(value);

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing && !_leaveOpen)
			_underlying.Dispose();

		base.Dispose(disposing);
	}

	private int CountRead(int read)
	{
		if (read > 0)
			Interlocked.Add(ref _readCount, read);

		return read;
	}

	private void CountWrite(int written)
	{
		if (written > 0)
			Interlocked.Add(ref _writeCount, written);
	}
}
