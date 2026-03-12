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
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);

		await foreach (var item in source.WithEnforcedCancellation(cancellationToken).NoWait())
		{
			if (await predicate(item, cancellationToken).NoWait())
				yield return item;
		}
	}
}