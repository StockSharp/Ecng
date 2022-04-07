namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading.Tasks;

	public interface IQueryContext
	{
		IEnumerable<TEntity> ExecuteEnum<TEntity>(Expression expression);
		IAsyncEnumerable<TEntity> ExecuteEnumAsync<TEntity>(Expression expression);

		ValueTask ExecuteAsync<TEntity>(Expression expression);
		ValueTask<TResult> ExecuteResultAsync<TEntity, TResult>(Expression expression);
	}
}