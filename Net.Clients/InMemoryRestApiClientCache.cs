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

	protected readonly SynchronizedDictionary<(HttpMethod method, string uri, object body), (object value, DateTime till)> Cache = new();

	protected virtual (HttpMethod, string, object) ToKey(HttpMethod method, Uri uri, object body)
	{
		if (method is null)	throw new ArgumentNullException(nameof(method));
		if (uri is null)	throw new ArgumentNullException(nameof(uri));

		return (method, uri.To<string>().ToLowerInvariant(), null);
	}

	protected virtual bool IsSupported(HttpMethod method) => method == HttpMethod.Get;

	void IRestApiClientCache.Set<T>(HttpMethod method, Uri uri, object body, T value)
	{
		if (value is null || !IsSupported(method))
			return;

		Cache[ToKey(method, uri, body)] = new(value, DateTime.UtcNow + Timeout);
	}

	bool IRestApiClientCache.TryGet<T>(HttpMethod method, Uri uri, object body, out T value)
	{
		value = default;

		var key = ToKey(method, uri, body);

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