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
		: this(timeout, TimeProvider.System)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRestApiClientCache"/> class with the specified timeout.
	/// </summary>
	/// <param name="timeout">How long a response outlives the last request for it.</param>
	/// <param name="time">The clock an item's age is measured on.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> is less than or equal to zero.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
	public InMemoryRestApiClientCache(TimeSpan timeout, TimeProvider time)
		: this(timeout, timeout * _staleFactor, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="InMemoryRestApiClientCache"/> class.
	/// </summary>
	/// <param name="timeout">How long a response outlives the last request for it.</param>
	/// <param name="maxAge">The oldest a response may be, however often it is requested.</param>
	/// <param name="time">The clock an item's age is measured on.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="timeout"/> or <paramref name="maxAge"/> is less than or equal to zero.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="time"/> is null.</exception>
	public InMemoryRestApiClientCache(TimeSpan timeout, TimeSpan maxAge, TimeProvider time)
	{
		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout));

		if (maxAge <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(maxAge));

		Timeout = timeout;
		MaxAge = maxAge;
		Cache = new(timeout, maxAge, time);
	}

	// How much older than the timeout a response may get while it is still being requested. A response
	// nobody has wanted for the timeout goes; one that is wanted stays, but not for ever, or the endpoint
	// asked for most often would be the one whose answer never changes.
	private const int _staleFactor = 5;

	/// <summary>
	/// Gets how long a response outlives the last request for it.
	/// </summary>
	public TimeSpan Timeout { get; }

	/// <summary>
	/// Gets the oldest a response may be, however often it is requested.
	/// </summary>
	public TimeSpan MaxAge { get; }

	/// <summary>
	/// The cache that stores the cached items.
	/// </summary>
	protected readonly TtlCache<(HttpMethod method, string uri, object body), object> Cache;

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

		return (method, builder.Uri.ToString(), null);
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

		Cache.Set(ToKey(method, uri, body), value);
	}

	bool IRestApiClientCache.TryGet<T>(HttpMethod method, Uri uri, object body, out T value)
	{
		value = default;

		if (!IsSupported(method) || !Cache.TryGet(ToKey(method, uri, body), out var held))
			return false;

		value = (T)held;
		return true;
	}

	void IRestApiClientCache.Remove(HttpMethod method, string uriLike, ComparisonOperator op)
	{
		if (method is null && uriLike.IsEmpty())
		{
			Cache.Clear();
			return;
		}

		Cache.RemoveWhere(key => (method is null || key.method == method) && (uriLike.IsEmpty() || key.uri.Like(uriLike, op)));
	}
}
