#if !NET7_0_OR_GREATER
namespace System.IO;

using System.Threading;
using System.Threading.Tasks;

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
}
#endif