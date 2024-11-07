namespace Ecng.Collections
{
	using System.Linq;

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