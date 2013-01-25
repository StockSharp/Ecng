namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web.UI.WebControls;

	using Ecng.Collections;

	public class InMemoryStorage : IStorage
	{
		private readonly SynchronizedMultiDictionary<Type, object> _cache = new SynchronizedMultiDictionary<Type, object>();

		public long GetCount<TEntity>()
		{
			return _cache[typeof(TEntity)].Count;
		}

		public TEntity Add<TEntity>(TEntity entity)
		{
			_cache.Add(typeof(TEntity), entity);
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
			return entity;
		}

		public void Remove<TEntity>(TEntity entity)
		{
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

		public BatchContext BeginBatch()
		{
			return new BatchContext(this);
		}

		public void CommitBatch()
		{
			
		}

		public void EndBatch()
		{
			
		}

		public IEnumerable<TEntity> GetGroup<TEntity>(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			return _cache[typeof(TEntity)].Skip((int)startIndex).Take((int)count).Cast<TEntity>();
		}
	}
}