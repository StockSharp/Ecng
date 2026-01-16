#if !NET7_0_OR_GREATER
namespace System.IO;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="Stream"/>.
/// </summary>
public static class StreamExtensions
{
#if NETSTANDARD2_0
	/// <summary>
	/// Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified cancellation token.
	/// </summary>
	/// <param name="source">The source stream.</param>
	/// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous copy operation.</returns>
	public static Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		return source.CopyToAsync(destination, 81920, cancellationToken);
	}

	/// <summary>
	/// Asynchronously writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="buffer">The buffer to write data from.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		return stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
	}

	/// <summary>
	/// Asynchronously reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="buffer">The buffer to write the data into.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value contains the total number of bytes read into the buffer.</returns>
	public static Task<int> ReadAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		return stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
	}
#endif

#if NETSTANDARD2_0 || NET6_0
	/// <summary>
	/// Reads count number of bytes from the current stream and advances the position within the stream.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
	/// <param name="offset">The byte offset in buffer at which to begin storing the data read from the current stream.</param>
	/// <param name="count">The number of bytes to be read from the current stream.</param>
	/// <exception cref="EndOfStreamException">The end of the stream is reached before reading count bytes.</exception>
	public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		var totalRead = 0;
		while (totalRead < count)
		{
			var read = stream.Read(buffer, offset + totalRead, count - totalRead);
			if (read == 0)
				throw new EndOfStreamException();
			totalRead += read;
		}
	}

	/// <summary>
	/// Asynchronously reads count number of bytes from the current stream and advances the position within the stream.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
	/// <param name="offset">The byte offset in buffer at which to begin storing the data read from the current stream.</param>
	/// <param name="count">The number of bytes to be read from the current stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <exception cref="EndOfStreamException">The end of the stream is reached before reading count bytes.</exception>
	public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		var totalRead = 0;
		while (totalRead < count)
		{
			var read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, cancellationToken).NoWait();
			if (read == 0)
				throw new EndOfStreamException();
			totalRead += read;
		}
	}
#endif

#if NETSTANDARD2_0
	/// <summary>
	/// Asynchronously writes a sequence of bytes to the current stream.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="buffer">The region of memory to write data from.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			return new ValueTask(stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
			buffer.Span.CopyTo(tempBuffer);
			return WriteAsyncWithRent(stream, tempBuffer, buffer.Length, cancellationToken);
		}
	}

	private static async ValueTask WriteAsyncWithRent(Stream stream, byte[] buffer, int length, CancellationToken cancellationToken)
	{
		try
		{
			await stream.WriteAsync(buffer, 0, length, cancellationToken).NoWait();
		}
		finally
		{
			System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	/// <summary>
	/// Asynchronously reads a sequence of bytes from the current stream.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="buffer">The region of memory to write the data into.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value contains the total number of bytes read into the buffer.</returns>
	public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			return new ValueTask<int>(stream.ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
		}
		else
		{
			return ReadAsyncWithRent(stream, buffer, cancellationToken);
		}
	}

	private static async ValueTask<int> ReadAsyncWithRent(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
	{
		var tempBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			var read = await stream.ReadAsync(tempBuffer, 0, buffer.Length, cancellationToken).NoWait();
			tempBuffer.AsSpan(0, read).CopyTo(buffer.Span);
			return read;
		}
		finally
		{
			System.Buffers.ArrayPool<byte>.Shared.Return(tempBuffer);
		}
	}

	/// <summary>
	/// Asynchronously reads bytes from the current stream and advances the position within the stream until the buffer is filled.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <exception cref="EndOfStreamException">The end of the stream is reached before filling the buffer.</exception>
	public static async ValueTask ReadExactlyAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
		{
			await stream.ReadExactlyAsync(segment.Array, segment.Offset, segment.Count, cancellationToken).NoWait();
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
			try
			{
				await stream.ReadExactlyAsync(tempBuffer, 0, buffer.Length, cancellationToken).NoWait();
				tempBuffer.AsSpan(0, buffer.Length).CopyTo(buffer.Span);
			}
			finally
			{
				System.Buffers.ArrayPool<byte>.Shared.Return(tempBuffer);
			}
		}
	}
#endif

#if NET6_0
	/// <summary>
	/// Reads bytes from the current stream and advances the position within the stream until the buffer is filled.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current stream.</param>
	/// <exception cref="EndOfStreamException">The end of the stream is reached before filling the buffer.</exception>
	public static void ReadExactly(this Stream stream, Span<byte> buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		var totalRead = 0;
		while (totalRead < buffer.Length)
		{
			var read = stream.Read(buffer.Slice(totalRead));
			if (read == 0)
				throw new EndOfStreamException();
			totalRead += read;
		}
	}

	/// <summary>
	/// Asynchronously reads bytes from the current stream and advances the position within the stream until the buffer is filled.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <exception cref="EndOfStreamException">The end of the stream is reached before filling the buffer.</exception>
	public static async ValueTask ReadExactlyAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		var totalRead = 0;
		while (totalRead < buffer.Length)
		{
			var read = await stream.ReadAsync(buffer.Slice(totalRead), cancellationToken).NoWait();
			if (read == 0)
				throw new EndOfStreamException();
			totalRead += read;
		}
	}
#endif
}
#endif