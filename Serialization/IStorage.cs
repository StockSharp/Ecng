namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	public interface IStorage
	{
		long GetCount<TEntity>();

		TEntity Add<TEntity>(TEntity entity);

		TEntity GetBy<TEntity>(SerializationItemCollection by);

		TEntity GetById<TEntity>(object id);

		IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction);

		TEntity Update<TEntity>(TEntity entity);

		void Remove<TEntity>(TEntity entity);

		void Clear<TEntity>();

		void ClearCache();

		IBatchContext BeginBatch();
		void CommitBatch();
		void EndBatch();

		event Action<object> Added;
		event Action<object> Updated;
		event Action<object> Removed;
	}
}