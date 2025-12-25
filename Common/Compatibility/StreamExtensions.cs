#if NETSTANDARD2_0
namespace System.IO;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="Stream"/>.
/// </summary>
public static class StreamExtensions
{
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
}
#endif
