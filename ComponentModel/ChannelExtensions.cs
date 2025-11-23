#if NET10_0_OR_GREATER == false
namespace System.Threading.Channels;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Ecng.Common;

/// <summary>
/// Extension methods for <see cref="ChannelReader{T}"/>.
/// </summary>
public static class ChannelReaderExtensions
{
	/// <summary>
	/// Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data from the channel.
	/// </summary>
	/// <typeparam name="T">The type of the elements in the channel.</typeparam>
	/// <param name="reader">The channel reader.</param>
	/// <param name="cancellationToken">The cancellation token to use to cancel the enumeration.</param>
	/// <returns>An <see cref="IAsyncEnumerable{T}"/> that provides all the data from the channel.</returns>
	public static async IAsyncEnumerable<T> ReadAllAsync<T>(
		this ChannelReader<T> reader,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (reader is null)
			throw new ArgumentNullException(nameof(reader));

		while (await reader.WaitToReadAsync(cancellationToken).NoWait())
		{
			while (reader.TryRead(out var item))
			{
				yield return item;
			}
		}
	}
}

#endif