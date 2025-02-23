namespace Ecng.Net;

/// <summary>
/// Provides caching functionality for REST API client responses.
/// </summary>
public interface IRestApiClientCache
{
	/// <summary>
	/// Attempts to retrieve a cached value for the specified HTTP method, URI, and request body.
	/// </summary>
	/// <typeparam name="T">The type of the cached value.</typeparam>
	/// <param name="method">The HTTP method of the request.</param>
	/// <param name="uri">The URI of the request.</param>
	/// <param name="body">The request body.</param>
	/// <param name="value">When this method returns, contains the cached value if found; otherwise, the default value for the type.</param>
	/// <returns>true if a cached value was found; otherwise, false.</returns>
	bool TryGet<T>(HttpMethod method, Uri uri, object body, out T value);

	/// <summary>
	/// Caches the specified value for the given HTTP method, URI, and request body.
	/// </summary>
	/// <typeparam name="T">The type of the value to cache.</typeparam>
	/// <param name="method">The HTTP method of the request.</param>
	/// <param name="uri">The URI of the request.</param>
	/// <param name="body">The request body.</param>
	/// <param name="value">The value to be cached.</param>
	void Set<T>(HttpMethod method, Uri uri, object body, T value);

	/// <summary>
	/// Removes cached entries based on the specified HTTP method and a URI pattern.
	/// </summary>
	/// <param name="method">The HTTP method to match for removal. Defaults to the default value of HttpMethod if not specified.</param>
	/// <param name="uriLike">
	/// A string pattern to match part of the URI for removal.
	/// If null or empty, no URI filter is applied.
	/// </param>
	/// <param name="op">
	/// The comparison operator to apply when filtering URIs.
	/// Defaults to <see cref="ComparisonOperator.Greater"/> if not specified.
	/// </param>
	void Remove(HttpMethod method = default, string uriLike = default, ComparisonOperator op = ComparisonOperator.Greater);
}