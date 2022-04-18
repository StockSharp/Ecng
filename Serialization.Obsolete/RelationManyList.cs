namespace Ecng.Serialization
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Collections;

	using Nito.AsyncEx;

	public class RelationManyList<TEntity, TId> : ICollection<TEntity>, ICollection, IRangeCollection//, IAsyncEnumerable<TEntity>
	{
		#region private class RelationManyListEnumerator

		private sealed class RelationManyListEnumerator : BaseEnumerator<RelationManyList<TEntity, TId>, TEntity>//, IAsyncEnumerator<TEntity>
		{
			private readonly long _bufferSize;
			private readonly CancellationToken _cancellationToken;
			private long _startIndex;
			private IList<TEntity> _temporaryBuffer;
			private int _posInBuffer;

			public RelationManyListEnumerator(RelationManyList<TEntity, TId> list, int bufferSize, CancellationToken cancellationToken = default)
				: base(list)
			{
				_bufferSize = bufferSize;
				_cancellationToken = cancellationToken;
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
#pragma warning disable CS0612 // Type or member is obsolete
					if (_startIndex < Source.Count)
					{
						_temporaryBuffer = (IList<TEntity>)((IRangeCollection)Source).GetRange(_startIndex, _bufferSize, default, default);

						if (!_temporaryBuffer.IsEmpty())
						{
							_startIndex += _temporaryBuffer.Count;
							_posInBuffer = 0;
							return _temporaryBuffer.First();	
						}
					}
#pragma warning restore CS0612 // Type or member is obsolete
				}
				else
					return _temporaryBuffer[++_posInBuffer];

				canProcess = false;
				return default;
			}

			//ValueTask IAsyncDisposable.DisposeAsync()
			//{
			//	Reset();
			//	return default;
			//}

			//async ValueTask<bool> IAsyncEnumerator<TEntity>.MoveNextAsync()
			//{
			//	if (_temporaryBuffer is null || _posInBuffer >= (_temporaryBuffer.Count - 1))
			//	{
			//		if (_startIndex < await Source.CountAsync(_cancellationToken))
			//		{
			//			_temporaryBuffer = (IList<TEntity>)await Source.GetRangeAsync(_startIndex, _bufferSize, default, default, _cancellationToken);

			//			if (!_temporaryBuffer.IsEmpty())
			//			{
			//				_startIndex += _temporaryBuffer.Count;
			//				_posInBuffer = 0;
			//				Current = _temporaryBuffer.First();
			//				return true;
			//			}
			//		}
			//	}
			//	else
			//	{
			//		Current = _temporaryBuffer[++_posInBuffer];
			//		return true;
			//	}

			//	Current = default;
			//	return default;
			//}
		}

		#endregion

		public virtual bool IsReadOnly { get; protected set; }

		private object _syncRoot;
		object ICollection.SyncRoot => _syncRoot ??= new object();

		bool ICollection.IsSynchronized => false;

		void ICollection.CopyTo(Array array, int index) => CopyTo((TEntity[])array, index);

		private readonly SynchronizedSet<TEntity> _cache = new();
		private int? _count;
		//private readonly CachedSynchronizedDictionary<TEntity, object> _pendingAdd = new(); 

		public RelationManyList(IStorage<TId> storage)
		{
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));
			//Storage.Added += value => DoIf<TEntity>(value, entity =>
			//{
			//	if (_cache.Remove(entity))
			//		Added?.Invoke(entity);
			//});
			//Storage.Removed += value => DoIf<TEntity>(value, e => Removed?.Invoke(e));
		}

		public IQueryable<TEntity> ToQueryable()
			=> ToQueryable(Storage);

		public IQueryable<TEntity> ToQueryable(IQueryContext queryContext)
			=> new DefaultQueryable<TEntity>(new DefaultQueryProvider<TEntity>(queryContext), null);

		private static Schema _schema;
		public static Schema Schema => _schema ??= SchemaManager.GetSchema<TEntity>();

		private class StringIdComparer : IEqualityComparer<object>
		{
			private readonly StringComparer _underlying = StringComparer.InvariantCultureIgnoreCase;

			bool IEqualityComparer<object>.Equals(object x, object y) => _underlying.Equals(x, y);
			int IEqualityComparer<object>.GetHashCode(object obj) => _underlying.GetHashCode(obj);
		}

		private AsyncReaderWriterLock _cachedEntitiesLock;
		private Dictionary<TId, TEntity> _cachedEntities;

		private (AsyncReaderWriterLock sync, Dictionary<TId, TEntity> dict) CachedEntities
		{
			get
			{
				if (_cachedEntities is null)
				{
					_cachedEntitiesLock = new();

					if (Schema.Identity != null && typeof(TId) == typeof(string))
						_cachedEntities = new(new StringIdComparer().To<IEqualityComparer<TId>>());
					else
						_cachedEntities = new();
				}

				return (_cachedEntitiesLock, _cachedEntities);
			}
		}

		public IStorage<TId> Storage { get; }

		public bool BulkLoad { get; set; }
		public bool CacheCount { get; set; }
		public TimeSpan CacheTimeOut { get; set; } = TimeSpan.MaxValue;

		public CommandType? CommandType { get; set; }

		private DateTime? _cacheExpire;
		private bool _bulkInitialized;

		private async ValueTask<bool> BulkInitialized(CancellationToken cancellationToken)
		{
			if (_bulkInitialized)
			{
				if (_cacheExpire < DateTime.UtcNow)
					await ResetCacheAsync(cancellationToken);
			}

			return _bulkInitialized;
		}

		//public void ChangeCachedCount(int diff)
		//{
		//	_count += diff;
		//}

		public virtual async ValueTask ResetCacheAsync(CancellationToken cancellationToken)
		{
			var (sync, dict) = CachedEntities;

			using var _ = await sync.WriterLockAsync();

			dict.Clear();
			_bulkInitialized = false;
			_cacheExpire = null;

			_count = null;
		}

		public virtual ValueTask<TEntity> ReadById(TId id, CancellationToken cancellationToken)
		{
			if (id is null)
				throw new ArgumentNullException(nameof(id));

			//if (BulkLoad)
			//{
			//	if (!BulkInitialized)
			//		GetRange();

			//	return CachedEntities.TryGetValue(id);
			//}
			//else
			//{
			//if (DelayAction != null && _pendingAdd.Count > 0)
			//{
			//	var pair = _pendingAdd.CachedPairs.FirstOrDefault(p => Equals(p.Value, id));

			//	if (!pair.Key.IsNull())
			//		return pair.Key;
			//}

			var identity = Schema.Identity;

			if (identity is null)
				throw new InvalidOperationException($"Schema {Schema.Name} doesn't have identity.");

			return Read(new SerializationItem(identity, id), cancellationToken);
			//}
		}

		#region Save

		public virtual async ValueTask<TEntity> SaveAsync(TEntity item, CancellationToken cancellationToken)
		{
			if (Schema.Identity is null)
				throw new InvalidOperationException($"Schema {Schema.Name} doesn't have identity.");

			return await IsSaved(item, cancellationToken)
				? await UpdateAsync(item, cancellationToken)
				: await AddAsync(item, cancellationToken);
		}

		#endregion

		#region Update

		public virtual async ValueTask<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			if (BulkLoad && await BulkInitialized(cancellationToken))
			{
				var id = GetCacheId(entity);

				var isNew = false;

				var (sync, dict) = CachedEntities;

				using (await sync.WriterLockAsync(cancellationToken))
				{
					if (!dict.ContainsKey(id))
					{
						dict.Add(id, entity);
						isNew = true;
					}
				}

				if (isNew)
					await IncrementCount(cancellationToken);
			}

			return await OnUpdate(entity, cancellationToken);
		}

		private async ValueTask IncrementCount(CancellationToken cancellationToken)
		{
			if (_count is null)
				_count = (int)(await OnGetCount(cancellationToken));
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

		protected virtual async ValueTask<bool> IsSaved(TEntity item, CancellationToken cancellationToken)
		{
			if (_cache.Contains(item))
				return true;

			var id = (TId)Schema.Identity.Accessor.GetValue(item);

			return !(await ReadById(id, cancellationToken)).IsDefault();
		}

		//private static void DoIf<T>(object obj, Action<T> action)
		//{
		//	if (obj is T t)
		//	{
		//		action(t);
		//	}
		//}

		#region BaseListEx<E> Members

		public virtual async ValueTask<int> CountAsync(CancellationToken cancellationToken)
		{
			if (BulkLoad)
			{
				if (!await BulkInitialized(cancellationToken))
					await GetRangeAsync(0, 1 /* passed count's value will be ignored and set into OnGetCount() */, default, default, default, cancellationToken);

				var (sync, dict) = CachedEntities;

				using var _ = await sync.ReaderLockAsync(cancellationToken);
				return dict.Count;
			}
			else
			{
				if (CacheCount)
				{
					if (_count is null)
						_count = (int)await OnGetCount(cancellationToken);

					return _count.Value;
				}
				else
					return (int)await OnGetCount(cancellationToken);
			}
		}

		[Obsolete]
		public int Count => ThreadingHelper.Run(() => CountAsync(default));

		[Obsolete]
		public void Add(TEntity item)
			=> ThreadingHelper.Run(() => AddAsync(item, default));

		public virtual async ValueTask<TEntity> AddAsync(TEntity item, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			if (item is null)
				throw new ArgumentNullException(nameof(item));

			//Adding?.Invoke(item);

			_cache.Add(item);

			//var id = GetCacheId(item);

			//if (DelayAction != null)
			//	_pendingAdd.Add(item, id);

			item = await OnAdd(item, cancellationToken);

			if (BulkLoad)
			{
				if (await BulkInitialized(cancellationToken))
				{
					var (sync, dict) = CachedEntities;

					using var _ = await sync.WriterLockAsync(cancellationToken);
					dict.Add(GetCacheId(item), item);
				}
				else
					await GetRange(cancellationToken);
			}

			await IncrementCount(cancellationToken);

			return item;
		}

		[Obsolete]
		public virtual void Clear()
			=> ThreadingHelper.Run(() => ClearAsync(default));

		public virtual async ValueTask ClearAsync(CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			//Clearing?.Invoke();

			_cache.Clear();

			await OnClear(cancellationToken);

			if (BulkLoad)
			{
				await GetRange(cancellationToken);
				CachedEntities.dict.Clear();
			}

			_count = 0;

			//Cleared?.Invoke();
		}

		[Obsolete]
		public virtual bool Contains(TEntity item)
			=> ThreadingHelper.Run(() => ContainsAsync(item, default));

		public virtual ValueTask<bool> ContainsAsync(TEntity item, CancellationToken cancellationToken)
			=> new(this.Any(arg => arg.Equals(item)));

		public virtual void CopyTo(TEntity[] array, int index)
			=> ThreadingHelper.Run(() => CopyTo(array, index, default));

		public virtual async ValueTask CopyTo(TEntity[] array, int index, CancellationToken cancellationToken)
			=> ((ICollection<TEntity>)await GetRangeAsync(index, await CountAsync(cancellationToken), default, default, default, cancellationToken)).CopyTo(array, 0);

		[Obsolete]
		public virtual bool Remove(TEntity item)
			=> ThreadingHelper.Run(() => RemoveAsync(item, default));

		public virtual async ValueTask<bool> RemoveAsync(TEntity item, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			//Removing?.Invoke(item);

			//ThrowExceptionIfReadOnly();
			await OnRemove(item, cancellationToken);

			if (BulkLoad)
			{
				if (await BulkInitialized(cancellationToken))
				{
					var (sync, dict) = CachedEntities;

					using var _ = await sync.WriterLockAsync(cancellationToken);
					dict.Remove(GetCacheId(item));
				}
			}

			if (_count != null)
				_count--;

			return true;
		}

		IEnumerable IRangeCollection.GetRange(long startIndex, long count, string sortExpression, ListSortDirection directions)
			=> ThreadingHelper.Run(() => GetRangeAsync(startIndex, count, default, sortExpression, directions, default));

		public virtual ValueTask<IEnumerable<TEntity>> GetRangeAsync(long startIndex, long count, bool deleted, string sortExpression, ListSortDirection directions, CancellationToken cancellationToken)
		{
			var orderBy = sortExpression.IsEmpty() ? null : Schema.Fields.TryGet(sortExpression) ?? new VoidField(sortExpression, typeof(object));
			return ReadAll(startIndex, count, deleted, orderBy, directions, cancellationToken);
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

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public virtual IEnumerator<TEntity> GetEnumerator()
			=> new RelationManyListEnumerator(this, BufferSize);

		//IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(CancellationToken cancellationToken)
		//	=> new RelationManyListEnumerator(this, BufferSize, cancellationToken);

		public virtual int IndexOf(TEntity item) => throw new NotSupportedException();

		public virtual void Insert(int index, TEntity item)
			=> ThreadingHelper.Run(() => AddAsync(item, default));

		public virtual async ValueTask RemoveAtAsync(int index, CancellationToken cancellationToken)
		{
			if (BulkLoad)
				await RemoveAsync((await GetRange(cancellationToken)).ElementAt(index), cancellationToken);
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

		protected virtual ValueTask<long> OnGetCount(CancellationToken cancellationToken)
			=> Storage.GetCountAsync<TEntity>(CommandType, cancellationToken);

		protected virtual ValueTask<TEntity> OnAdd(TEntity entity, CancellationToken cancellationToken)
			=> Storage.AddAsync(CommandType, entity, cancellationToken);

		protected virtual ValueTask<TEntity> OnGet(SerializationItemCollection by, CancellationToken cancellationToken)
			=> Storage.GetByAsync<TEntity>(CommandType, by, cancellationToken);

		protected virtual ValueTask<IEnumerable<TEntity>> OnGetGroup(long startIndex, long count, bool deleted, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken)
			=> Storage.GetGroupAsync<TEntity>(CommandType, startIndex, count, deleted, orderBy, direction, cancellationToken);

		protected virtual ValueTask<TEntity> OnUpdate(TEntity entity, CancellationToken cancellationToken)
			=> Storage.UpdateAsync(CommandType, entity, cancellationToken);

		protected virtual ValueTask OnRemove(TEntity entity, CancellationToken cancellationToken)
			=> Storage.RemoveAsync(CommandType, entity, cancellationToken);

		protected virtual ValueTask OnClear(CancellationToken cancellationToken)
			=> Storage.ClearAsync<TEntity>(CommandType, cancellationToken);

		#endregion

		public ValueTask<IEnumerable<TEntity>> ReadFirsts(long count, Field orderBy, CancellationToken cancellationToken)
			=> ReadAll(0, count, default, orderBy, ListSortDirection.Ascending, cancellationToken);

		public ValueTask<IEnumerable<TEntity>> ReadLasts(long count, Field orderBy, CancellationToken cancellationToken)
			=> ReadAll(0, count, default, orderBy, ListSortDirection.Descending, cancellationToken);

		public ValueTask<TEntity> Read(SerializationItem by, CancellationToken cancellationToken)
		{
			if (by is null)
				throw new ArgumentNullException(nameof(by));

			return Read(new SerializationItemCollection { by }, cancellationToken);
		}

		public ValueTask<TEntity> Read(SerializationItemCollection by, CancellationToken cancellationToken)
			=> OnGet(by, cancellationToken);

		private async ValueTask<IEnumerable<TEntity>> GetRange(CancellationToken cancellationToken)
			=> await GetRangeAsync(0, await CountAsync(cancellationToken), default, default, default, cancellationToken);

		public async ValueTask<IEnumerable<TEntity>> ReadAll(long startIndex, long count, bool deleted, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		{
			//if (orderBy is null)
			//	throw new ArgumentNullException(nameof(orderBy));

			if (count == 0)
				return new List<TEntity>();

			var oldStartIndex = startIndex;
			var oldCount = count;

			if (!deleted && BulkLoad)
			{
				if (await BulkInitialized(cancellationToken))
				{
					IEnumerable<TEntity> source;

					var (sync, dict) = CachedEntities;

					using (await sync.ReaderLockAsync(cancellationToken))
						source = dict.Values.ToArray();

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
					count = await OnGetCount(cancellationToken);
				}
			}

			//var pendingAdd = _pendingAdd.CachedKeys;

			var entities = (await OnGetGroup(startIndex, count, deleted, orderBy ?? Schema.Identity, direction, cancellationToken)).ToList();

			if (!deleted && BulkLoad)
			{
				if (!await BulkInitialized(cancellationToken))
				{
					var (sync, dict) = CachedEntities;

					using var _ = await sync.WriterLockAsync(cancellationToken);

					dict.Clear();

					foreach (var entity in entities)
						dict.Add(GetCacheId(entity), entity);

					_bulkInitialized = true;

					if (CacheTimeOut < TimeSpan.MaxValue)
						_cacheExpire = DateTime.UtcNow + CacheTimeOut;

					entities = entities.Skip((int)oldStartIndex).Take((int)oldCount).ToList();
				}
			}

			//if (pendingAdd.Length > 0)
			//{
			//	var set = new HashSet<TEntity>();
			//	set.AddRange(entities);
			//	set.AddRange(pendingAdd);
			//	entities = set.ToList();
			//}

			//var added = Added;

			//if (added != null)
			//	entities.ForEach(added);

			return entities;
		}

		public async ValueTask<bool> RemoveById(TId id, CancellationToken cancellationToken)
			=> await RemoveAsync(await ReadById(id, cancellationToken), cancellationToken);

		private static TId GetCacheId(TEntity entity)
		{
			if (Schema.Identity is null)
				return entity.To<TId>();

			return (TId)Schema.Identity.GetAccessor<TEntity>().GetValue(entity);
		}

		public async ValueTask<bool> TryAddAsync(TEntity entity, CancellationToken cancellationToken)
		{
			if (await ContainsAsync(entity, cancellationToken))
				return false;

			await AddAsync(entity, cancellationToken);
			return true;
		}

		//public event Func<TEntity, bool> Adding;
		//public event Action<TEntity> Added;
		//public event Func<TEntity, bool> Removing;
		//public event Func<int, bool> RemovingAt;
		//public event Action<TEntity> Removed;
		//public event Func<bool> Clearing;
		//public event Action Cleared;
		//public event Func<int, TEntity, bool> Inserting;
		//public event Action<int, TEntity> Inserted;
		//public event Action Changed;
	}
}