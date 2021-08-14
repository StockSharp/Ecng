namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	public class InMemoryStorage : IStorage
	{
		private readonly SynchronizedMultiDictionary<Type, object> _cache = new();

		public long GetCount<TEntity>()
		{
			return _cache[typeof(TEntity)].Count;
		}

		public TEntity Add<TEntity>(TEntity entity)
		{
			_cache.Add(typeof(TEntity), entity);
			Added?.Invoke(entity);
			return entity;
		}

		public TEntity GetBy<TEntity>(SerializationItemCollection by)
		{
			throw new NotImplementedException();
		}

		public TEntity GetById<TEntity>(object id)
		{
			throw new NotImplementedException();
		}

		public TEntity Update<TEntity>(TEntity entity)
		{
			Updated?.Invoke(entity);
			return entity;
		}

		public void Remove<TEntity>(TEntity entity)
		{
			Removed?.Invoke(entity);
			_cache.Remove(typeof(TEntity), entity);
		}

		public void Clear<TEntity>()
		{
			_cache.Remove(typeof(TEntity));
		}

		public void ClearCache()
		{
			_cache.Clear();
		}

		public IBatchContext BeginBatch()
		{
			return new BatchContext(this);
		}

		public void CommitBatch()
		{
			
		}

		public void EndBatch()
		{
			
		}

		public event Action<object> Added;
		public event Action<object> Updated;
		public event Action<object> Removed;

		public IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			return _cache[typeof(TEntity)].Skip((int)startIndex).Take((int)count).Cast<TEntity>();
		}
	}
}