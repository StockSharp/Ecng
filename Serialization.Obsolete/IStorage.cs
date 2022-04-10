namespace Ecng.Serialization
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;

	public interface IStorage : IQueryContext
	{
		ValueTask<IBatchContext> BeginBatch(CancellationToken cancellationToken);
		ValueTask CommitBatch(CancellationToken cancellationToken);
		ValueTask EndBatch(CancellationToken cancellationToken);

		ValueTask ClearCacheAsync(CancellationToken cancellationToken);
	}

	public interface IStorage<TId> : IStorage
	{
		ValueTask<long> GetCountAsync<TEntity>(CommandType? commandType, CancellationToken cancellationToken);

		ValueTask<TEntity> AddAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		ValueTask<TEntity> GetByAsync<TEntity>(CommandType? commandType, SerializationItemCollection by, CancellationToken cancellationToken);

		ValueTask<TEntity> GetByIdAsync<TEntity>(CommandType? commandType, TId id, CancellationToken cancellationToken);

		ValueTask<IEnumerable<TEntity>> GetGroupAsync<TEntity>(CommandType? commandType, long startIndex, long count, bool deleted, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken);

		ValueTask<TEntity> UpdateAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		ValueTask RemoveAsync<TEntity>(CommandType? commandType, TEntity entity, CancellationToken cancellationToken);

		ValueTask ClearAsync<TEntity>(CommandType? commandType, CancellationToken cancellationToken);

		//event Action<object> Added;
		//event Action<object> Updated;
		//event Action<object> Removed;
	}
}