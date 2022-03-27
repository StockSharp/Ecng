namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Threading;
	using System.Threading.Tasks;

	public interface IStorage
	{
		Task<IBatchContext> BeginBatch(CancellationToken cancellationToken);
		Task CommitBatch(CancellationToken cancellationToken);
		Task EndBatch(CancellationToken cancellationToken);

		Task ClearCache(CancellationToken cancellationToken);
	}

	public interface IStorage<TId> : IStorage
	{
		Task<long> GetCount<TEntity>(CancellationToken cancellationToken);

		Task<TEntity> Add<TEntity>(TEntity entity, CancellationToken cancellationToken);

		Task<TEntity> GetBy<TEntity>(SerializationItemCollection by, CancellationToken cancellationToken);

		Task<TEntity> GetById<TEntity>(TId id, CancellationToken cancellationToken);

		Task<IEnumerable<TEntity>> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken);

		Task<TEntity> Update<TEntity>(TEntity entity, CancellationToken cancellationToken);

		Task Remove<TEntity>(TEntity entity, CancellationToken cancellationToken);

		Task Clear<TEntity>(CancellationToken cancellationToken);

		event Action<object> Added;
		event Action<object> Updated;
		event Action<object> Removed;
	}
}