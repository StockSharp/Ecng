#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;
using System.Linq.Expressions;

using Ecng.Serialization;

/// <summary>
/// Test subclass of RelationManyList that allows controlling OnUpdate behavior.
/// </summary>
public class TestRelationManyList : RelationManyList<TestItem, long>
{
	private readonly bool _failOnUpdate;

	public TestRelationManyList(IStorage storage, bool failOnUpdate = false)
		: base(storage)
	{
		_failOnUpdate = failOnUpdate;
	}

	protected override ValueTask<TestItem> OnUpdate(TestItem entity, CancellationToken cancellationToken)
	{
		if (_failOnUpdate)
			throw new InvalidOperationException("Storage update failed");

		return new(entity);
	}

	protected override ValueTask<TestItem> OnAdd(TestItem entity, CancellationToken cancellationToken)
		=> new(entity);

	protected override ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
		=> new(0L);

	protected override ValueTask<TestItem[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
		=> new(Array.Empty<TestItem>());

	public override ValueTask<bool> ContainsAsync(TestItem item, CancellationToken cancellationToken)
		=> new(false);

	protected override ValueTask<bool> IsSaved(TestItem item, CancellationToken cancellationToken)
		=> new(false);

	/// <summary>
	/// Gets the internal cache dictionary for test assertions.
	/// </summary>
	public async ValueTask<Dictionary<long, TestItem>> GetCacheForTest(CancellationToken ct)
	{
		// Force bulk initialization by reading range
		await GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, ct);

		var field = typeof(RelationManyList<TestItem, long>)
			.GetField("_cachedEntities", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return (Dictionary<long, TestItem>)field.GetValue(this);
	}
}

/// <summary>
/// Minimal IStorage stub for unit tests.
/// </summary>
public class NullStorage : IStorage
{
	public Ecng.ComponentModel.Stat<string> Stat => default;
	public IStorageTransaction CreateTransaction() => throw new NotSupportedException();
	public void AddBulkLoad<TEntity>() where TEntity : IDbPersistable { }
	public ValueTask AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask ClearCacheAsync(CancellationToken ct) => default;
	public ValueTask<long> GetCountAsync<TEntity>(CancellationToken ct) where TEntity : IDbPersistable => new(0L);
	public ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(entity);
	public ValueTask<TEntity> GetByAsync<TEntity>(IQueryable<TEntity> expression, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask<TEntity> GetByIdAsync<TId, TEntity>(TId id, CancellationToken ct) where TEntity : IDbPersistable => default;
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
}

#endif
