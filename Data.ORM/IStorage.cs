namespace Ecng.Serialization;

using Ecng.ComponentModel;

public interface IStorage : IQueryContext
{
	IStorageTransaction CreateTransaction();

	void AddBulkLoad<TEntity>();

	Stat<string> Stat { get; }

	ValueTask AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken cancellationToken);

	ValueTask ClearCacheAsync(CancellationToken cancellationToken);

	ValueTask<long> GetCountAsync<TEntity>(CancellationToken cancellationToken);

	ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken);

	ValueTask<TEntity> GetByAsync<TEntity>(IQueryable<TEntity> expression, CancellationToken cancellationToken);

	ValueTask<TEntity> GetByIdAsync<TId, TEntity>(TId id, CancellationToken cancellationToken);

	ValueTask<TEntity[]> GetGroupAsync<TEntity>(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken);

	ValueTask<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken);

	ValueTask<bool> RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken);

	ValueTask ClearAsync<TEntity>(CancellationToken cancellationToken);
}
