namespace Ecng.Serialization;

using System.Data;

using Nito.AsyncEx;

/// <summary>
/// Abstract base class for a many-relation list backed by <see cref="IStorage"/>.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TId">Entity identifier type.</typeparam>
/// <param name="storage">The underlying storage provider.</param>
public abstract class RelationManyList<TEntity, TId>(IStorage storage) : IRelationManyList<TEntity>
	where TEntity : IDbPersistable
{
	#region private class RelationManyListEnumerator

	private sealed class RelationManyListEnumerator(RelationManyList<TEntity, TId> list, CancellationToken cancellationToken) : IAsyncEnumerator<TEntity>
	{
		private readonly RelationManyList<TEntity, TId> _list = list ?? throw new ArgumentNullException(nameof(list));
		private readonly CancellationToken _cancellationToken = cancellationToken;
		private long _startIndex;
		private IEnumerator<TEntity> _range;

		TEntity IAsyncEnumerator<TEntity>.Current
			=> _range is null ? default : _range.Current;

		ValueTask IAsyncDisposable.DisposeAsync()
		{
			_range?.Dispose();
			return default;
		}

		async ValueTask<bool> IAsyncEnumerator<TEntity>.MoveNextAsync()
		{
			async ValueTask<bool> InitRange()
			{
				_range = (await _list.GetRangeAsync(_startIndex, _list.BufferSize, false, Meta.Identity?.Name, ListSortDirection.Ascending, _cancellationToken).NoWait()).GetEnumerator();
				_startIndex += _list.BufferSize;

				return _range.MoveNext();
			}

			if (_range is null)
			{
				if (!await InitRange())
					return false;
			}
			else
			{
				if (!_range.MoveNext())
				{
					_range.Dispose();

					if (!await InitRange())
						return false;
				}
			}

			return true;
		}
	}

	#endregion

	/// <summary>
	/// Gets or sets whether this list is read-only.
	/// </summary>
	public virtual bool IsReadOnly { get; protected set; }

	private int? _count;

	/// <summary>
	/// Converts the list to an <see cref="IQueryable{T}"/>.
	/// </summary>
	public virtual IQueryable<TEntity> ToQueryable() => CreateQueryable();

	/// <summary>
	/// Creates an <see cref="IQueryable{T}"/> over the list data.
	/// </summary>
	protected virtual IQueryable<TEntity> CreateQueryable()
	{
		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntitiesPair;

				using var _ = sync.ReaderLock();
				return dict.Values.ToArray().AsQueryable();
			}
		}

		return new DefaultQueryable<TEntity>(new DefaultQueryProvider<TEntity>(this), null);
	}

	private static Schema _meta;

	/// <summary>
	/// Gets the schema metadata for <typeparamref name="TEntity"/>.
	/// </summary>
	public static Schema Meta => _meta ??= SchemaRegistry.Get(typeof(TEntity));

	private class StringIdComparer : IEqualityComparer<object>
	{
		private readonly StringComparer _underlying = StringComparer.InvariantCultureIgnoreCase;

		bool IEqualityComparer<object>.Equals(object x, object y) => _underlying.Equals(x, y);
		int IEqualityComparer<object>.GetHashCode(object obj) => _underlying.GetHashCode(obj);
	}

	// Reference-type wrapper so a concurrent reader either sees a fully
	// constructed pair or null — never a torn read. (A nullable struct
	// `(AsyncReaderWriterLock, Dictionary<,>)?` here was producing
	// transient NREs under parallel access because nullable-struct reads
	// are not atomic for payloads larger than 8 bytes.)
	private sealed class BulkCachePair(AsyncReaderWriterLock sync, Dictionary<TId, TEntity> dict)
	{
		public AsyncReaderWriterLock Sync { get; } = sync;
		public Dictionary<TId, TEntity> Dict { get; } = dict;

		public void Deconstruct(out AsyncReaderWriterLock sync, out Dictionary<TId, TEntity> dict)
		{
			sync = Sync;
			dict = Dict;
		}
	}

	private BulkCachePair _cachedEntitiesPair;
	private readonly Lock _cachedEntitiesInitLock = new();

	/// <summary>
	/// Read-only view of the lazily-built bulk-load cache, keyed by entity
	/// identity. Returns <see langword="null"/> when the cache has not been
	/// initialised yet (e.g. before the first read on a <see cref="BulkLoad"/>
	/// list). Useful for diagnostics — and lets tests assert on cache state
	/// without reaching into private fields.
	/// </summary>
	public IReadOnlyDictionary<TId, TEntity> CachedEntities => Volatile.Read(ref _cachedEntitiesPair)?.Dict;

	private BulkCachePair CachedEntitiesPair
	{
		get
		{
			var existing = Volatile.Read(ref _cachedEntitiesPair);
			if (existing is not null)
				return existing;

			using (_cachedEntitiesInitLock.EnterScope())
			{
				existing = Volatile.Read(ref _cachedEntitiesPair);
				if (existing is not null)
					return existing;

				var dict = Meta.Identity != null && typeof(TId) == typeof(string)
					? new Dictionary<TId, TEntity>(new StringIdComparer().To<IEqualityComparer<TId>>())
					: [];

				var pair = new BulkCachePair(new AsyncReaderWriterLock(), dict);
				Volatile.Write(ref _cachedEntitiesPair, pair);
				return pair;
			}
		}
	}

	/// <summary>
	/// Gets the underlying storage provider.
	/// </summary>
	public IStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

	/// <summary>
	/// Gets or sets whether to load all entities in bulk on first access.
	/// </summary>
	public bool BulkLoad { get; set; }

	/// <summary>
	/// Gets or sets whether to cache the entity count.
	/// </summary>
	public bool CacheCount { get; set; }

	/// <summary>
	/// Gets or sets the cache expiration timeout.
	/// </summary>
	public TimeSpan CacheTimeOut { get; set; } = TimeSpan.MaxValue;

	private DateTime? _cacheExpire;
	private bool _bulkInitialized;

	private bool BulkInitialized()
	{
		if (_bulkInitialized)
		{
			if (_cacheExpire < DateTime.UtcNow)
				ResetCache();
		}

		return _bulkInitialized;
	}

	/// <summary>
	/// Resets the in-memory cache, forcing data to be reloaded from storage.
	/// </summary>
	public virtual void ResetCache()
	{
		var (sync, dict) = CachedEntitiesPair;

		using var _ = sync.WriterLock();

		dict.Clear();
		_bulkInitialized = false;
		_cacheExpire = null;

		using (_cachedEntitiesInitLock.EnterScope())
			_count = null;
	}

	/// <summary>
	/// Reads an entity by its identifier.
	/// </summary>
	public virtual ValueTask<TEntity> ReadById(TId id, CancellationToken cancellationToken)
	{
		if (id is null)
			throw new ArgumentNullException(nameof(id));

		var identity = Meta.Identity;

		if (identity is null)
			throw new InvalidOperationException($"Meta {Meta.Name} doesn't have identity.");

		return Storage.GetByIdAsync<TId, TEntity>(id, cancellationToken);
	}

	#region Save

	/// <summary>
	/// Saves an entity by adding it if new, or updating it if it already exists.
	/// </summary>
	public async ValueTask<TEntity> SaveAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (Meta.Identity is null)
			throw new InvalidOperationException($"Meta {Meta.Name} doesn't have identity.");

		return await IsSaved(item, cancellationToken).NoWait()
			? await UpdateAsync(item, cancellationToken).NoWait()
			: await AddAsync(item, cancellationToken).NoWait();
	}

	#endregion

	#region Update

	/// <summary>
	/// Updates an existing entity in storage.
	/// </summary>
	public async ValueTask<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		entity = await OnUpdate(entity, cancellationToken).NoWait();

		if (BulkLoad && BulkInitialized())
		{
			var id = GetCacheId(entity);

			var isNew = false;

			var (sync, dict) = CachedEntitiesPair;

			using (await sync.WriterLockAsync(cancellationToken).ConfigureAwait(false))
			{
				if (dict.TryAdd(id, entity))
				{
					isNew = true;
				}
			}

			if (isNew)
				await IncrementCount(cancellationToken).NoWait();
		}

		return entity;
	}

	private async ValueTask IncrementCount(CancellationToken cancellationToken)
	{
		if (_count is null)
		{
			var c = (int)(await OnGetCount(default, cancellationToken).NoWait());
			using (_cachedEntitiesInitLock.EnterScope())
				_count ??= c;
		}
		else
		{
			using (_cachedEntitiesInitLock.EnterScope())
				_count++;
		}
	}

	#endregion

	/// <summary>
	/// Determines whether the specified entity has already been saved to storage.
	/// </summary>
	protected abstract ValueTask<bool> IsSaved(TEntity item, CancellationToken cancellationToken);

	#region BaseListEx<E> Members

	/// <summary>
	/// Gets the count of non-deleted entities.
	/// </summary>
	public ValueTask<int> CountAsync(CancellationToken cancellationToken)
		=> CountAsync(false, cancellationToken);

	/// <summary>
	/// Gets the count of entities, optionally including deleted ones.
	/// </summary>
	public async ValueTask<int> CountAsync(bool deleted, CancellationToken cancellationToken)
	{
		if (BulkLoad)
		{
			if (!BulkInitialized())
				await GetRangeAsync(0, long.MaxValue, deleted, default, default, cancellationToken).NoWait();

			var (sync, dict) = CachedEntitiesPair;

			using var _ = await sync.ReaderLockAsync(cancellationToken).ConfigureAwait(false);
			return dict.Count;
		}
		else
		{
			if (CacheCount)
			{
				if (_count is null)
				{
					var c = (int)await OnGetCount(deleted, cancellationToken).NoWait();
					using (_cachedEntitiesInitLock.EnterScope())
						_count ??= c;
				}

				return _count.Value;
			}
			else
				return (int)await OnGetCount(deleted, cancellationToken).NoWait();
		}
	}

	/// <summary>
	/// Adds a new entity to the list and storage.
	/// </summary>
	public async ValueTask<TEntity> AddAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		if (item is null)
			throw new ArgumentNullException(nameof(item));

		item = await OnAdd(item, cancellationToken).NoWait();

		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntitiesPair;

				using var _ = await sync.WriterLockAsync(cancellationToken).ConfigureAwait(false);
				dict.Add(GetCacheId(item), item);
			}
			else
				await GetRangeAsync(cancellationToken).NoWait();
		}

		await IncrementCount(cancellationToken).NoWait();

		return item;
	}

	/// <summary>
	/// Removes all entities from the list and storage.
	/// </summary>
	public async ValueTask ClearAsync(CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		await OnClear(cancellationToken).NoWait();

		if (BulkLoad)
		{
			await GetRangeAsync(cancellationToken).NoWait();

			var (sync, dict) = CachedEntitiesPair;

			using var _ = await sync.WriterLockAsync(cancellationToken).ConfigureAwait(false);
			dict.Clear();
		}

		using (_cachedEntitiesInitLock.EnterScope())
			_count = 0;
	}

	/// <summary>
	/// Determines whether the list contains the specified entity.
	/// </summary>
	public abstract ValueTask<bool> ContainsAsync(TEntity item, CancellationToken cancellationToken);

	/// <summary>
	/// Copies entities to the specified array starting at the given index.
	/// </summary>
	public async ValueTask CopyToAsync(TEntity[] array, int index, CancellationToken cancellationToken)
		=> ((ICollection<TEntity>)await GetRangeAsync(index, long.MaxValue, false, default, default, cancellationToken).NoWait()).CopyTo(array, index);

	/// <summary>
	/// Removes the specified entity from the list and storage.
	/// </summary>
	public async ValueTask<bool> RemoveAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		var retVal = await OnRemove(item, cancellationToken).NoWait();

		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntitiesPair;

				using var _ = await sync.WriterLockAsync(cancellationToken).ConfigureAwait(false);
				dict.Remove(GetCacheId(item));
			}
		}

		using (_cachedEntitiesInitLock.EnterScope())
		{
			if (_count != null)
				_count--;
		}

		return retVal;
	}

	private ValueTask<IEnumerable<TEntity>> GetRangeAsync(CancellationToken cancellationToken)
		=> GetRangeAsync(0, long.MaxValue, false, default, default, cancellationToken);

	/// <summary>
	/// Gets a range of entities with paging, sorting, and optional deleted filter.
	/// </summary>
	public async ValueTask<IEnumerable<TEntity>> GetRangeAsync(long startIndex, long count, bool deleted, string sortExpression, ListSortDirection direction, CancellationToken cancellationToken)
	{
		var orderByColumn = sortExpression;

		if (count == 0)
			return [];

		var oldStartIndex = startIndex;
		var oldCount = count;

		static IEnumerable<TEntity> ApplySkipTake(IEnumerable<TEntity> source, long skip, long take)
		{
			if (skip > 0)
				source = source.Skip((int)skip);

			if (take != long.MaxValue)
			{
				if (take >= int.MaxValue)
					throw new OverflowException(nameof(take));

				source = source.Take((int)take);
			}

			return source;
		}

		IEnumerable<TEntity> OrderAndPage(IEnumerable<TEntity> snapshot)
		{
			if (orderByColumn is not null)
			{
				object KeySelector(TEntity entity)
				{
					if (orderByColumn == Meta.Identity?.Name)
						return entity.GetIdentity();

					var s = new SettingsStorage();
					entity.Save(s);
					s.TryGetValue(orderByColumn, out var v);
					return v;
				}

				snapshot = direction == ListSortDirection.Ascending ? snapshot.OrderBy(KeySelector) : snapshot.OrderByDescending(KeySelector);
			}

			return ApplySkipTake(snapshot, oldStartIndex, oldCount);
		}

		if (!deleted && BulkLoad)
		{
			var (sync, dict) = CachedEntitiesPair;

			if (BulkInitialized())
			{
				TEntity[] cached;

				using (await sync.ReaderLockAsync(cancellationToken).ConfigureAwait(false))
					cached = [.. dict.Values];

				return OrderAndPage(cached);
			}

			// First-time bulk init. Serialise concurrent initialisers under
			// the writer lock so OnGetGroup is called once; re-check inside
			// to handle the race where another caller populated the cache
			// while we were waiting for the lock.
			using (await sync.WriterLockAsync(cancellationToken).ConfigureAwait(false))
			{
				if (!_bulkInitialized)
				{
					List<TEntity> loaded = [];
					long offset = 0;

					while (true)
					{
						var buffer = await OnGetGroup(offset, BufferSize, deleted, orderByColumn, direction, cancellationToken).NoWait();
						loaded.AddRange(buffer);

						if (buffer.Length < BufferSize)
							break;

						offset += BufferSize;
					}

					dict.Clear();

					foreach (var entity in loaded)
						dict.Add(GetCacheId(entity), entity);

					_bulkInitialized = true;

					if (CacheTimeOut < TimeSpan.MaxValue)
						_cacheExpire = DateTime.UtcNow + CacheTimeOut;
				}
			}

			TEntity[] postInit;

			using (await sync.ReaderLockAsync(cancellationToken).ConfigureAwait(false))
				postInit = [.. dict.Values];

			return OrderAndPage(postInit);
		}

		List<TEntity> entities = [];

		while (count > 0)
		{
			var pageSize = count.Min(BufferSize);

			var buffer = await OnGetGroup(startIndex, pageSize, deleted, orderByColumn, direction, cancellationToken).NoWait();

			entities.AddRange(buffer);

			if (buffer.Length < pageSize)
				break;

			count -= pageSize;
			startIndex += pageSize;
		}

		return entities;
	}

	private int _bufferSize = 20;

	/// <summary>
	/// Gets or sets the page size used for batched loading from storage.
	/// </summary>
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

	IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(CancellationToken cancellationToken)
		=> new RelationManyListEnumerator(this, cancellationToken);

	#endregion

	#region Virtual CRUD Methods

	/// <summary>
	/// Gets the total count of entities from storage.
	/// </summary>
	protected virtual ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
		=> Storage.GetCountAsync<TEntity>(cancellationToken);

	/// <summary>
	/// Adds an entity to the underlying storage.
	/// </summary>
	protected virtual ValueTask<TEntity> OnAdd(TEntity entity, CancellationToken cancellationToken)
		=> Storage.AddAsync(entity, cancellationToken);

	/// <summary>
	/// Retrieves a page of entities from the underlying storage.
	/// </summary>
	protected virtual ValueTask<TEntity[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		=> Storage.GetGroupAsync<TEntity>(startIndex, count, deleted, orderBy, direction, cancellationToken);

	/// <summary>
	/// Updates an entity in the underlying storage.
	/// </summary>
	protected virtual ValueTask<TEntity> OnUpdate(TEntity entity, CancellationToken cancellationToken)
		=> Storage.UpdateAsync(entity, cancellationToken);

	/// <summary>
	/// Removes an entity from the underlying storage.
	/// </summary>
	protected virtual ValueTask<bool> OnRemove(TEntity entity, CancellationToken cancellationToken)
		=> Storage.RemoveAsync(entity, cancellationToken);

	/// <summary>
	/// Clears all entities from the underlying storage.
	/// </summary>
	protected virtual ValueTask OnClear(CancellationToken cancellationToken)
		=> Storage.ClearAsync<TEntity>(cancellationToken);

	#endregion

	/// <summary>
	/// Removes an entity by its identifier.
	/// </summary>
	public async ValueTask<bool> RemoveById(TId id, CancellationToken cancellationToken)
		=> await RemoveAsync(await ReadById(id, cancellationToken).NoWait(), cancellationToken).NoWait();

	private static TId GetCacheId(TEntity entity)
	{
		if (Meta.Identity is null)
			return entity.To<TId>();

		return entity.GetIdentity().To<TId>();
	}

	/// <summary>
	/// Tries to add an entity, returning false if it already exists.
	/// </summary>
	public async ValueTask<bool> TryAddAsync(TEntity entity, CancellationToken cancellationToken)
	{
		if (await ContainsAsync(entity, cancellationToken).NoWait())
			return false;

		await AddAsync(entity, cancellationToken).NoWait();
		return true;
	}

	async ValueTask<IQueryable<TEntity>> IRelationManyList<TEntity>.TryInitBulkLoad(CancellationToken cancellationToken)
	{
		if (!BulkLoad || BulkInitialized())
			return default;

		return (await GetRangeAsync(cancellationToken).NoWait()).AsQueryable();
	}
}
