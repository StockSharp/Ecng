namespace Ecng.Net;

using Ecng.ComponentModel;

public class InMemoryRestApiClientCache : IRestApiClientCache
{
	public InMemoryRestApiClientCache(TimeSpan timeout)
	{
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout));

		Timeout = timeout;
	}

	public TimeSpan Timeout { get; }

	protected readonly SynchronizedDictionary<(HttpMethod method, string uri), (object value, DateTime till)> Cache = new();

	private static (HttpMethod, string) ToKey(HttpMethod method, Uri uri)
		=> (method.CheckOnNull(nameof(method)), uri.CheckOnNull(nameof(uri)).To<string>().ToLowerInvariant());

	protected virtual bool IsSupported(HttpMethod method) => method == HttpMethod.Get;

	void IRestApiClientCache.Set<T>(HttpMethod method, Uri uri, T value)
	{
		if (value is null || !IsSupported(method))
			return;

		Cache[ToKey(method, uri)] = new(value, DateTime.UtcNow + Timeout);
	}

	bool IRestApiClientCache.TryGet<T>(HttpMethod method, Uri uri, out T value)
	{
		value = default;

		var key = ToKey(method, uri);

		if (!IsSupported(method) || !Cache.TryGetValue(key, out var tuple))
			return false;

		if (tuple.till < DateTime.UtcNow)
		{
			Cache.Remove(key);
			return false;
		}

		value = (T)tuple.value;
		return true;
	}

	void IRestApiClientCache.Remove(HttpMethod method, string uriLike, ComparisonOperator op)
	{
		if (method is null && uriLike.IsEmpty())
		{
			Cache.Clear();
			return;
		}

		lock (Cache.SyncRoot)
		{
			var keys = Cache.Keys.Where(p => (method is null || p.method == method) && (uriLike.IsEmpty() || p.uri.Like(uriLike, op))).ToArray();

			foreach (var key in keys)
				Cache.Remove(key);
		}
	}
}