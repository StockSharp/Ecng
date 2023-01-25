namespace Ecng.Net;

public interface IRestApiClientCache
{
	bool TryGet<T>(HttpMethod method, Uri uri, object body, out T value);
	void Set<T>(HttpMethod method, Uri uri, object body, T value);
	void Remove(HttpMethod method = default, string uriLike = default, ComparisonOperator op = ComparisonOperator.Greater);
}