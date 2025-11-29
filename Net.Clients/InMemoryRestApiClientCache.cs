namespace Ecng.Net;

using Ecng.ComponentModel;

/// <summary>
/// Represents an in-memory cache for REST API client responses.
/// </summary>
public class InMemoryRestApiClientCache : IRestApiClientCache
{
	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRestApiClientCache"/> class with the specified timeout.
	/// </summary>
	/// <param name="timeout">The duration after which a cached item expires.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero.</exception>
	public InMemoryRestApiClientCache(TimeSpan timeout)
	{
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout));

		Timeout = timeout;
	}

	/// <summary>
	/// Gets the timeout duration for cached items.
	/// </summary>
	public TimeSpan Timeout { get; }

	/// <summary>
	/// The cache that stores the cached items.
	/// </summary>
	protected readonly SynchronizedDictionary<(HttpMethod method, string uri, object body), (object value, DateTime till)> Cache = [];

	/// <summary>
	/// Converts the provided HTTP method, URI, and body into a cache key.
	/// </summary>
	/// <param name="method">The HTTP method of the request.</param>
	/// <param name="uri">The URI of the request.</param>
	/// <param name="body">The request body.</param>
	/// <returns>A tuple that represents the cache key.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="method"/> or <paramref name="uri"/> is null.</exception>
	protected virtual (HttpMethod, string, object) ToKey(HttpMethod method, Uri uri, object body)
	{
		if (method is null)	throw new ArgumentNullException(nameof(method));
		if (uri is null)	throw new ArgumentNullException(nameof(uri));

		var builder = new UriBuilder(uri);
		
		if (!builder.Query.IsEmpty())
		{
			var sortedQuery = builder.Query.Substring(1)
				.ParseUrl()
				.ExcludeEmpty()
				.OrderBy(p => p.key, StringComparer.InvariantCultureIgnoreCase)
				.ToQueryString(true);

			builder.Query = sortedQuery;
		}

		return (method, builder.Uri.ToString().ToLowerInvariant(), null);
	}

	/// <summary>
	/// Determines whether the specified HTTP method is supported for caching.
	/// </summary>
	/// <param name="method">The HTTP method of the request.</param>
	/// <returns>True if the method is supported; otherwise, false.</returns>
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

		using (Cache.EnterScope())
		{
			var keys = Cache.Keys.Where(p => (method is null || p.method == method) && (uriLike.IsEmpty() || p.uri.Like(uriLike, op))).ToArray();

			foreach (var key in keys)
				Cache.Remove(key);
		}
	}
}
