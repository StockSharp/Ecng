namespace MoreLinq
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public static class MoreEnumerable
	{
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> items, T item)
		{
			return items.Concat(new[] { item });
		}

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			if (action == null)
				throw new ArgumentNullException("action");

			foreach (var t in source)
				action(t);
		}

		public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int batch)
		{
			return new[] { items };
		}
	}
}