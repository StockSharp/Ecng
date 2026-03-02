namespace Ecng.Serialization;

public interface IQueryContext
{
	IEnumerable<TResult> ExecuteEnum<TSource, TResult>(Expression expression);
	IAsyncEnumerable<TResult> ExecuteEnumAsync<TSource, TResult>(Expression expression);

	ValueTask ExecuteAsync<TSource>(Expression expression);
	
	TResult ExecuteResult<TSource, TResult>(Expression expression);
	ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression);
}