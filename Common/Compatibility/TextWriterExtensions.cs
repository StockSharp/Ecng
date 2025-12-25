namespace System.IO;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="TextWriter"/>.
/// </summary>
public static class TextWriterExtensions
{
#if NET7_0_OR_GREATER
	/// <summary>
	/// Writes a string to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The string to write. If value is null, nothing is written to the text stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this TextWriter writer, string value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		return writer.WriteAsync(value.AsMemory(), cancellationToken);
	}

	/// <summary>
	/// Writes a string followed by a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The string to write. If value is null, only a line terminator is written.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, string value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		return writer.WriteLineAsync(value.AsMemory(), cancellationToken);
	}
#else
	/// <summary>
	/// Writes a character to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The character to write.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this TextWriter writer, char value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteAsync(value);
	}

	/// <summary>
	/// Writes a string to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The string to write. If value is null, nothing is written to the text stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this TextWriter writer, string value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteAsync(value);
	}

	/// <summary>
	/// Writes a character array to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="buffer">The character array to write to the text stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this TextWriter writer, char[] buffer, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteAsync(buffer);
	}

	/// <summary>
	/// Writes a subarray of characters to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="buffer">The character array to write data from.</param>
	/// <param name="index">The character position in the buffer at which to start retrieving data.</param>
	/// <param name="count">The number of characters to write.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteAsync(this TextWriter writer, char[] buffer, int index, int count, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteAsync(buffer, index, count);
	}

	/// <summary>
	/// Writes a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteLineAsync();
	}

	/// <summary>
	/// Writes a character followed by a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The character to write.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, char value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteLineAsync(value);
	}

	/// <summary>
	/// Writes a string followed by a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="value">The string to write. If value is null, only a line terminator is written.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, string value, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteLineAsync(value);
	}

	/// <summary>
	/// Writes an array of characters followed by a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="buffer">The character array to write to the text stream.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, char[] buffer, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteLineAsync(buffer);
	}

	/// <summary>
	/// Writes a subarray of characters followed by a line terminator to the text stream asynchronously.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="buffer">The character array to write data from.</param>
	/// <param name="index">The character position in the buffer at which to start retrieving data.</param>
	/// <param name="count">The number of characters to write.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous write operation.</returns>
	public static Task WriteLineAsync(this TextWriter writer, char[] buffer, int index, int count, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.WriteLineAsync(buffer, index, count);
	}
#endif

#if NET8_0_OR_GREATER == false
	/// <summary>
	/// Asynchronously clears all buffers for the current writer and causes any buffered data to be written to the underlying device.
	/// </summary>
	/// <param name="writer">The text writer.</param>
	/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous flush operation.</returns>
	public static Task FlushAsync(this TextWriter writer, CancellationToken cancellationToken)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		cancellationToken.ThrowIfCancellationRequested();

		return writer.FlushAsync();
	}
#endif
}
