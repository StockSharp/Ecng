namespace Ecng.Serialization
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	using Nito.AsyncEx;

	public abstract class RelationManyList<TEntity, TId> : ICollection<TEntity>, ICollection, IRangeCollection//, IAsyncEnumerable<TEntity>
	{
		private class QueryProvider : IQueryProvider
		{
			private readonly IQueryContext _context;

			private readonly MethodInfo _execEnum;
			private readonly MethodInfo _execEnumAsync;
			private readonly MethodInfo _execAsync;
			private readonly MethodInfo _execResultAsync;

			public QueryProvider(IQueryContext context)
			{
				_context = context ?? throw new ArgumentNullException(nameof(context));

				_execEnum = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnum));
				_execEnumAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteEnumAsync));
				_execAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteAsync));
				_execResultAsync = typeof(IQueryContext).GetMethod(nameof(IQueryContext.ExecuteResultAsync));
			}

			IQueryable IQueryProvider.CreateQuery(Expression expression)
			{
				Type elementType = expression.Type;

				try
				{
					return
					   (IQueryable)Activator.CreateInstance(typeof(RelationManyList<,>).
							  Make(elementType, typeof(long)), new object[] { this, expression });
				}
				catch (TargetInvocationException e)
				{
					throw e.InnerException;
				}
			}

			IQueryable<T> IQueryProvider.CreateQuery<T>(Expression expression)
				=> new Queryable<T>(this, expression);

			object IQueryProvider.Execute(Expression expression)
				=> throw new NotSupportedException();

			T IQueryProvider.Execute<T>(Expression expression)
			{
				if (typeof(T).GetGenericType(typeof(IEnumerable<>)) is not null)
				{
					return (T)_execEnum.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
				}
				else if (typeof(T).GetGenericType(typeof(IAsyncEnumerable<>)) is not null)
				{
					return (T)_execEnumAsync.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
				}
				else if (typeof(T) == typeof(Task))
				{
					return (T)_execAsync.Make(typeof(TEntity)).Invoke(_context, new object[] { expression });
				}
				else if (typeof(T).IsSubclassOf(typeof(Task)))
				{
					return (T)_execResultAsync.Make(typeof(TEntity), typeof(T).GetGenericArguments().First()).Invoke(_context, new object[] { expression });
				}
				else
					throw new NotSupportedException();
			}
		}

		private class Queryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
		{
			public Queryable(IQueryProvider provider, Expression expression)
			{
				if (expression != null && !typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
					throw new ArgumentException($"Not assignable from {expression.Type}.", nameof(expression));

				_provider = provider ?? throw new ArgumentNullException(nameof(provider));
				_expression = expression ?? Expression.Constant(this);
			}

			Type IQueryable.ElementType => typeof(T);

			private readonly Expression _expression;
			Expression IQueryable.Expression => _expression;

			private readonly IQueryProvider _provider;
			IQueryProvider IQueryable.Provider => _provider;

			IEnumerator<T> IEnumerable<T>.GetEnumerator() => _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _provider.Execute<IEnumerable>(_expression).GetEnumerator();

			IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
				=> _provider.Execute<IAsyncEnumerable<T>>(_expression).GetAsyncEnumerator(cancellationToken);
		}

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

		public virtual bool IsReadOnly => false;

		private object _syncRoot;
		object ICollection.SyncRoot => _syncRoot ??= new object();

		bool ICollection.IsSynchronized => false;

		void ICollection.CopyTo(Array array, int index) => CopyTo((TEntity[])array, index);

		private readonly SynchronizedSet<TEntity> _cache = new();
		private int? _count;
		//private readonly CachedSynchronizedDictionary<TEntity, object> _pendingAdd = new(); 

		protected RelationManyList(IStorage<TId> storage)
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
			=> new Queryable<TEntity>(new QueryProvider(queryContext), null);

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

		private DateTime? _cacheExpire;
		private bool _bulkInitialized;

		private async Task<bool> BulkInitialized(CancellationToken cancellationToken)
		{
			if (_bulkInitialized)
			{
				if (_cacheExpire < DateTime.UtcNow)
					await ResetCache(cancellationToken);
			}

			return _bulkInitialized;
		}

		//public void ChangeCachedCount(int diff)
		//{
		//	_count += diff;
		//}

		public virtual async Task ResetCache(CancellationToken cancellationToken)
		{
			var (sync, dict) = CachedEntities;

			using var _ = await sync.WriterLockAsync();

			dict.Clear();
			_bulkInitialized = false;
			_cacheExpire = null;

			_count = null;
		}

		public virtual Task<TEntity> ReadById(TId id, CancellationToken cancellationToken)
		{
			if (id is null)
				throw new ArgumentNullException(nameof(id));

			ThrowIfStorageNull();

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

		public virtual async Task SaveAsync(TEntity item, CancellationToken cancellationToken)
		{
			if (Schema.Identity is null)
				throw new InvalidOperationException($"Schema {Schema.Name} doesn't have identity.");

			if (!await CheckExist(item, cancellationToken))
				await AddAsync(item, cancellationToken);
			else
				await UpdateAsync(item, cancellationToken);
		}

		#endregion

		#region Update

		public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken)
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

			await OnUpdate(entity, cancellationToken);
		}

		private async Task IncrementCount(CancellationToken cancellationToken)
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

		private async Task<bool> CheckExist(TEntity item, CancellationToken cancellationToken)
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

		public virtual async Task<int> CountAsync(CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();

			if (BulkLoad)
			{
				if (!await BulkInitialized(cancellationToken))
					await GetRangeAsync(0, 1 /* passed count's value will be ignored and set into OnGetCount() */, default, default, cancellationToken);

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
		public int Count => AsyncContext.Run(() => CountAsync(default));

		[Obsolete]
		public void Add(TEntity item)
			=> AsyncContext.Run(() => AddAsync(item, default));

		public virtual async Task AddAsync(TEntity item, CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			if (item is null)
				throw new ArgumentNullException(nameof(item));

			//Adding?.Invoke(item);

			ThrowIfStorageNull();

			_cache.Add(item);

			//var id = GetCacheId(item);

			//if (DelayAction != null)
			//	_pendingAdd.Add(item, id);

			await OnAdd(item, cancellationToken);

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
		}

		[Obsolete]
		public virtual void Clear()
			=> AsyncContext.Run(() => ClearAsync(default));

		public virtual async Task ClearAsync(CancellationToken cancellationToken)
		{
			if (IsReadOnly)
				throw new ReadOnlyException();

			//Clearing?.Invoke();

			_cache.Clear();

			ThrowIfStorageNull();

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
			=> AsyncContext.Run(() => ContainsAsync(item, default));

		public virtual Task<bool> ContainsAsync(TEntity item, CancellationToken cancellationToken)
			=> Task.FromResult(this.Any(arg => arg.Equals(item)));

		public virtual void CopyTo(TEntity[] array, int index)
			=> AsyncContext.Run(() => CopyTo(array, index, default));

		public virtual async Task CopyTo(TEntity[] array, int index, CancellationToken cancellationToken)
			=> ((ICollection<TEntity>)await GetRangeAsync(index, await CountAsync(cancellationToken), default, default, cancellationToken)).CopyTo(array, 0);

		[Obsolete]
		public virtual bool Remove(TEntity item)
			=> AsyncContext.Run(() => RemoveAsync(item, default));

		public virtual async Task<bool> RemoveAsync(TEntity item, CancellationToken cancellationToken)
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
			=> AsyncContext.Run(() => GetRangeAsync(startIndex, count, sortExpression, directions, default));

		public virtual Task<IEnumerable<TEntity>> GetRangeAsync(long startIndex, long count, string sortExpression, ListSortDirection directions, CancellationToken cancellationToken)
		{
			var orderBy = sortExpression.IsEmpty() ? null : Schema.Fields.TryGet(sortExpression) ?? new VoidField(sortExpression, typeof(object));
			return ReadAll(startIndex, count, orderBy, directions, cancellationToken);
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

		public virtual int IndexOf(TEntity item)
		{
			if (BulkLoad)
				throw new NotImplementedException();
			else
				throw new NotSupportedException();
		}

		public virtual void Insert(int index, TEntity item)
		{
			AsyncContext.Run(() => AddAsync(item, default));
		}

		public virtual async Task RemoveAtAsync(int index, CancellationToken cancellationToken)
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

		protected virtual Task<long> OnGetCount(CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.GetCount<TEntity>(cancellationToken);
		}

		protected virtual Task OnAdd(TEntity entity, CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.Add(entity, cancellationToken);
		}

		protected virtual Task<TEntity> OnGet(SerializationItemCollection by, CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.GetBy<TEntity>(by, cancellationToken);
		}

		protected virtual Task<IEnumerable<TEntity>> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.GetGroup<TEntity>(startIndex, count, orderBy, direction, cancellationToken);
		}

		protected virtual Task OnUpdate(TEntity entity, CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.Update(entity, cancellationToken);
		}

		protected virtual Task OnRemove(TEntity entity, CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.Remove(entity, cancellationToken);
		}

		protected virtual Task OnClear(CancellationToken cancellationToken)
		{
			ThrowIfStorageNull();
			return Storage.Clear<TEntity>(cancellationToken);
		}

		#endregion

		public Task<IEnumerable<TEntity>> ReadFirsts(long count, Field orderBy, CancellationToken cancellationToken)
		{
			return ReadAll(0, count, orderBy, ListSortDirection.Ascending, cancellationToken);
		}

		public Task<IEnumerable<TEntity>> ReadLasts(long count, Field orderBy, CancellationToken cancellationToken)
		{
			return ReadAll(0, count, orderBy, ListSortDirection.Descending, cancellationToken);
		}

		public Task<TEntity> Read(SerializationItem by, CancellationToken cancellationToken)
		{
			if (by is null)
				throw new ArgumentNullException(nameof(by));

			return Read(new SerializationItemCollection { by }, cancellationToken);
		}

		public Task<TEntity> Read(SerializationItemCollection by, CancellationToken cancellationToken)
		{
			return OnGet(by, cancellationToken);
		}

		private async Task<IEnumerable<TEntity>> GetRange(CancellationToken cancellationToken)
		{
			return await GetRangeAsync(0, await CountAsync(cancellationToken), default, default, cancellationToken);
		}

		public async Task<IEnumerable<TEntity>> ReadAll(long startIndex, long count, Field orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		{
			//if (orderBy is null)
			//	throw new ArgumentNullException(nameof(orderBy));

			if (count == 0)
				return new List<TEntity>();

			var oldStartIndex = startIndex;
			var oldCount = count;

			if (BulkLoad)
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

			ThrowIfStorageNull();

			//var pendingAdd = _pendingAdd.CachedKeys;

			var entities = (await OnGetGroup(startIndex, count, orderBy ?? Schema.Identity, direction, cancellationToken)).ToList();

			if (BulkLoad)
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

		public async Task RemoveById(TId id, CancellationToken cancellationToken)
		{
			await RemoveAsync(await ReadById(id, cancellationToken), cancellationToken);
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