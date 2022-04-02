namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;

	public interface IStorage : IQueryContext
	{
		Task<IBatchContext> BeginBatch(CancellationToken cancellationToken);
		Task CommitBatch(CancellationToken cancellationToken);
		Task EndBatch(CancellationToken cancellationToken);

		Task ClearCacheAsync(CancellationToken cancellationToken);
	}

	public interface IStorage<TId> : IStorage
	{
		Task<long> GetCountAsync<TEntity>(CommandType? commandType, CancellationToken cancellationToken);

		Task<TEntity> AddAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		Task<TEntity> GetByAsync<TEntity>(CommandType? commandType, SerializationItemCollection by, CancellationToken cancellationToken);

		Task<TEntity> GetByIdAsync<TEntity>(CommandType? commandType, TId id, CancellationToken cancellationToken);

		Task<IEnumerable<TEntity>> GetGroupAsync<TEntity>(CommandType? commandType, long startIndex, long count, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken);

		Task<TEntity> UpdateAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		Task RemoveAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		Task ClearAsync<TEntity>(CommandType? commandType, CancellationToken cancellationToken);

		//event Action<object> Added;
		//event Action<object> Updated;
		//event Action<object> Removed;
	}
}