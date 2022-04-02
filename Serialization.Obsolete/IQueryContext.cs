namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Threading.Tasks;

	public interface IQueryContext
	{
		IEnumerable<TEntity> ExecuteEnum<TEntity>(Expression expression);
		IAsyncEnumerable<TEntity> ExecuteEnumAsync<TEntity>(Expression expression);

		Task ExecuteAsync<TEntity>(Expression expression);
		Task<TResult> ExecuteResultAsync<TEntity, TResult>(Expression expression);
	}
}