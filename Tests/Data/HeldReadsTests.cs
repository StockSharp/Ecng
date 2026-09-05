#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;
using System.Linq.Expressions;

using Ecng.Serialization;

/// <summary>
/// A store that refuses to be read synchronously, and counts the times it is read at all.
/// </summary>
/// <remarks>
/// A blocking read returns the right answer and only costs a parked thread, so it cannot be caught after the
/// fact. Refusing it turns that into a failure a test cannot pass through.
/// </remarks>
public class RecordingStorage : IStorage
{
	/// <summary>How many times the store was asked for rows, the allowed way.</summary>
	public int AsyncReads;

	public IAsyncEnumerable<TResult> ExecuteEnumAsync<TSource, TResult>(Expression expression)
	{
		Interlocked.Increment(ref AsyncReads);
		return AsyncEnumerable.Empty<TResult>();
	}

	public ValueTask<TResult> ExecuteResultAsync<TSource, TResult>(Expression expression)
	{
		Interlocked.Increment(ref AsyncReads);
		return default;
	}

	public Ecng.ComponentModel.Stat<string> Stat => default;
	public IStorageTransaction CreateTransaction() => throw new NotSupportedException();
	public void AddBulkLoad<TEntity>() where TEntity : IDbPersistable { }
	public ValueTask AddCacheAsync<TId, TEntity>(TId id, TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask ClearCacheAsync(CancellationToken ct) => default;
	public ValueTask<long> GetCountAsync<TEntity>(CancellationToken ct) where TEntity : IDbPersistable => new(0L);
	public ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(entity);
	public ValueTask<TEntity> GetByAsync<TEntity>(IQueryable<TEntity> expression, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask<TEntity> GetByIdAsync<TId, TEntity>(TId id, CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask<TEntity[]> GetByIdsAsync<TId, TEntity>(IEnumerable<TId> ids, CancellationToken ct) where TEntity : IDbPersistable => new([]);
	public ValueTask<TEntity[]> GetGroupAsync<TEntity>(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken ct) where TEntity : IDbPersistable => new([]);
	public ValueTask<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(entity);
	public ValueTask<bool> RemoveAsync<TEntity>(TEntity entity, CancellationToken ct) where TEntity : IDbPersistable => new(true);
	public ValueTask ClearAsync<TEntity>(CancellationToken ct) where TEntity : IDbPersistable => default;
	public ValueTask ExecuteAsync<TSource>(Expression expression) => default;
}

/// <summary>
/// What a query over a table kept in memory owes its caller: the right rows, without blocking the thread, and
/// without going back to the store once the table is held.
/// </summary>
[TestClass]
public class HeldReadsTests : BaseTestClass
{
	// The provider dispatches through reflection, so a refusal arrives wrapped.
	private static bool RefusedAsSynchronous(Action read)
	{
		try
		{
			read();
			return false;
		}
		catch (Exception error)
		{
			for (var e = error; e is not null; e = e.InnerException)
			{
				if (e.Message.Contains("on the calling thread"))
					return true;
			}

			throw;
		}
	}

	private static TestRelationManyList Held(RecordingStorage store, params string[] names)
		=> new(store)
		{
			BulkLoad = true,
			GroupItems = [.. names.Select((n, i) => new TestItem { Id = i + 1, Name = n })],
		};

	[TestMethod]
	public async Task AColdTableIsReadWithoutBlocking()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		var found = await list.ToQueryable().Where(i => i.Name == "Kraken").ToArrayAsyncEx(CancellationToken);

		AreEqual(1, found.Length);
		AreEqual("Kraken", found[0].Name);
	}

	[TestMethod]
	public async Task AHeldTableIsNotReadAgain()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		await list.PreloadAsync(CancellationToken);
		var readsAfterWarmUp = list.OnGetGroupCalls;

		await list.ToQueryable().Where(i => i.Id > 0).ToArrayAsyncEx(CancellationToken);

		AreEqual(readsAfterWarmUp, list.OnGetGroupCalls);
		AreEqual(0, store.AsyncReads);
	}

	/// <summary>The answer comes from what is held, which is what makes holding it worth anything.</summary>
	[TestMethod]
	public async Task AHeldTableAnswersFromWhatItHolds()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		await list.PreloadAsync(CancellationToken);
		var readsAfterWarmUp = list.OnGetGroupCalls;

		// The store now says something else. A held table must not notice.
		list.GroupItems = [new() { Id = 9, Name = "Changed" }];

		var found = await list.ToQueryable().ToArrayAsyncEx(CancellationToken);

		AreEqual(2, found.Length);
		IsFalse(found.Any(i => i.Name == "Changed"), "the query went back to the store");
		AreEqual(readsAfterWarmUp, list.OnGetGroupCalls);
	}

	[TestMethod]
	public async Task ATableNamedBySomebodyElsesQueryIsNotBlockedOn()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		var outside = new[] { 2L }.AsQueryable();

		var found = await outside
			.Join(list.ToQueryable(), id => id, i => i.Id, (_, i) => i.Name)
			.ToArrayAsyncEx(CancellationToken);

		AreEqual("Kraken", found.Single());
	}

	[TestMethod]
	public async Task ATableNamedBySubQueryIsNotBlockedOn()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		var outside = new[] { 1L, 5L }.AsQueryable();

		var found = await outside
			.Where(id => list.ToQueryable().Any(i => i.Id == id))
			.ToArrayAsyncEx(CancellationToken);

		AreEqual(1L, found.Single());
	}

	[TestMethod]
	public async Task CountingDoesNotBlockEither()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		AreEqual(2, await list.ToQueryable().CountAsyncEx(CancellationToken));
	}

	[TestMethod]
	public async Task TakingTheFirstDoesNotBlockEither()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		var first = await list.ToQueryable().OrderBy(i => i.Id).FirstOrDefaultAsyncEx(CancellationToken);

		AreEqual("Binance", first.Name);
	}

	/// <summary>Asked for synchronously before it is loaded, a held table is still a reference to the store.</summary>
	[TestMethod]
	public void AColdHeldTableHandedOverSynchronouslyStillPointsAtTheStore()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		IsTrue(RefusedAsSynchronous(() => list.ToQueryable().ToArray()), "a cold table was read without complaint");
	}

	/// <summary>Asked for the other way, it is loaded first and handed over as memory.</summary>
	[TestMethod]
	public async Task AHeldTableIsHandedOverAsMemory()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");

		var held = await list.ToQueryableAsync(CancellationToken);

		// Reading it plainly is the point: there is nothing left to ask anyone for.
		var found = held.Where(i => i.Name == "Kraken").ToArray();

		AreEqual(1, found.Length);
		AreEqual("Kraken", found[0].Name);
		AreEqual(0, store.AsyncReads);
	}

	/// <summary>A table nobody asked to keep stays the store's, which is what makes a large table workable.</summary>
	[TestMethod]
	public async Task ATableNotKeptInMemoryIsStillTheStores()
	{
		var store = new RecordingStorage();
		var list = new TestRelationManyList(store) { BulkLoad = false };

		var query = await list.ToQueryableAsync(CancellationToken);

		IsTrue(RefusedAsSynchronous(() => query.ToArray()), "a table that is not kept was read without complaint");
	}

	/// <summary>A crowd arriving on a cold table reads it once between them, not once each.</summary>
	[TestMethod]
	public async Task ManyCallersOnAColdTableReadItOnce()
	{
		var store = new RecordingStorage();
		var list = Held(store, "Binance", "Kraken");
		list.GroupDelay = TimeSpan.FromMilliseconds(50);

		await Task.WhenAll([.. Enumerable.Range(0, 8).Select(_ =>
			list.ToQueryable().Where(i => i.Id > 0).ToArrayAsyncEx(CancellationToken).AsTask())]);

		// One pass over the table is a read per page plus the short page that ends it.
		IsTrue(list.OnGetGroupCalls <= 2, $"the table was read {list.OnGetGroupCalls} times");
	}
}

#endif
