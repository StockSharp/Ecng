namespace Ecng.Serialization;

using System.Data;

using Nito.AsyncEx;

public abstract class RelationManyList<TEntity, TId>(IStorage storage) : IRelationManyList<TEntity>
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
				_range = (await _list.GetRangeAsync(_startIndex, _list.BufferSize, false, Meta.Identity?.Name, ListSortDirection.Ascending, _cancellationToken)).GetEnumerator();
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

	public virtual bool IsReadOnly { get; protected set; }

	private int? _count;

	public virtual IQueryable<TEntity> ToQueryable() => CreateQueryable();

	protected virtual IQueryable<TEntity> CreateQueryable()
	{
		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntities;

				using var _ = sync.ReaderLock();
				return dict.Values.ToArray().AsQueryable();
			}
		}

		return new DefaultQueryable<TEntity>(new DefaultQueryProvider<TEntity>(this), null);
	}

	private static Schema _meta;
	public static Schema Meta => _meta ??= SchemaRegistry.Get(typeof(TEntity));

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

				if (Meta.Identity != null && typeof(TId) == typeof(string))
					_cachedEntities = new(new StringIdComparer().To<IEqualityComparer<TId>>());
				else
					_cachedEntities = [];
			}

			return (_cachedEntitiesLock, _cachedEntities);
		}
	}

	public IStorage Storage { get; } = storage ?? throw new ArgumentNullException(nameof(storage));

	public bool BulkLoad { get; set; }
	public bool CacheCount { get; set; }
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

	public virtual void ResetCache()
	{
		var (sync, dict) = CachedEntities;

		using var _ = sync.WriterLock();

		dict.Clear();
		_bulkInitialized = false;
		_cacheExpire = null;

		_count = null;
	}

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

	public async ValueTask<TEntity> SaveAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (Meta.Identity is null)
			throw new InvalidOperationException($"Meta {Meta.Name} doesn't have identity.");

		return await IsSaved(item, cancellationToken)
			? await UpdateAsync(item, cancellationToken)
			: await AddAsync(item, cancellationToken);
	}

	#endregion

	#region Update

	public async ValueTask<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		if (BulkLoad && BulkInitialized())
		{
			var id = GetCacheId(entity);

			var isNew = false;

			var (sync, dict) = CachedEntities;

			using (await sync.WriterLockAsync(cancellationToken))
			{
				if (dict.TryAdd(id, entity))
				{
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
			_count = (int)(await OnGetCount(default, cancellationToken));
		else
			_count++;
	}

	#endregion

	protected abstract ValueTask<bool> IsSaved(TEntity item, CancellationToken cancellationToken);

	#region BaseListEx<E> Members

	public ValueTask<int> CountAsync(CancellationToken cancellationToken)
		=> CountAsync(false, cancellationToken);

	public async ValueTask<int> CountAsync(bool deleted, CancellationToken cancellationToken)
	{
		if (BulkLoad)
		{
			if (!BulkInitialized())
				await GetRangeAsync(0, long.MaxValue, deleted, default, default, cancellationToken);

			var (sync, dict) = CachedEntities;

			using var _ = await sync.ReaderLockAsync(cancellationToken);
			return dict.Count;
		}
		else
		{
			if (CacheCount)
			{
				_count ??= (int)await OnGetCount(deleted, cancellationToken);

				return _count.Value;
			}
			else
				return (int)await OnGetCount(deleted, cancellationToken);
		}
	}

	public async ValueTask<TEntity> AddAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		if (item is null)
			throw new ArgumentNullException(nameof(item));

		item = await OnAdd(item, cancellationToken);

		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntities;

				using var _ = await sync.WriterLockAsync(cancellationToken);
				dict.Add(GetCacheId(item), item);
			}
			else
				await GetRangeAsync(cancellationToken);
		}

		await IncrementCount(cancellationToken);

		return item;
	}

	public async ValueTask ClearAsync(CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		await OnClear(cancellationToken);

		if (BulkLoad)
		{
			await GetRangeAsync(cancellationToken);

			var (sync, dict) = CachedEntities;

			using var _ = await sync.WriterLockAsync(cancellationToken);
			dict.Clear();
		}

		_count = 0;
	}

	public abstract ValueTask<bool> ContainsAsync(TEntity item, CancellationToken cancellationToken);

	public async ValueTask CopyToAsync(TEntity[] array, int index, CancellationToken cancellationToken)
		=> ((ICollection<TEntity>)await GetRangeAsync(index, long.MaxValue, false, default, default, cancellationToken)).CopyTo(array, index);

	public async ValueTask<bool> RemoveAsync(TEntity item, CancellationToken cancellationToken)
	{
		if (IsReadOnly)
			throw new ReadOnlyException();

		var retVal = await OnRemove(item, cancellationToken);

		if (BulkLoad)
		{
			if (BulkInitialized())
			{
				var (sync, dict) = CachedEntities;

				using var _ = await sync.WriterLockAsync(cancellationToken);
				dict.Remove(GetCacheId(item));
			}
		}

		if (_count != null)
			_count--;

		return retVal;
	}

	private ValueTask<IEnumerable<TEntity>> GetRangeAsync(CancellationToken cancellationToken)
		=> GetRangeAsync(0, long.MaxValue, false, default, default, cancellationToken);

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

		if (!deleted && BulkLoad)
		{
			if (BulkInitialized())
			{
				IEnumerable<TEntity> source;

				var (sync, dict) = CachedEntities;

				using (sync.ReaderLock())
					source = dict.Values.ToArray();

				if (orderByColumn is not null)
				{
					object KeySelector(TEntity entity)
					{
						if (orderByColumn == Meta.Identity?.Name)
							return ((IDbPersistable)entity).GetIdentity();

						var s = new SettingsStorage();
						((IDbPersistable)entity).Save(s);
						s.TryGetValue(orderByColumn, out var v);
						return v;
					}

					source = direction == ListSortDirection.Ascending ? source.OrderBy(KeySelector) : source.OrderByDescending(KeySelector);
				}

				return ApplySkipTake(source, startIndex, count);
			}
			else
			{
				startIndex = 0;
				count = long.MaxValue;
			}
		}

		List<TEntity> entities = [];

		while (count > 0)
		{
			var pageSize = count.Min(BufferSize);

			var buffer = await OnGetGroup(startIndex, pageSize, deleted, orderByColumn, direction, cancellationToken);

			entities.AddRange(buffer);

			if (buffer.Length < pageSize)
				break;

			count -= pageSize;
			startIndex += pageSize;
		}

		if (!deleted && BulkLoad)
		{
			if (!BulkInitialized())
			{
				var (sync, dict) = CachedEntities;

				using var _ = await sync.WriterLockAsync(cancellationToken);

				dict.Clear();

				foreach (var entity in entities)
					dict.Add(GetCacheId(entity), entity);

				_bulkInitialized = true;

				if (CacheTimeOut < TimeSpan.MaxValue)
					_cacheExpire = DateTime.UtcNow + CacheTimeOut;

				return ApplySkipTake(entities, oldStartIndex, oldCount);
			}
		}

		return entities;
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

	IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(CancellationToken cancellationToken)
		=> new RelationManyListEnumerator(this, cancellationToken);

	#endregion

	#region Virtual CRUD Methods

	protected virtual ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
		=> Storage.GetCountAsync<TEntity>(cancellationToken);

	protected virtual ValueTask<TEntity> OnAdd(TEntity entity, CancellationToken cancellationToken)
		=> Storage.AddAsync(entity, cancellationToken);

	protected virtual ValueTask<TEntity[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		=> Storage.GetGroupAsync<TEntity>(startIndex, count, deleted, orderBy, direction, cancellationToken);

	protected virtual ValueTask<TEntity> OnUpdate(TEntity entity, CancellationToken cancellationToken)
		=> Storage.UpdateAsync(entity, cancellationToken);

	protected virtual ValueTask<bool> OnRemove(TEntity entity, CancellationToken cancellationToken)
		=> Storage.RemoveAsync(entity, cancellationToken);

	protected virtual ValueTask OnClear(CancellationToken cancellationToken)
		=> Storage.ClearAsync<TEntity>(cancellationToken);

	#endregion

	public async ValueTask<bool> RemoveById(TId id, CancellationToken cancellationToken)
		=> await RemoveAsync(await ReadById(id, cancellationToken), cancellationToken);

	private static TId GetCacheId(TEntity entity)
	{
		if (Meta.Identity is null)
			return entity.To<TId>();

		return ((IDbPersistable)entity).GetIdentity().To<TId>();
	}

	public async ValueTask<bool> TryAddAsync(TEntity entity, CancellationToken cancellationToken)
	{
		if (await ContainsAsync(entity, cancellationToken))
			return false;

		await AddAsync(entity, cancellationToken);
		return true;
	}

	async ValueTask<IQueryable<TEntity>> IRelationManyList<TEntity>.TryInitBulkLoad(CancellationToken cancellationToken)
	{
		if (!BulkLoad || BulkInitialized())
			return default;

		return (await GetRangeAsync(cancellationToken)).AsQueryable();
	}
}
