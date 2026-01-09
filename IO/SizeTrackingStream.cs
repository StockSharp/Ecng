namespace Ecng.IO;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Stream wrapper that tracks write operations against a file system size limit.
/// Checks limits during Write (fail-fast), not during Dispose.
/// </summary>
/// <remarks>
/// For file overwrites, the old file size is "credited" upfront, so writes are checked
/// against the effective available space. On dispose, the actual space adjustment is made.
/// </remarks>
internal class SizeTrackingStream : Stream
{
	private readonly Stream _inner;
	private readonly Func<long, long, bool> _checkAndReserve;
	private readonly Action<long, long> _commitSize;
	private readonly FileSystemOverflowBehavior _behavior;
	private readonly long _oldFileSize;

	private long _writtenBytes;
	private bool _disposed;
	private bool _limitExceeded;

	/// <summary>
	/// Creates a new size tracking stream wrapper.
	/// </summary>
	/// <param name="inner">The underlying stream to wrap.</param>
	/// <param name="checkAndReserve">
	/// Callback to check if write is allowed. Takes (bytesToWrite, oldFileSize), returns true if allowed.
	/// For EvictOldest, this callback should attempt eviction before returning false.
	/// </param>
	/// <param name="commitSize">
	/// Callback to commit the final size change. Takes (newSize, oldSize).
	/// Called on dispose to update TotalSize.
	/// </param>
	/// <param name="behavior">Overflow behavior when limit is exceeded.</param>
	/// <param name="oldFileSize">Previous file size (credited for overwrites).</param>
	public SizeTrackingStream(
		Stream inner,
		Func<long, long, bool> checkAndReserve,
		Action<long, long> commitSize,
		FileSystemOverflowBehavior behavior,
		long oldFileSize)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_checkAndReserve = checkAndReserve ?? throw new ArgumentNullException(nameof(checkAndReserve));
		_commitSize = commitSize ?? throw new ArgumentNullException(nameof(commitSize));
		_behavior = behavior;
		_oldFileSize = oldFileSize;
	}

	/// <inheritdoc />
	public override void Write(byte[] buffer, int offset, int count)
	{
		if (_limitExceeded)
			return; // Already failed, ignore subsequent writes

		if (!_checkAndReserve(_writtenBytes + count, _oldFileSize))
		{
			_limitExceeded = true;

			if (_behavior == FileSystemOverflowBehavior.ThrowException ||
			    _behavior == FileSystemOverflowBehavior.EvictOldest)
				throw new IOException("File system size limit exceeded.");

			return; // IgnoreWrites - silently drop
		}

		_writtenBytes += count;
		_inner.Write(buffer, offset, count);
	}

	/// <inheritdoc />
	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (_limitExceeded)
			return;

		if (!_checkAndReserve(_writtenBytes + count, _oldFileSize))
		{
			_limitExceeded = true;

			if (_behavior == FileSystemOverflowBehavior.ThrowException ||
			    _behavior == FileSystemOverflowBehavior.EvictOldest)
				throw new IOException("File system size limit exceeded.");

			return;
		}

		_writtenBytes += count;
		await _inner.WriteAsync(buffer, offset, count, cancellationToken);
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (!_disposed && disposing)
		{
			_disposed = true;

			// Commit actual size change (newSize - oldSize)
			if (!_limitExceeded)
				_commitSize(_writtenBytes, _oldFileSize);

			_inner.Dispose();
		}

		base.Dispose(disposing);
	}

	/// <inheritdoc />
	public override void Flush() => _inner.Flush();

	/// <inheritdoc />
	public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

	/// <inheritdoc />
	public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

	/// <inheritdoc />
	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		=> _inner.ReadAsync(buffer, offset, count, cancellationToken);

	/// <inheritdoc />
	public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

	/// <inheritdoc />
	public override void SetLength(long value) => _inner.SetLength(value);

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
}
