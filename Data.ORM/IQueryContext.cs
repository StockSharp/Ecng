namespace Ecng.Serialization;

/// <summary>
/// Provides query execution context for LINQ expression evaluation.
/// </summary>
public interface IQueryContext
{
	/// <summary>
	/// Executes a LINQ expression and returns an enumerable result.
	/// </summary>
	IEnumerable<TResult> ExecuteEnum<TSource, TResult>(Expression expression);

	/// <summary>
	/// Executes a LINQ expression and returns an async enumerable result.
	/// </summary>
	IAsyncEnumerable<TResult> ExecuteEnumAsync<TSource, TResult>(Expression expression);

	/// <summary>
	/// Asynchronously executes a LINQ expression with no return value.
	/// </summary>
	ValueTask ExecuteAsync<TSource>(Expression expression);

	/// <summary>
	/// Executes a LINQ expression and returns a scalar result.
	/// </summary>
	TResult ExecuteResult<TSource, TResult>(Expression expression);

	/// <summary>
	/// Asynchronously executes a LINQ expression and returns a scalar result.
	/// </summary>
	ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression);
}