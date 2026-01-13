#if !NET7_0_OR_GREATER
namespace System.IO;

using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="TextReader"/>.
/// </summary>
public static class TextReaderExtensions
{
	/// <summary>
	/// Reads a line of characters asynchronously from the current stream and returns the data as a string.
	/// </summary>
	/// <param name="reader">The text reader.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the next line from the text reader, or is null if all of the characters have been read.</returns>
	public static Task<string> ReadLineAsync(this TextReader reader, CancellationToken cancellationToken)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		cancellationToken.ThrowIfCancellationRequested();

		return reader.ReadLineAsync();
	}

	/// <summary>
	/// Reads all characters from the current position to the end of the text reader asynchronously and returns them as one string.
	/// </summary>
	/// <param name="reader">The text reader.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains a string with the characters from the current position to the end of the text reader.</returns>
	public static Task<string> ReadToEndAsync(this TextReader reader, CancellationToken cancellationToken)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		cancellationToken.ThrowIfCancellationRequested();

		return reader.ReadToEndAsync();
	}

#if NETSTANDARD2_0 || NET6_0
	/// <summary>
	/// Reads characters from the current stream asynchronously and writes the data to a buffer.
	/// </summary>
	/// <param name="reader">The text reader.</param>
	/// <param name="buffer">The buffer to write the data into.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value contains the number of characters read.</returns>
	public static async ValueTask<int> ReadAsync(this TextReader reader, Memory<char> buffer, CancellationToken cancellationToken = default)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		cancellationToken.ThrowIfCancellationRequested();

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<char> segment))
		{
			return await reader.ReadAsync(segment.Array, segment.Offset, segment.Count).NoWait();
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(buffer.Length);
			try
			{
				var charsRead = await reader.ReadAsync(tempBuffer, 0, buffer.Length).NoWait();
				tempBuffer.AsSpan(0, charsRead).CopyTo(buffer.Span);
				return charsRead;
			}
			finally
			{
				System.Buffers.ArrayPool<char>.Shared.Return(tempBuffer);
			}
		}
	}

	/// <summary>
	/// Reads characters from the current stream asynchronously and writes the data to a buffer until the buffer is filled.
	/// </summary>
	/// <param name="reader">The text reader.</param>
	/// <param name="buffer">The buffer to write the data into.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous read operation. The value contains the number of characters read.</returns>
	public static async ValueTask<int> ReadBlockAsync(this TextReader reader, Memory<char> buffer, CancellationToken cancellationToken = default)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		cancellationToken.ThrowIfCancellationRequested();

		if (System.Runtime.InteropServices.MemoryMarshal.TryGetArray(buffer, out ArraySegment<char> segment))
		{
			return await reader.ReadBlockAsync(segment.Array, segment.Offset, segment.Count).NoWait();
		}
		else
		{
			var tempBuffer = System.Buffers.ArrayPool<char>.Shared.Rent(buffer.Length);
			try
			{
				var charsRead = await reader.ReadBlockAsync(tempBuffer, 0, buffer.Length).NoWait();
				tempBuffer.AsSpan(0, charsRead).CopyTo(buffer.Span);
				return charsRead;
			}
			finally
			{
				System.Buffers.ArrayPool<char>.Shared.Return(tempBuffer);
			}
		}
	}
#endif
}
#endif