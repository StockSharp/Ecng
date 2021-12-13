namespace Ecng.Net
{
	using System;

	using Ecng.Common;
	using Ecng.Collections;

	public interface IRestApiClientCache
	{
		bool TryGet<T>(Uri uri, out T value);
		void Set<T>(Uri uri, T value);
		bool Remove(Uri uri);
		void Clear();
	}

	public class InMemoryRestApiClientCache : IRestApiClientCache
	{
		private readonly SynchronizedDictionary<string, (object value, DateTime till)> _cache = new(StringComparer.InvariantCultureIgnoreCase);
		private readonly TimeSpan _timeout;

		public InMemoryRestApiClientCache(TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout));

			_timeout = timeout;
		}
		
		void IRestApiClientCache.Set<T>(Uri uri, T value)
		{
			if (uri is null)
				throw new ArgumentNullException(nameof(uri));

			if (value is null)
				return;

			_cache[uri.To<string>()] = new(value, DateTime.UtcNow + _timeout);
		}

		bool IRestApiClientCache.TryGet<T>(Uri uri, out T value)
		{
			if (uri is null)
				throw new ArgumentNullException(nameof(uri));

			value = default;

			var key = uri.To<string>();

			if (!_cache.TryGetValue(key, out var tuple))
				return false;

			if (tuple.till < DateTime.UtcNow)
			{
				_cache.Remove(key);
				return false;
			}

			value = (T)tuple.value;
			return true;
		}

		void IRestApiClientCache.Clear() => _cache.Clear();
		bool IRestApiClientCache.Remove(Uri uri) => _cache.Remove(uri.To<string>());
	}
}