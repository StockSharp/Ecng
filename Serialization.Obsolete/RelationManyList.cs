namespace Ecng.Serialization
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	public abstract class RelationManyList<TEntity, TId> : INotifyCollection<TEntity>, ICollection<TEntity>, ICollection, IRangeCollection
	{
		#region private class RelationManyListEnumerator

		private sealed class RelationManyListEnumerator : BaseEnumerator<RelationManyList<TEntity, TId>, TEntity>
		{
			private readonly long _bufferSize;

			private long _startIndex;
			private ICollection<TEntity> _temporaryBuffer;
			private int _posInBuffer;

			public RelationManyListEnumerator(RelationManyList<TEntity, TId> list, int bufferSize)
				: base(list)
			{
				_bufferSize = bufferSize;
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
				if (_temporaryBuffer is null || _posInBuffer >= (_temporaryBuffer.Count - 1))
				{
					if (_startIndex < Source.Count)
					{
						_temporaryBuffer = (ICollection<TEntity>)Source.GetRange(_startIndex, _bufferSize);

						if (!_temporaryBuffer.IsEmpty())
						{
							_startIndex += _temporaryBuffer.Count;
							_posInBuffer = 0;
							return _temporaryBuffer.First();	
						}
					}
				}
				else
					return _temporaryBuffer.ElementAt(++_posInBuffer);

				canProcess = false;
				return default;
			}
		}

		#endregion

		IEnumerable IRangeCollection.GetRange(long startIndex, long count, string sortExpression, ListSortDirection directions)
		{
			return GetRange(startIndex, count, sortExpression, directions);
		}

		public virtual bool IsReadOnly => false;

		private object _syncRoot;

		object ICollection.SyncRoot => _syncRoot ??= new object();

		bool ICollection.IsSynchronized => false;

		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((TEntity[])array, index);
		}

		private readonly SynchronizedSet<TEntity> _cache = new();
		private bool _bulkInitialized;
		private int? _count;
		private readonly CachedSynchronizedDictionary<TEntity, object> _pendingAdd = new(); 

		protected RelationManyList(IStorage<TId> storage)
		{
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));
			Storage.Added += value => DoIf<TEntity>(value, entity =>
			{
				if (_cache.Remove(entity))
					Added?.Invoke(entity);
			});
			Storage.Removed += value => DoIf<TEntity>(value, e => Removed?.Invoke(e));
		}

		private static Schema _schema;

		public static Schema Schema => _schema ??= SchemaManager.GetSchema<TEntity>();

		private class StringIdComparer : IEqualityComparer<object>
		{
			bool IEqualityComparer<object>.Equals(object x, object y)
			{
				return StringComparer.InvariantCultureIgnoreCase.Equals(x, y);
			}

			int IEqualityComparer<object>.GetHashCode(object obj)
			{
				return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj);
			}
		}

		private SynchronizedDictionary<TId, TEntity> _cachedEntities;

		private SynchronizedDictionary<TId, TEntity> CachedEntities
		{
			get
			{
				if (_cachedEntities is null)
				{
					if (Schema.Identity != null && typeof(TId) == typeof(string))
						_cachedEntities = new SynchronizedDictionary<TId, TEntity>(new StringIdComparer().To<IEqualityComparer<TId>>());
					else
						_cachedEntities = new SynchronizedDictionary<TId, TEntity>();
				}

				return _cachedEntities;
			}
		}

		public IStorage<TId> Storage { get; }

		public StorageDelayAction DelayAction { get; set; }

		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }

		//public void ChangeCachedCount(int diff)
		//{
		//	_count += diff;
		//}

		public virtual void ResetCache()
		{
			lock (CachedEntities.SyncRoot)
			{
				CachedEntities.Clear();
				_bulkInitialized = false;
			}

			_count = null;
		}

		public virtual TEntity ReadById(TId id)
		{
			if (id is null)
				throw new ArgumentNullException(nameof(id));

			ThrowIfStorageNull();

			if (BulkLoad)
			{
				if (!_bulkInitialized)
					GetRange();

				return CachedEntities.TryGetValue(id);
			}
			else
			{
				if (DelayAction != null && _pendingAdd.Count > 0)
				{
					var pair = _pendingAdd.CachedPairs.FirstOrDefault(p => Equals(p.Value, id));

					if (!pair.Key.IsNull())
						return pair.Key;
				}

				var identity = Schema.Identity;

				if (identity is null)
					throw new InvalidOperationException("Schema {0} doesn't have identity.".Put(Schema.Name));

				return Read(new SerializationItem(identity, id));
			}
		}

		#region Save

		public virtual void Save(TEntity item)
		{
			if (Schema.Identity is null)
				throw new InvalidOperationException("Schema {0} doesn't have identity.".Put(Schema.Name));

			if (!CheckExist(item))
				Add(item);
			else
				Update(item);
		}

		#endregion

		#region Update

		public virtual void Update(TEntity entity)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			if (BulkLoad && _bulkInitialized)
			{
				var id = GetCacheId(entity);

				if (!CachedEntities.ContainsKey(id))
				{
					CachedEntities.Add(id, entity);
					IncrementCount();
				}
			}

			ProcessDelayed(() => OnUpdate(entity));
		}

		private void IncrementCount()
		{
			if (_count is null)
				_count = (int)OnGetCount();
			else
				_count++;
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

		private void ProcessDelayed(Action action, Action<Exception> postAction = null)
		{
			if (DelayAction != null)
				DelayAction.DefaultGroup.Add(action, postAction);
			else
				action();
		}

		private bool CheckExist(TEntity item)
		{
			if (_cache.Contains(item))
				return true;

			var id = (TId)Schema.Identity.Accessor.GetValue(item);

			return !ReadById(id).IsDefault();
		}

		private static void DoIf<T>(object obj, Action<T> action)
		{
			if (obj is T t)
			{
				action(t);
			}
		}

		#region BaseListEx<E> Members

		public virtual int Count
		{
			get
			{
				ThrowIfStorageNull();

				if (BulkLoad)
				{
					if (!_bulkInitialized)
						GetRange(0, 1 /* passed count's value will be ignored and set into OnGetCount() */);

					return CachedEntities.Count;
				}
				else
				{
					if (CacheCount)
					{
						if (_count is null)
							_count = (int)OnGetCount();

						return _count.Value;
					}
					else
						return (int)OnGetCount();
				}
			}
		}

		public virtual void Add(TEntity item)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			if (item is null)
				throw new ArgumentNullException(nameof(item));

			Adding?.Invoke(item);

			ThrowIfStorageNull();

			_cache.Add(item);

			var id = GetCacheId(item);

			if (DelayAction != null)
				_pendingAdd.Add(item, id);

			ProcessDelayed(() => OnAdd(item), err => _pendingAdd.Remove(item));

			if (BulkLoad)
			{
				lock (CachedEntities.SyncRoot)
				{
					if (_bulkInitialized)
						CachedEntities.Add(GetCacheId(item), item);
					else
						GetRange();
				}
			}

			IncrementCount();
		}

		public virtual void Clear()
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			Clearing?.Invoke();

			_cache.Clear();

			ThrowIfStorageNull();
			ProcessDelayed(OnClear);

			if (BulkLoad)
			{
				GetRange();
				CachedEntities.Clear();
			}

			_count = 0;

			Cleared?.Invoke();
		}

		public virtual bool Contains(TEntity item)
		{
			return this.Any(arg => arg.Equals(item));
		}

		public virtual void CopyTo(TEntity[] array, int index)
		{
			((ICollection<TEntity>)GetRange(index, Count)).CopyTo(array, 0);
		}

		public virtual bool Remove(TEntity item)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			Removing?.Invoke(item);

			//ThrowExceptionIfReadOnly();
			ProcessDelayed(() => OnRemove(item));

			if (BulkLoad)
			{
				lock (CachedEntities.SyncRoot)
				{
					if (_bulkInitialized)
						CachedEntities.Remove(GetCacheId(item));
				}
			}

			if (_count != null)
				_count--;

			return true;
		}

		public virtual IEnumerable<TEntity> GetRange(long startIndex, long count, string sortExpression = null, ListSortDirection directions = ListSortDirection.Ascending)
		{
			var orderBy = sortExpression.IsEmpty() ? null : Schema.Fields.TryGet(sortExpression) ?? new VoidField(sortExpression, typeof(object));
			return ReadAll(startIndex, count, orderBy, directions);
		}

		private int _bufferSize = 20;

		public int BufferSize
		{
			get => _bufferSize;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				_bufferSize = value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public virtual IEnumerator<TEntity> GetEnumerator()
		{
			return new RelationManyListEnumerator(this, BufferSize);
		}

		public virtual int IndexOf(TEntity item)
		{
			if (BulkLoad)
				throw new NotImplementedException();
			else
				throw new NotSupportedException();
		}

		public virtual void Insert(int index, TEntity item)
		{
			Add(item);
		}

		public virtual void RemoveAt(int index)
		{
			if (BulkLoad)
				Remove(GetRange().ElementAt(index));
			else
				throw new NotSupportedException();
		}

		//public override TEntity this[int index]
		//{
		//	get
		//	{
		//		if (BulkLoad)
		//		{
		//			return GetRange().ElementAt(index);
		//		}
		//		else
		//		{
		//			if (index != 0)
		//				throw new NotImplementedException();
		//			else
		//				return ReadFirsts(1, Schema.Identity).FirstOrDefault();
		//		}
		//	}
		//	set => throw new NotImplementedException();
		//}

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

		protected virtual IEnumerable<TEntity> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
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
			return ReadAll(0, count, orderBy, ListSortDirection.Ascending);
		}

		public IEnumerable<TEntity> ReadLasts(long count, Field orderBy)
		{
			return ReadAll(0, count, orderBy, ListSortDirection.Descending);
		}

		public TEntity Read(SerializationItem by)
		{
			if (by is null)
				throw new ArgumentNullException(nameof(by));

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

		public IEnumerable<TEntity> ReadAll(long startIndex, long count, Field orderBy, ListSortDirection direction)
		{
			//if (orderBy is null)
			//	throw new ArgumentNullException(nameof(orderBy));

			if (count == 0)
				return new List<TEntity>();

			var oldStartIndex = startIndex;
			var oldCount = count;

			if (BulkLoad)
			{
				if (_bulkInitialized)
				{
					IEnumerable<TEntity> source = CachedEntities.SyncGet(d => d.Values.ToArray());

					if (orderBy != null)
					{
						object KeySelector(TEntity entity) => orderBy.GetAccessor<TEntity>().GetValue(entity);
						source = direction == ListSortDirection.Ascending ? source.OrderBy(KeySelector) : source.OrderByDescending(KeySelector);
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

			var pendingAdd = _pendingAdd.CachedKeys;

			var entities = OnGetGroup(startIndex, count, orderBy ?? Schema.Identity, direction).ToList();

			if (BulkLoad)
			{
				lock (CachedEntities.SyncRoot)
				{
					if (!_bulkInitialized)
					{
						//_cachedEntities = new Dictionary<object, E>();

						foreach (var entity in entities)
							CachedEntities.Add(GetCacheId(entity), entity);

						_bulkInitialized = true;

						entities = entities.Skip((int)oldStartIndex).Take((int)oldCount).ToList();
					}
				}
			}

			if (pendingAdd.Length > 0)
			{
				var set = new HashSet<TEntity>();
				set.AddRange(entities);
				set.AddRange(pendingAdd);
				entities = set.ToList();
			}

			//var added = Added;

			//if (added != null)
			//	entities.ForEach(added);

			return entities;
		}

		public void RemoveById(TId id)
		{
			Remove(ReadById(id));
		}

		private static TId GetCacheId(TEntity entity)
		{
			if (Schema.Identity is null)
				return entity.To<TId>();

			return (TId)Schema.Identity.GetAccessor<TEntity>().GetValue(entity);
		}

		private void ThrowIfStorageNull()
		{
			if (Storage is null)
				throw new InvalidOperationException();
		}

		public event Func<TEntity, bool> Adding;
		public event Action<TEntity> Added;
		public event Func<TEntity, bool> Removing;
		public event Func<int, bool> RemovingAt;
		public event Action<TEntity> Removed;
		public event Func<bool> Clearing;
		public event Action Cleared;
		public event Func<int, TEntity, bool> Inserting;
		public event Action<int, TEntity> Inserted;
		public event Action Changed;
	}
}