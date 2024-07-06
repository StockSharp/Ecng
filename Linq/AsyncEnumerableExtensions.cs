namespace Ecng.Linq;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

public static class AsyncEnumerableExtensions
{
	public static async ValueTask<T[]> ToArrayAsync2<T>(this IAsyncEnumerable<T> enu, CancellationToken cancellationToken)
	{
		if (enu is null)
			throw new ArgumentNullException(nameof(enu));

		var list = new List<T>();

		await foreach (var item in enu.WithEnforcedCancellation(cancellationToken))
			list.Add(item);

		return list.ToArray();
	}
}