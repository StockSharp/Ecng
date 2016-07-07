namespace Ecng.Collections
{
	using System.Collections.Generic;

	public class CachedSynchronizedSet<T> : SynchronizedSet<T>
	{
		public CachedSynchronizedSet()
		{
		}

		public CachedSynchronizedSet(bool allowIndexing)
			: base(allowIndexing)
		{
		}

		public CachedSynchronizedSet(IEqualityComparer<T> comparer)
			: base(comparer)
		{
		}

		public CachedSynchronizedSet(bool allowIndexing, IEqualityComparer<T> comparer)
			: base(allowIndexing, comparer)
		{
		}

		private T[] _cache;

		public T[] Cache
		{
			get
			{
				lock (SyncRoot)
					return _cache ?? (_cache = ToArray());
			}
		}

		protected override void OnChanged()
		{
			_cache = null;

			base.OnChanged();
		}
	}
}