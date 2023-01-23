namespace Ecng.Net;

public interface IRestApiClientCache
{
	bool TryGet<T>(HttpMethod method, Uri uri, out T value);
	void Set<T>(HttpMethod method, Uri uri, T value);
	void Remove(HttpMethod method = default, string uriLike = default, ComparisonOperator op = ComparisonOperator.Greater);
}