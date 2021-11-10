namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	public interface IStorage
	{
		IBatchContext BeginBatch();
		void CommitBatch();
		void EndBatch();

		void ClearCache();
	}

	public interface IStorage<TId>
	{
		long GetCount<TEntity>();

		TEntity Add<TEntity>(TEntity entity);

		TEntity GetBy<TEntity>(SerializationItemCollection by);

		TEntity GetById<TEntity>(TId id);

		IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction);

		TEntity Update<TEntity>(TEntity entity);

		void Remove<TEntity>(TEntity entity);

		void Clear<TEntity>();

		event Action<object> Added;
		event Action<object> Updated;
		event Action<object> Removed;
	}
}