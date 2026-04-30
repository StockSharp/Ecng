#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;
using System.Data;
using System.Linq.Expressions;

using Ecng.Serialization;

/// <summary>
/// Test subclass of RelationManyList that allows controlling OnUpdate behavior
/// and tracking call counts for the protected hooks.
/// </summary>
public class TestRelationManyList : RelationManyList<TestItem, long>
{
	private readonly bool _failOnUpdate;

	public TestRelationManyList(IStorage storage, bool failOnUpdate = false)
		: base(storage)
	{
		_failOnUpdate = failOnUpdate;
	}

	/// <summary>
	/// Counts how many times <see cref="OnAdd"/> has been invoked.
	/// </summary>
	public int OnAddCalls;

	/// <summary>
	/// Counts how many times <see cref="OnUpdate"/> has been invoked.
	/// </summary>
	public int OnUpdateCalls;

	/// <summary>
	/// Counts how many times <see cref="OnRemove"/> has been invoked.
	/// </summary>
	public int OnRemoveCalls;

	/// <summary>
	/// Counts how many times <see cref="OnGetCount"/> has been invoked.
	/// </summary>
	public int OnGetCountCalls;

	/// <summary>
	/// Counts how many times <see cref="OnGetGroup"/> has been invoked.
	/// Uses <see cref="Interlocked.Increment(ref int)"/> for safe concurrent access.
	/// </summary>
	public int OnGetGroupCalls;

	/// <summary>
	/// Counts how many times <see cref="OnClear"/> has been invoked.
	/// </summary>
	public int OnClearCalls;

	/// <summary>
	/// Counts how many times <see cref="ContainsAsync"/> has been invoked.
	/// </summary>
	public int ContainsCalls;

	/// <summary>
	/// Configurable result for <see cref="ContainsAsync"/>.
	/// </summary>
	public bool ContainsResult { get; set; }

	/// <summary>
	/// Configurable result for <see cref="IsSaved"/>.
	/// </summary>
	public bool IsSavedResult { get; set; }

	/// <summary>
	/// Configurable count returned by <see cref="OnGetCount"/>.
	/// </summary>
	public long GetCountResult { get; set; }

	/// <summary>
	/// Configurable items returned by <see cref="OnGetGroup"/>. When non-null,
	/// the items array is paged using the requested startIndex / count window.
	/// When null, an empty array is returned (the original test behaviour).
	/// </summary>
	public TestItem[] GroupItems { get; set; }

	/// <summary>
	/// Optional artificial delay applied inside <see cref="OnGetGroup"/> to
	/// widen the race window when exercising concurrent bulk initialisation.
	/// </summary>
	public TimeSpan GroupDelay { get; set; }

	protected override async ValueTask<TestItem> OnUpdate(TestItem entity, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnUpdateCalls);

		if (_failOnUpdate)
			throw new InvalidOperationException("Storage update failed");

		await Task.Yield();
		return entity;
	}

	protected override ValueTask<TestItem> OnAdd(TestItem entity, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnAddCalls);
		return new(entity);
	}

	protected override ValueTask<bool> OnRemove(TestItem entity, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnRemoveCalls);
		return new(true);
	}

	protected override ValueTask OnClear(CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnClearCalls);
		return default;
	}

	protected override ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnGetCountCalls);
		return new(GetCountResult);
	}

	protected override async ValueTask<TestItem[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref OnGetGroupCalls);

		if (GroupDelay > TimeSpan.Zero)
			await Task.Delay(GroupDelay, cancellationToken);

		if (GroupItems is null)
			return [];

		var source = GroupItems.AsEnumerable();

		if (startIndex > 0)
			source = source.Skip((int)startIndex);

		if (count != long.MaxValue && count < int.MaxValue)
			source = source.Take((int)count);

		return source.ToArray();
	}

	public override ValueTask<bool> ContainsAsync(TestItem item, CancellationToken cancellationToken)
	{
		Interlocked.Increment(ref ContainsCalls);
		return new(ContainsResult);
	}

	protected override ValueTask<bool> IsSaved(TestItem item, CancellationToken cancellationToken)
		=> new(IsSavedResult);

	/// <summary>
	/// Force bulk initialisation by reading the full range and return the
	/// resulting cache snapshot.
	/// </summary>
	public async ValueTask<IReadOnlyDictionary<long, TestItem>> GetCacheForTest(CancellationToken ct)
	{
		await GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, ct);
		return CachedEntities;
	}

	/// <summary>
	/// Allows tests to flip the read-only flag without subclassing further.
	/// </summary>
	public void SetReadOnly(bool value) => IsReadOnly = value;
}

/// <summary>
/// Minimal IStorage stub for unit tests. Captures whatever entity the
/// production <see cref="RelationManyList{TEntity,TId}"/> requests through
/// <see cref="GetByIdAsync"/> so <see cref="RelationManyList{TEntity,TId}.RemoveById"/>
/// can be exercised without a real backing store.
/// </summary>
public class NullStorage : IStorage
{
	/// <summary>
	/// Optional entity returned by <see cref="GetByIdAsync"/> regardless of id.
	/// </summary>
	public IDbPersistable GetByIdResult { get; set; }

	public Ecng.ComponentModel.Stat<string> Stat => default;
	public IStorageTransaction CreateTransaction() => throw new NotSupportedException();
	public void AddBulkLoad<TEntity>() where TEntity : IDbPersistable { }
	public ValueTask AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask ClearCacheAsync(CancellationToken ct) => default;
	public ValueTask<long> GetCountAsync<TEntity>(CancellationToken ct) where TEntity : IDbPersistable => new(0L);
	public ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(entity);
	public ValueTask<TEntity> GetByAsync<TEntity>(IQueryable<TEntity> expression, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask<TEntity> GetByIdAsync<TId, TEntity>(TId id, CancellationToken ct) where TEntity : IDbPersistable
		=> new((TEntity)GetByIdResult);
	public ValueTask<TEntity[]> GetByIdsAsync<TId, TEntity>(IEnumerable<TId> ids, CancellationToken ct) where TEntity : IDbPersistable => new(Array.Empty<TEntity>());
	public ValueTask<TEntity[]> GetGroupAsync<TEntity>(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken ct) where TEntity : IDbPersistable => new(Array.Empty<TEntity>());
	public ValueTask<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(entity);
	public ValueTask<bool> RemoveAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(true);
	public ValueTask ClearAsync<TEntity>(CancellationToken ct) where TEntity : IDbPersistable => default;
	public IEnumerable<TResult> ExecuteEnum<TSource, TResult>(Expression expression) => [];
	public IAsyncEnumerable<TResult> ExecuteEnumAsync<TSource, TResult>(Expression expression) => AsyncEnumerable.Empty<TResult>();
	public ValueTask ExecuteAsync<TSource>(Expression expression) => default;
	public TResult ExecuteResult<TSource, TResult>(Expression expression) => default;
	public ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression) => default;
}

[TestClass]
public class RelationManyListTests : BaseTestClass
{
	[TestMethod]
	public async Task UpdateAsync_BulkLoad_FailedStorage_CacheShouldNotContainEntity()
	{
		// Finding #6: UpdateAsync adds to cache BEFORE calling OnUpdate.
		// If OnUpdate fails, the entity remains in cache — phantom entry.
		var storage = new NullStorage();
		var list = new TestRelationManyList(storage, failOnUpdate: true) { BulkLoad = true };

		var cache = await list.GetCacheForTest(CancellationToken);
		cache.Count.AssertEqual(0);

		var entity = new TestItem { Id = 1, Name = "Test" };

		try
		{
			await list.UpdateAsync(entity, CancellationToken);
		}
		catch (InvalidOperationException)
		{
			// expected — storage failed
		}

		// After failed update, entity should NOT be in cache
		cache.ContainsKey(1L).AssertFalse(
			"Failed UpdateAsync should not leave phantom entity in cache");
	}

	[TestMethod]
	public async Task AddAsync_BulkLoad_StorageFirst_ThenCache()
	{
		// Contrast: AddAsync correctly calls OnAdd BEFORE mutating cache.
		var storage = new NullStorage();
		var list = new TestRelationManyList(storage) { BulkLoad = true };

		var cache = await list.GetCacheForTest(CancellationToken);

		var entity = new TestItem { Id = 2, Name = "Added" };
		await list.AddAsync(entity, CancellationToken);

		cache.ContainsKey(2L).AssertTrue(
			"Successful AddAsync should put entity in cache");
	}

	[TestMethod]
	public async Task AddAsync_NonBulk_DoesNotPopulateCache()
	{
		var list = new TestRelationManyList(new NullStorage());

		var entity = new TestItem { Id = 10, Name = "Added" };
		var result = await list.AddAsync(entity, CancellationToken);

		result.Id.AssertEqual(10L);
		list.OnAddCalls.AssertEqual(1);
		// Non-bulk mode never instantiates the cache dictionary.
		(list.CachedEntities is null || list.CachedEntities.Count == 0).AssertTrue();
	}

	[TestMethod]
	public async Task AddAsync_BulkLoad_AddsEntryAndIncrementsCount()
	{
		var list = new TestRelationManyList(new NullStorage()) { BulkLoad = true };

		// Force bulk initialisation with an empty backing store.
		await list.GetCacheForTest(CancellationToken);

		await list.AddAsync(new TestItem { Id = 3, Name = "X" }, CancellationToken);
		await list.AddAsync(new TestItem { Id = 4, Name = "Y" }, CancellationToken);

		list.CachedEntities.Count.AssertEqual(2);
		(await list.CountAsync(CancellationToken)).AssertEqual(2);
	}

	[TestMethod]
	public async Task AddAsync_ReadOnly_Throws()
	{
		var list = new TestRelationManyList(new NullStorage());
		list.SetReadOnly(true);

		await ThrowsExactlyAsync<ReadOnlyException>(async () =>
			await list.AddAsync(new TestItem { Id = 1 }, CancellationToken));
	}

	[TestMethod]
	public async Task RemoveAsync_BulkLoad_RemovesFromCacheAndDecrementsCount()
	{
		var list = new TestRelationManyList(new NullStorage()) { BulkLoad = true };

		await list.GetCacheForTest(CancellationToken);

		var entity = new TestItem { Id = 5, Name = "Z" };
		await list.AddAsync(entity, CancellationToken);
		list.CachedEntities.Count.AssertEqual(1);
		(await list.CountAsync(CancellationToken)).AssertEqual(1);

		var removed = await list.RemoveAsync(entity, CancellationToken);

		removed.AssertTrue();
		list.CachedEntities.ContainsKey(5L).AssertFalse();
		(await list.CountAsync(CancellationToken)).AssertEqual(0);
	}

	[TestMethod]
	public async Task RemoveAsync_NonBulk_NoCacheMutation()
	{
		var list = new TestRelationManyList(new NullStorage());
		var result = await list.RemoveAsync(new TestItem { Id = 1 }, CancellationToken);
		result.AssertTrue();
		list.OnRemoveCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task RemoveAsync_BulkLoad_NotInCache_ReturnsStorageResult()
	{
		var list = new TestRelationManyList(new NullStorage()) { BulkLoad = true };

		await list.GetCacheForTest(CancellationToken);

		// Removing an entity that is not in the cache must not throw.
		var ghost = new TestItem { Id = 99 };
		var result = await list.RemoveAsync(ghost, CancellationToken);

		result.AssertTrue();
		list.CachedEntities.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task RemoveAsync_ReadOnly_Throws()
	{
		var list = new TestRelationManyList(new NullStorage());
		list.SetReadOnly(true);

		await ThrowsExactlyAsync<ReadOnlyException>(async () =>
			await list.RemoveAsync(new TestItem { Id = 1 }, CancellationToken));
	}

	[TestMethod]
	public async Task UpdateAsync_NonBulk_NoCacheInteraction()
	{
		var list = new TestRelationManyList(new NullStorage());

		var result = await list.UpdateAsync(new TestItem { Id = 7, Name = "U" }, CancellationToken);

		result.Id.AssertEqual(7L);
		list.OnUpdateCalls.AssertEqual(1);
		(list.CachedEntities is null || list.CachedEntities.Count == 0).AssertTrue();
	}

	[TestMethod]
	public async Task UpdateAsync_BulkLoad_ExistingEntry_ReplacedNotDuplicated()
	{
		var list = new TestRelationManyList(new NullStorage()) { BulkLoad = true };

		await list.GetCacheForTest(CancellationToken);

		var first = new TestItem { Id = 8, Name = "first" };
		await list.AddAsync(first, CancellationToken);
		list.CachedEntities.Count.AssertEqual(1);

		var updated = new TestItem { Id = 8, Name = "second" };
		await list.UpdateAsync(updated, CancellationToken);

		// Same id -> single entry, original instance preserved by TryAdd semantics.
		list.CachedEntities.Count.AssertEqual(1);
		list.CachedEntities[8L].Name.AssertEqual("first");
	}

	[TestMethod]
	public async Task UpdateAsync_ReadOnly_Throws()
	{
		var list = new TestRelationManyList(new NullStorage());
		list.SetReadOnly(true);

		await ThrowsExactlyAsync<ReadOnlyException>(async () =>
			await list.UpdateAsync(new TestItem { Id = 1 }, CancellationToken));
	}

	[TestMethod]
	public async Task ResetCache_NextRead_RepopulatesFromStorage()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			GroupItems = [new TestItem { Id = 1, Name = "A" }, new TestItem { Id = 2, Name = "B" }],
		};

		var first = await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken);
		first.Count().AssertEqual(2);
		list.OnGetGroupCalls.AssertEqual(1);

		list.ResetCache();
		list.CachedEntities.Count.AssertEqual(0);

		var second = await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken);
		second.Count().AssertEqual(2);
		// After reset bulk init must have replayed the storage call.
		list.OnGetGroupCalls.AssertEqual(2);
	}

	[TestMethod]
	public async Task CacheTimeOut_Expires_TriggersReload()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			CacheTimeOut = TimeSpan.FromMilliseconds(50),
			GroupItems = [new TestItem { Id = 1, Name = "A" }],
		};

		await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken);
		list.OnGetGroupCalls.AssertEqual(1);

		// Sleep past the cache TTL.
		await Task.Delay(150, CancellationToken);

		await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken);
		list.OnGetGroupCalls.AssertEqual(2);
	}

	[TestMethod]
	public async Task ClearAsync_BulkLoad_EmptiesCacheAndResetsCount()
	{
		var list = new TestRelationManyList(new NullStorage()) { BulkLoad = true };

		await list.GetCacheForTest(CancellationToken);
		await list.AddAsync(new TestItem { Id = 1 }, CancellationToken);
		await list.AddAsync(new TestItem { Id = 2 }, CancellationToken);
		list.CachedEntities.Count.AssertEqual(2);

		await list.ClearAsync(CancellationToken);

		list.OnClearCalls.AssertEqual(1);
		list.CachedEntities.Count.AssertEqual(0);
		(await list.CountAsync(CancellationToken)).AssertEqual(0);
	}

	[TestMethod]
	public async Task ClearAsync_ReadOnly_Throws()
	{
		var list = new TestRelationManyList(new NullStorage());
		list.SetReadOnly(true);

		await ThrowsExactlyAsync<ReadOnlyException>(async () =>
			await list.ClearAsync(CancellationToken));
	}

	[TestMethod]
	public async Task TryAddAsync_AlreadyContains_ReturnsFalseAndSkipsAdd()
	{
		var list = new TestRelationManyList(new NullStorage()) { ContainsResult = true };

		var added = await list.TryAddAsync(new TestItem { Id = 1 }, CancellationToken);

		added.AssertFalse();
		list.ContainsCalls.AssertEqual(1);
		list.OnAddCalls.AssertEqual(0);
	}

	[TestMethod]
	public async Task TryAddAsync_NotContained_AddsAndReturnsTrue()
	{
		var list = new TestRelationManyList(new NullStorage()) { ContainsResult = false };

		var added = await list.TryAddAsync(new TestItem { Id = 1 }, CancellationToken);

		added.AssertTrue();
		list.ContainsCalls.AssertEqual(1);
		list.OnAddCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task SaveAsync_NewItem_RoutedToOnAdd()
	{
		var list = new TestRelationManyList(new NullStorage()) { IsSavedResult = false };

		await list.SaveAsync(new TestItem { Id = 1 }, CancellationToken);

		list.OnAddCalls.AssertEqual(1);
		list.OnUpdateCalls.AssertEqual(0);
	}

	[TestMethod]
	public async Task SaveAsync_ExistingItem_RoutedToOnUpdate()
	{
		var list = new TestRelationManyList(new NullStorage()) { IsSavedResult = true };

		await list.SaveAsync(new TestItem { Id = 1 }, CancellationToken);

		list.OnUpdateCalls.AssertEqual(1);
		list.OnAddCalls.AssertEqual(0);
	}

	[TestMethod]
	public async Task CountAsync_NonBulk_CacheCountTrue_CachesAfterFirstCall()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			CacheCount = true,
			GetCountResult = 7,
		};

		(await list.CountAsync(CancellationToken)).AssertEqual(7);
		(await list.CountAsync(CancellationToken)).AssertEqual(7);
		(await list.CountAsync(CancellationToken)).AssertEqual(7);

		list.OnGetCountCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task CountAsync_NonBulk_CacheCountFalse_HitsStorageEachTime()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			CacheCount = false,
			GetCountResult = 3,
		};

		(await list.CountAsync(CancellationToken)).AssertEqual(3);
		(await list.CountAsync(CancellationToken)).AssertEqual(3);
		(await list.CountAsync(CancellationToken)).AssertEqual(3);

		list.OnGetCountCalls.AssertEqual(3);
	}

	[TestMethod]
	public async Task CountAsync_BulkLoad_ReturnsCacheDictionarySize()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			GroupItems =
			[
				new TestItem { Id = 1 },
				new TestItem { Id = 2 },
				new TestItem { Id = 3 },
			],
		};

		(await list.CountAsync(CancellationToken)).AssertEqual(3);
	}

	[TestMethod]
	public async Task GetRangeAsync_NonBulk_PagesAcrossBufferSize()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BufferSize = 2,
			GroupItems =
			[
				new TestItem { Id = 1 },
				new TestItem { Id = 2 },
				new TestItem { Id = 3 },
				new TestItem { Id = 4 },
				new TestItem { Id = 5 },
			],
		};

		var range = (await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken)).ToArray();

		range.Length.AssertEqual(5);
		range.Select(i => i.Id).SequenceEqual([1L, 2L, 3L, 4L, 5L]).AssertTrue();
		// Pages of 2: full(2) + full(2) + partial(1) -> three calls, the partial
		// page terminates the loop.
		list.OnGetGroupCalls.AssertEqual(3);
	}

	[TestMethod]
	public async Task GetRangeAsync_BulkLoad_CachesAndReusesOnSecondCall()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			GroupItems =
			[
				new TestItem { Id = 1 },
				new TestItem { Id = 2 },
				new TestItem { Id = 3 },
			],
		};

		var first = (await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken)).ToArray();
		first.Length.AssertEqual(3);
		list.OnGetGroupCalls.AssertEqual(1);

		var second = (await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken)).ToArray();
		second.Length.AssertEqual(3);
		// Bulk-loaded list must serve the second range from the cache.
		list.OnGetGroupCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task RemoveById_LoadsThenRemoves()
	{
		var entity = new TestItem { Id = 42, Name = "ToRemove" };
		var storage = new NullStorage { GetByIdResult = entity };
		var list = new TestRelationManyList(storage);

		var removed = await list.RemoveById(42L, CancellationToken);

		removed.AssertTrue();
		list.OnRemoveCalls.AssertEqual(1);
	}

	[TestMethod]
	public void BufferSize_NonPositive_Throws()
	{
		var list = new TestRelationManyList(new NullStorage());

		ThrowsExactly<ArgumentOutOfRangeException>(() => list.BufferSize = 0);
		ThrowsExactly<ArgumentOutOfRangeException>(() => list.BufferSize = -1);
	}

	[TestMethod]
	public void Meta_ResolvesRegisteredSchema()
	{
		var meta = RelationManyList<TestItem, long>.Meta;
		meta.AssertNotNull();
		meta.Identity.AssertNotNull();
		meta.Identity.Name.AssertEqual(nameof(TestItem.Id));
	}

	[TestMethod]
	public async Task BulkLoad_Concurrent_GetRangeAsync_InitializesOnce()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			GroupDelay = TimeSpan.FromMilliseconds(50),
			GroupItems =
			[
				new TestItem { Id = 1, Name = "A" },
				new TestItem { Id = 2, Name = "B" },
				new TestItem { Id = 3, Name = "C" },
			],
		};

		const int parallelism = 20;
		var tasks = new Task<TestItem[]>[parallelism];

		for (var i = 0; i < parallelism; i++)
		{
			tasks[i] = Task.Run(async () =>
				(await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken)).ToArray(),
				CancellationToken);
		}

		var results = await Task.WhenAll(tasks);

		// All callers see the same set of entities.
		foreach (var result in results)
		{
			result.Length.AssertEqual(3);
			result.Select(i => i.Id).OrderBy(id => id).SequenceEqual([1L, 2L, 3L]).AssertTrue();
		}

		// Bulk initialisation must collapse N concurrent reads into a single
		// storage round-trip.
		list.OnGetGroupCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task IAsyncEnumerable_IteratesAllEntitiesAcrossPages()
	{
		var items = Enumerable
			.Range(1, 7)
			.Select(i => new TestItem { Id = i, Name = $"item-{i}" })
			.ToArray();

		var list = new TestRelationManyList(new NullStorage())
		{
			BufferSize = 3,
			GroupItems = items,
		};

		IAsyncEnumerable<TestItem> source = list;

		var collected = new List<long>();

		await foreach (var entity in source.WithCancellation(CancellationToken))
			collected.Add(entity.Id);

		collected.Count.AssertEqual(7);
		collected.SequenceEqual([1L, 2L, 3L, 4L, 5L, 6L, 7L]).AssertTrue();
	}
}

#endif
