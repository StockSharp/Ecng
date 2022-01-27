namespace Ecng.Net
{
	using System;
	using System.Net.Http;

	public interface IRestApiClientCache
	{
		bool TryGet<T>(HttpMethod method, Uri uri, out T value);
		void Set<T>(HttpMethod method, Uri uri, T value);
		bool Remove(HttpMethod method, Uri uri);
		void RemoveLike(HttpMethod method, string startWith);
		void Clear();
	}
}