namespace Ecng.Common;

using System;
using System.IO;

/// <summary>
/// Represents a stream wrapper that records all data read from and written to the underlying stream.
/// </summary>
/// <param name="underlying">The underlying stream to wrap.</param>
public class DumpableStream(Stream underlying) : Stream
{
	private readonly Stream _underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));

	/// <summary>
	/// Retrieves and clears the dump of data read from the underlying stream.
	/// </summary>
	/// <returns>A byte array containing the dumped read data.</returns>
	public byte[] GetReadDump()
	{
		return GetDump(ReadDump);
	}

	/// <summary>
	/// Retrieves and clears the dump of data written to the underlying stream.
	/// </summary>
	/// <returns>A byte array containing the dumped write data.</returns>
	public byte[] GetWriteDump()
	{
		return GetDump(WriteDump);
	}

	private static byte[] GetDump(AllocationArray<byte> dump)
	{
		if (dump.Count == 0)
			return [];

		var buffer = new byte[dump.Count];
		Array.Copy(dump.Buffer, 0, buffer, 0, buffer.Length);
		dump.Count = 0;
		return buffer;
	}

	/// <summary>
	/// Gets the allocation array that collects data read from the underlying stream.
	/// </summary>
	public AllocationArray<byte> ReadDump { get; } = [];

	/// <summary>
	/// Gets the allocation array that collects data written to the underlying stream.
	/// </summary>
	public AllocationArray<byte> WriteDump { get; } = [];

	/// <summary>
	/// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
	/// </summary>
	/// <exception cref="IOException">An I/O error occurs.</exception>
	public override void Flush()
	{
		_underlying.Flush();
	}

	/// <summary>
	/// When overridden in a derived class, sets the position within the current stream.
	/// </summary>
	/// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
	/// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
	/// <returns>The new position within the current stream.</returns>
	/// <exception cref="IOException">An I/O error occurs.</exception>
	/// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
	public override long Seek(long offset, SeekOrigin origin)
	{
		return _underlying.Seek(offset, origin);
	}

	/// <summary>
	/// When overridden in a derived class, sets the length of the current stream.
	/// </summary>
	/// <param name="value">The desired length of the current stream in bytes.</param>
	/// <exception cref="IOException">An I/O error occurs.</exception>
	/// <exception cref="NotSupportedException">The stream does not support both writing and seeking.</exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
	public override void SetLength(long value)
	{
		_underlying.SetLength(value);
	}

	/// <summary>
	/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
	/// </summary>
	/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source.</param>
	/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.</param>
	/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
	/// <returns>The total number of bytes read into the buffer.</returns>
	/// <exception cref="ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative.</exception>
	/// <exception cref="IOException">An I/O error occurs.</exception>
	/// <exception cref="NotSupportedException">The stream does not support reading.</exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
	public override int Read(byte[] buffer, int offset, int count)
	{
		var read = _underlying.Read(buffer, offset, count);

		if (read > 0)
			ReadDump.Add(buffer, offset, read);

		return read;
	}

	/// <summary>
	/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
	/// </summary>
	/// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
	/// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
	/// <param name="count">The number of bytes to be written to the current stream.</param>
	public override void Write(byte[] buffer, int offset, int count)
	{
		_underlying.Write(buffer, offset, count);
		WriteDump.Add(buffer, offset, count);
	}

	/// <summary>
	/// Gets a value indicating whether the current stream supports reading.
	/// </summary>
	public override bool CanRead => _underlying.CanRead;

	/// <summary>
	/// Gets a value indicating whether the current stream supports seeking.
	/// </summary>
	public override bool CanSeek => _underlying.CanSeek;

	/// <summary>
	/// Gets a value indicating whether the current stream supports writing.
	/// </summary>
	public override bool CanWrite => _underlying.CanWrite;

	/// <summary>
	/// Gets the length in bytes of the stream.
	/// </summary>
	/// <exception cref="NotSupportedException">A class derived from Stream does not support seeking.</exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
	public override long Length => _underlying.Length;

	/// <summary>
	/// Gets or sets the position within the current stream.
	/// </summary>
	/// <exception cref="IOException">An I/O error occurs.</exception>
	/// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
	/// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
	public override long Position
	{
		get => _underlying.Position;
		set => _underlying.Position = value;
	}
}