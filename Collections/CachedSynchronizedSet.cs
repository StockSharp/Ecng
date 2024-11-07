namespace Ecng.Collections
{
	using System.Collections.Generic;
	using System.Linq;

	public class CachedSynchronizedSet<T> : SynchronizedSet<T>
	{
		public CachedSynchronizedSet()
		{
		}

		public CachedSynchronizedSet(bool allowIndexing)
			: base(allowIndexing)
		{
		}

		public CachedSynchronizedSet(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public CachedSynchronizedSet(IEqualityComparer<T> comparer)
			: base(comparer)
		{
		}

		public CachedSynchronizedSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
			: base(collection, comparer)
		{
		}

		public CachedSynchronizedSet(bool allowIndexing, IEqualityComparer<T> comparer)
			: base(allowIndexing, comparer)
		{
		}

		public CachedSynchronizedSet(bool allowIndexing, IEnumerable<T> collection, IEqualityComparer<T> comparer)
			: base(allowIndexing, collection, comparer)
		{
		}

		private T[] _cache;

		public T[] Cache
		{
			get
			{
				lock (SyncRoot)
					return _cache ??= [.. this];
			}
		}

		protected override void OnChanged()
		{
			_cache = null;

			base.OnChanged();
		}
	}
}