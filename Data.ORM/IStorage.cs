namespace Ecng.Serialization;

using Ecng.ComponentModel;

/// <summary>
/// Provides database storage operations for entities.
/// </summary>
public interface IStorage : IQueryContext
{
	/// <summary>
	/// Creates a new storage transaction.
	/// </summary>
	IStorageTransaction CreateTransaction();

	/// <summary>
	/// Registers an entity type for bulk loading optimization.
	/// </summary>
	void AddBulkLoad<TEntity>() where TEntity : IDbPersistable;

	/// <summary>
	/// Gets the storage statistics.
	/// </summary>
	Stat<string> Stat { get; }

	/// <summary>
	/// Asynchronously adds an entity to the cache.
	/// </summary>
	ValueTask AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously clears all cached entities.
	/// </summary>
	ValueTask ClearCacheAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Asynchronously returns the total count of entities of the specified type.
	/// </summary>
	ValueTask<long> GetCountAsync<TEntity>(CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously adds a new entity to the storage.
	/// </summary>
	ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously retrieves an entity matching the specified query expression.
	/// </summary>
	ValueTask<TEntity> GetByAsync<TEntity>(IQueryable<TEntity> expression, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously retrieves an entity by its primary key identifier.
	/// </summary>
	ValueTask<TEntity> GetByIdAsync<TId, TEntity>(TId id, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously retrieves multiple entities by their primary key identifiers using a single IN query.
	/// Results are returned in the same order as input <paramref name="ids"/>.
	/// Duplicate input ids produce duplicate entries in the result.
	/// IDs not found in the database are silently skipped.
	/// </summary>
	ValueTask<TEntity[]> GetByIdsAsync<TId, TEntity>(IEnumerable<TId> ids, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously retrieves a paged group of entities with sorting.
	/// </summary>
	ValueTask<TEntity[]> GetGroupAsync<TEntity>(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously updates an existing entity in the storage.
	/// </summary>
	ValueTask<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously removes an entity from the storage.
	/// </summary>
	ValueTask<bool> RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : IDbPersistable;

	/// <summary>
	/// Asynchronously removes all entities of the specified type from the storage.
	/// </summary>
	ValueTask ClearAsync<TEntity>(CancellationToken cancellationToken) where TEntity : IDbPersistable;
}
