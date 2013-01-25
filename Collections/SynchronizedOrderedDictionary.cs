namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public class SynchronizedOrderedDictionary<TKey, TValue> : SynchronizedDictionary<TKey, TValue>
	{
		public SynchronizedOrderedDictionary()
			: base(new SortedDictionary<TKey, TValue>())
		{
		}

		public SynchronizedOrderedDictionary(IComparer<TKey> comparer)
			: base(new SortedDictionary<TKey, TValue>(comparer))
		{
		}

		public SynchronizedOrderedDictionary(Func<TKey, TKey, int> comparer)
			: this(comparer.ToComparer())
		{
		}
	}
}