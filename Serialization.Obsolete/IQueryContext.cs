namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading.Tasks;

	public interface IQueryContext
	{
		IEnumerable<TResult> ExecuteEnum<TSource, TResult>(Expression expression);
		IAsyncEnumerable<TResult> ExecuteEnumAsync<TSource, TResult>(Expression expression);

		ValueTask ExecuteAsync<TSource>(Expression expression);
		ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression);
	}
}