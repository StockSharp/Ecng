namespace Ecng.Collections
{
	public class CachedSynchronizedSet<T> : SynchronizedSet<T>
	{
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