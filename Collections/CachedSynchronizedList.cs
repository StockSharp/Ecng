namespace Ecng.Collections
{
	public class CachedSynchronizedList<T> : SynchronizedList<T>
	{
		public CachedSynchronizedList()
		{
		}

		public CachedSynchronizedList(int capacity)
			: base(capacity)
		{
		}

		private T[] _cache;

		public T[] Cache
		{
			get
			{
				lock (SyncRoot)
					return _cache ??= ToArray();
			}
		}

		protected override void OnChanged()
		{
			_cache = null;

			base.OnChanged();
		}
	}
}