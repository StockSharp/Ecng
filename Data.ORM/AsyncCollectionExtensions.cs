namespace Ecng.Serialization;

using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for asynchronous collections.
/// </summary>
public static class AsyncCollectionExtensions
{
	/// <summary>
	/// Filters an async sequence using an asynchronous predicate with cancellation support.
	/// </summary>
	public static async IAsyncEnumerable<T> WhereAwait<T>(
		this IAsyncEnumerable<T> source,
		Func<T, CancellationToken, ValueTask<bool>> predicate,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));
		if (predicate is null) throw new ArgumentNullException(nameof(predicate));

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken).ConfigureAwait(false))
		{
			if (await predicate(item, cancellationToken).NoWait())
				yield return item;
		}
	}
}