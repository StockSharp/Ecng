namespace Ecng.Serialization;

using System.Runtime.CompilerServices;

public static class AsyncCollectionExtensions
{
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