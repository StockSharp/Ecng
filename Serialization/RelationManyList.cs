namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Web.UI.WebControls;

	using Ecng.Common;
	using Ecng.Collections;

	public abstract class RelationManyList<TEntity> : BaseListEx<TEntity>, INotifyList<TEntity>
	{
		#region private class RelationManyListEnumerator

		private sealed class RelationManyListEnumerator : BaseEnumerator<RelationManyList<TEntity>, TEntity>
		{
			private const long _bufferSize = 20;

			private long _startIndex;
			private ICollection<TEntity> _temporaryBuffer;
			private int _posInBuffer;

			public RelationManyListEnumerator(RelationManyList<TEntity> list)
				: base(list)
			{
			}

			public override void Reset()
			{
				base.Reset();
				_startIndex = 0;
				_posInBuffer = -1;
				_temporaryBuffer = null;
			}

			protected override TEntity ProcessMove(ref bool canProcess)
			{
				if (_temporaryBuffer == null || _posInBuffer >= (_temporaryBuffer.Count - 1))
				{
					if (_startIndex < Source.Count)
					{
						_temporaryBuffer = (ICollection<TEntity>)Source.GetRange(_startIndex, _bufferSize);

						if (_temporaryBuffer.IsEmpty())
							throw new InvalidOperationException();

						_startIndex += _temporaryBuffer.Count;
						_posInBuffer = 0;
						return _temporaryBuffer.First();
					}
					else
					{
						canProcess = false;
						return default(TEntity);
					}
				}
				else
					return _temporaryBuffer.ElementAt(++_posInBuffer);
			}
		}

		#endregion

		private bool _bulkInitialized;
		private readonly SynchronizedDictionary<object, TEntity> _cachedEntities = new SynchronizedDictionary<object, TEntity>();
		private int? _count;

		protected RelationManyList(IStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			Storage = storage;
		}

		private static Schema _schema;

		public static Schema Schema
		{
			get { return _schema ?? (_schema = SchemaManager.GetSchema<TEntity>()); }
		}

		public IStorage Storage { get; private set; }

		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }

		public void ChangeCachedCount(int diff)
		{
			_count += diff;
		}

		public void ResetCache()
		{
			lock (_cachedEntities.SyncRoot)
			{
				_cachedEntities.Clear();
				_bulkInitialized = false;
			}

			_count = null;
		}

		#region Item

		public TEntity this[object id]
		{
			get { return ReadById(id); }
		}

		#endregion

		public virtual TEntity ReadById(object id)
		{
			if (id == null)
				throw new ArgumentNullException("id");

			ThrowIfStorageNull();

			if (BulkLoad)
			{
				GetRange();
				return _cachedEntities.TryGetValue(id);
			}
			else
			{
				var identity = Schema.Identity;

				if (identity == null)
					throw new InvalidOperationException("Schema {0} doesn't have identity.".Put(Schema.Name));

				return Read(new SerializationItem(identity, id));
			}
		}

		#region Update

		public virtual void Update(TEntity entity)
		{
			if (BulkLoad && _bulkInitialized)
			{
				var id = GetCacheId(entity);

				if (!_cachedEntities.ContainsKey(id))
				{
					_cachedEntities.Add(id, entity);
					_count++;
				}
			}

			OnUpdate(entity);
		}

		//public void Update(TEntity entity, Field valueField)
		//{
		//    Update(entity, new FieldCollection(valueField));
		//}

		//public void Update(TEntity entity, FieldCollection valueFields)
		//{
		//    Update(entity, Schema.Identity, valueFields);
		//}

		//public void Update(TEntity entity, Field keyField, FieldCollection valueFields)
		//{
		//    Update(entity, new FieldCollection(keyField), valueFields);
		//}

		//public void Update(TEntity entity, FieldCollection keyFields, FieldCollection valueFields)
		//{
		//    OnUpdate(entity, keyFields, valueFields);
		//}

		#endregion

		#region BaseListEx<E> Members

		public override int Count
		{
			get
			{
				ThrowIfStorageNull();

				if (BulkLoad)
				{
					if (!_bulkInitialized)
						GetRange(0, 1 /* passed count's value will be ingored and set into OnGetCount() */);

					return _cachedEntities.Count;
				}
				else
				{
					if ((CacheCount && _count == null) || !CacheCount)
						_count = (int)OnGetCount();

					return _count ?? 0;
				}
			}
		}

		public override void Add(TEntity item)
		{
			Adding.SafeInvoke(item);

			ThrowIfStorageNull();
			OnAdd(item);

			if (BulkLoad)
			{
				lock (_cachedEntities.SyncRoot)
				{
					if (_bulkInitialized)
						_cachedEntities.Add(GetCacheId(item), item);
					else
						GetRange();
				}
			}

			_count++;

			Added.SafeInvoke(item);
		}

		public override void Clear()
		{
			Clearing.SafeInvoke();

			ThrowIfStorageNull();
			OnClear();

			if (BulkLoad)
			{
				GetRange();
				_cachedEntities.Clear();
			}

			_count = 0;

			Cleared.SafeInvoke();
		}

		public override bool Contains(TEntity item)
		{
			return this.Any(arg => arg.Equals(item));
		}

		public override void CopyTo(TEntity[] array, int index)
		{
			((ICollection<TEntity>)GetRange(index, Count)).CopyTo(array, 0);
		}

		public override bool Remove(TEntity item)
		{
			Removing.SafeInvoke(item);

			//ThrowExceptionIfReadOnly();
			OnRemove(item);

			if (BulkLoad)
			{
				lock (_cachedEntities.SyncRoot)
				{
					if (_bulkInitialized)
						_cachedEntities.Remove(GetCacheId(item));
				}
			}

			Removed.SafeInvoke(item);

			return true;
		}

		public override IEnumerable<TEntity> GetRange(long startIndex, long count, string sortExpression = null, SortDirection directions = SortDirection.Ascending)
		{
			var orderBy = sortExpression.IsEmpty() ? null : Schema.Fields[sortExpression];
			return ReadAll(startIndex, count, orderBy, directions);
		}

		public override IEnumerator<TEntity> GetEnumerator()
		{
			return new RelationManyListEnumerator(this);
		}

		public override int IndexOf(TEntity item)
		{
			if (BulkLoad)
				throw new NotImplementedException();
			else
				throw new NotSupportedException();
		}

		public override void Insert(int index, TEntity item)
		{
			Add(item);
		}

		public override void RemoveAt(int index)
		{
			if (BulkLoad)
				Remove(GetRange().ElementAt(index));
			else
				throw new NotSupportedException();
		}

		public override TEntity this[int index]
		{
			get
			{
				if (BulkLoad)
				{
					return GetRange().ElementAt(index);
				}
				else
					throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region Virtual CRUD Methods

		protected virtual long OnGetCount()
		{
			ThrowIfStorageNull();
			return Storage.GetCount<TEntity>();
		}

		protected virtual void OnAdd(TEntity entity)
		{
			ThrowIfStorageNull();
			Storage.Add(entity);
		}

		protected virtual TEntity OnGet(SerializationItemCollection by)
		{
			ThrowIfStorageNull();
			return Storage.GetBy<TEntity>(by);
		}

		protected virtual IEnumerable<TEntity> OnGetGroup(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			ThrowIfStorageNull();
			return Storage.GetGroup<TEntity>(startIndex, count, orderBy, direction);
		}

		protected virtual void OnUpdate(TEntity entity)
		{
			ThrowIfStorageNull();
			Storage.Update(entity);
		}

		protected virtual void OnRemove(TEntity entity)
		{
			ThrowIfStorageNull();
			Storage.Remove(entity);
		}

		protected virtual void OnClear()
		{
			ThrowIfStorageNull();
			Storage.Clear<TEntity>();
		}

		#endregion

		public IEnumerable<TEntity> ReadFirsts(long count, Field orderBy)
		{
			return ReadAll(0, count, orderBy, SortDirection.Ascending);
		}

		public IEnumerable<TEntity> ReadLasts(long count, Field orderBy)
		{
			return ReadAll(0, count, orderBy, SortDirection.Descending);
		}

		public TEntity Read(SerializationItem by)
		{
			if (by == null)
				throw new ArgumentNullException("by");

			return Read(new SerializationItemCollection { by });
		}

		public TEntity Read(SerializationItemCollection by)
		{
			return OnGet(by);
		}

		private IEnumerable<TEntity> GetRange()
		{
			return GetRange(0, Count);
		}

		public IEnumerable<TEntity> ReadAll(long startIndex, long count, Field orderBy, SortDirection direction)
		{
			//if (orderBy == null)
			//	throw new ArgumentNullException("orderBy");

			if (count == 0)
				return new List<TEntity>();

			var oldStartIndex = startIndex;
			var oldCount = count;

			if (BulkLoad)
			{
				if (_bulkInitialized)
				{
					IEnumerable<TEntity> source = _cachedEntities.Values;

					if (orderBy != null)
					{
						Func<TEntity, object> keySelector = entity => orderBy.GetAccessor<TEntity>().GetValue(entity);
						source = direction == SortDirection.Ascending ? source.OrderBy(keySelector) : source.OrderByDescending(keySelector);
					}

					return new ListEx<TEntity>(source.Skip((int)startIndex).Take((int)count));
				}
				else
				{
					startIndex = 0;
					count = OnGetCount();
				}
			}

			ThrowIfStorageNull();

			var entities = OnGetGroup(startIndex, count, orderBy ?? Schema.Identity, direction);

			if (BulkLoad)
			{
				lock (_cachedEntities.SyncRoot)
				{
					if (!_bulkInitialized)
					{
						//_cachedEntities = new Dictionary<object, E>();

						foreach (var entity in entities)
							_cachedEntities.Add(GetCacheId(entity), entity);

						_bulkInitialized = true;

						entities = entities.Skip((int)oldStartIndex).Take((int)oldCount).ToList();
					}
				}
			}

			var added = Added;

			if (added != null)
				entities.ForEach(added);

			return entities;
		}

		public void RemoveById(object id)
		{
			Remove(ReadById(id));
		}

		private static object GetCacheId(TEntity entity)
		{
			if (Schema.Identity == null)
				return entity;

			return Schema.Identity.GetAccessor<TEntity>().GetValue(entity);
		}

		private void ThrowIfStorageNull()
		{
			if (Storage == null)
				throw new InvalidOperationException();
		}

		public event Action<TEntity> Adding;
		public event Action<TEntity> Added;
		public event Action<TEntity> Removing;
		public event Action<TEntity> Removed;
		public event Action Clearing;
		public event Action Cleared;
		public event Action<int, TEntity> Inserting;
		public event Action<int, TEntity> Inserted;
		public event Action Changed;
	}
}