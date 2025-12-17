#if NET10_0_OR_GREATER == false
namespace Ecng.Tests.Linq;

using System.Runtime.CompilerServices;

using Ecng.Linq;

[TestClass]
public class AsyncEnumerableTests : BaseTestClass
{
	private static readonly TimeSpan _1mls = TimeSpan.FromMilliseconds(1);

	private static async IAsyncEnumerable<int> GetAsyncData([EnumeratorCancellation]CancellationToken cancellationToken)
	{
		for (int i = 1; i <= 3; i++)
		{
			await _1mls.Delay(cancellationToken);
			yield return i;
		}
	}

	[TestMethod]
	public async Task ToArrayAsync()
	{
		var token = CancellationToken;
		var arr = await GetAsyncData(token).ToArrayAsync(token);
		arr.Length.AssertEqual(3);
		arr[0].AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstAsync()
	{
		var token = CancellationToken;
		var first = await GetAsyncData(token).FirstAsync(token);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync()
	{
		var token = CancellationToken;
		var first = await GetAsyncData(token).FirstOrDefaultAsync(token);
		first.AssertEqual(1);
	}

	private class RefItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private async IAsyncEnumerable<RefItem> GetAsyncRefData()
	{
		var token = CancellationToken;
		await _1mls.Delay(token);
		yield return new RefItem { Id = 10, Name = "a" };
		await _1mls.Delay(token);
		yield return new RefItem { Id = 11, Name = "b" };
	}

	[TestMethod]
	public async Task ToArrayAsync2_RefType()
	{
		var token = CancellationToken;
		var arr = await GetAsyncRefData().ToArrayAsync(token);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task FirstAsync2_RefType()
	{
		var token = CancellationToken;
		var first = await GetAsyncRefData().FirstAsync(token);
		(first?.Id).AssertEqual(10);
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync2_RefType_Empty()
	{
		static async IAsyncEnumerable<RefItem> Empty()
		{
			await Task.CompletedTask;
			yield break;
		}

		var token = CancellationToken;
		var first = await Empty().FirstOrDefaultAsync(token);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_Ints()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 3 };
		var arr = await source.ToAsyncEnumerable().WithEnforcedCancellation(token).ToArrayAsync(token);
		arr.AssertEqual(source);
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_RefType()
	{
		var token = CancellationToken;
		var source = new[] { new RefItem { Id = 1, Name = "a" }, new RefItem { Id = 2, Name = "b" } };
		var list = new List<RefItem>();

		await foreach (var item in source.ToAsyncEnumerable().WithEnforcedCancellation(token))
			list.Add(item);

		list.Count.AssertEqual(2);
		list[0].Id.AssertEqual(1);
		list[1].Name.AssertEqual("b");
	}

	[TestMethod]
	public Task ToAsyncEnumerable2_Cancel()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		var asyncEnu = new[] { 1 }.ToAsyncEnumerable().WithEnforcedCancellation(cts.Token);
		return ThrowsExactlyAsync<OperationCanceledException>(async () => { await foreach (var _ in asyncEnu) { } });
	}

	[TestMethod]
	public async Task GroupByAsync()
	{
		var source = new[]
		{
			new RefItem { Id = 1, Name = "a" },
			new RefItem { Id = 1, Name = "a2" },
			new RefItem { Id = 2, Name = "b" },
			new RefItem { Id = 2, Name = "b2" },
			new RefItem { Id = 2, Name = "b3" },
		};

		var groups = new List<IGrouping<int, RefItem>>();

#pragma warning disable CS0618 // Type or member is obsolete
		await foreach (var group in source.ToAsyncEnumerable().GroupByAsync(x => x.Id, CancellationToken))
#pragma warning restore CS0618 // Type or member is obsolete
		{
			groups.Add(group);
		}

		groups.Count.AssertEqual(2);
		groups[0].Key.AssertEqual(1);
		groups[0].Count().AssertEqual(2);
		groups[1].Key.AssertEqual(2);
		groups[1].Count().AssertEqual(3);
	}

	[TestMethod]
	public async Task Empty()
	{
		var token = CancellationToken;
		var empty = AsyncEnumerable.Empty<int>();
		var arr = await empty.ToArrayAsync(token);
		arr.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task Empty2_RefType()
	{
		var token = CancellationToken;
		var empty = AsyncEnumerable.Empty<RefItem>();
		var first = await empty.FirstOrDefaultAsync(token);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_List()
	{
		var token = CancellationToken;
		var source = new List<int> { 1, 2, 3 };
		var arr = await source.ToAsyncEnumerable().WithEnforcedCancellation(token).ToArrayAsync(token);
		arr.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_IList()
	{
		var token = CancellationToken;
		IList<int> source = [10, 20, 30];
		var arr = await source.ToAsyncEnumerable().WithEnforcedCancellation(token).ToArrayAsync(token);
		arr.AssertEqual([10, 20, 30]);
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_EmptyArray()
	{
		var token = CancellationToken;
		var source = Array.Empty<int>();
		var arr = await source.ToAsyncEnumerable().WithEnforcedCancellation(token).ToArrayAsync(token);
		arr.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_Enumerable()
	{
		var token = CancellationToken;
		IEnumerable<int> source = Enumerable.Range(1, 5);
		var arr = await source.ToAsyncEnumerable().WithEnforcedCancellation(token).ToArrayAsync(token);
		arr.AssertEqual([1, 2, 3, 4, 5]);
	}

	[TestMethod]
	public Task FirstAsync2_ThrowsOnEmpty()
	{
		var token = CancellationToken;
		var empty = AsyncEnumerable.Empty<int>();
		return ThrowsExactlyAsync<InvalidOperationException>(() => empty.FirstAsync(token).AsTask());
	}

	[TestMethod]
	public async Task ToListAsync()
	{
		var token = CancellationToken;
		var list = await GetAsyncData(token).ToListAsync(token);
		list.Count.AssertEqual(3);
		list[0].AssertEqual(1);
		list[2].AssertEqual(3);
	}

	[TestMethod]
	public async Task ToDictionaryAsync()
	{
		var source = new[] { new RefItem { Id = 1, Name = "a" }, new RefItem { Id = 2, Name = "b" } };
		var dict = await source.ToAsyncEnumerable().ToDictionaryAsync(x => x.Id, CancellationToken);
		dict.Count.AssertEqual(2);
		dict[1].Name.AssertEqual("a");
		dict[2].Name.AssertEqual("b");
	}

	[TestMethod]
	public async Task ToHashSetAsync()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 2, 3, 3, 3 };
		var set = await source.ToAsyncEnumerable().ToHashSetAsync(token);
		set.Count.AssertEqual(3);
		set.Contains(1).AssertTrue();
		set.Contains(2).AssertTrue();
		set.Contains(3).AssertTrue();
	}

	[TestMethod]
	public async Task LastAsync()
	{
		var token = CancellationToken;
		var last = await GetAsyncData(token).LastAsync(token);
		last.AssertEqual(3);
	}

	[TestMethod]
	public async Task LastOrDefaultAsync()
	{
		var token = CancellationToken;
		var last = await GetAsyncData(token).LastOrDefaultAsync(token);
		last.AssertEqual(3);

		var emptyLast = await AsyncEnumerable.Empty<int>().LastOrDefaultAsync(token);
		emptyLast.AssertEqual(0);
	}

	[TestMethod]
	public async Task SingleAsync()
	{
		var token = CancellationToken;
		var source = new[] { 42 };
		var single = await source.ToAsyncEnumerable().SingleAsync(token);
		single.AssertEqual(42);
	}

	[TestMethod]
	public Task SingleAsync2_ThrowsOnEmpty()
	{
		var token = CancellationToken;
		return ThrowsExactlyAsync<InvalidOperationException>(() =>
			AsyncEnumerable.Empty<int>().SingleAsync(token).AsTask());
	}

	[TestMethod]
	public Task SingleAsync2_ThrowsOnMultiple()
	{
		var token = CancellationToken;
		
		return ThrowsExactlyAsync<InvalidOperationException>(() =>
			GetAsyncData(token).SingleAsync(token).AsTask());
	}

	[TestMethod]
	public async Task ElementAtAsync()
	{
		var token = CancellationToken;
		var elem = await GetAsyncData(token).ElementAtAsync(1, CancellationToken);
		elem.AssertEqual(2);
	}

	[TestMethod]
	public async Task ElementAtOrDefaultAsync()
	{
		var token = CancellationToken;
		var elem = await GetAsyncData(token).ElementAtOrDefaultAsync(10, CancellationToken);
		elem.AssertEqual(0);
	}

	[TestMethod]
	public async Task AnyAsync()
	{
		var token = CancellationToken;
		var hasAny = await GetAsyncData(token).AnyAsync(token);
		hasAny.AssertTrue();

		var emptyHasAny = await AsyncEnumerable.Empty<int>().AnyAsync(token);
		emptyHasAny.AssertFalse();
	}

	[TestMethod]
	public async Task AnyAsync2_WithPredicate()
	{
		var token = CancellationToken;
		var hasGreaterThan2 = await GetAsyncData(token).AnyAsync(x => x > 2, CancellationToken);
		hasGreaterThan2.AssertTrue();

		var hasGreaterThan10 = await GetAsyncData(token).AnyAsync(x => x > 10, CancellationToken);
		hasGreaterThan10.AssertFalse();
	}

	[TestMethod]
	public async Task AllAsync()
	{
		var token = CancellationToken;
		var allPositive = await GetAsyncData(token).AllAsync(x => x > 0, CancellationToken);
		allPositive.AssertTrue();

		var allGreaterThan2 = await GetAsyncData(token).AllAsync(x => x > 2, CancellationToken);
		allGreaterThan2.AssertFalse();
	}

	[TestMethod]
	public async Task ContainsAsync()
	{
		var token = CancellationToken;
		var contains2 = await GetAsyncData(token).ContainsAsync(2, CancellationToken);
		contains2.AssertTrue();

		var contains10 = await GetAsyncData(token).ContainsAsync(10, CancellationToken);
		contains10.AssertFalse();
	}

	[TestMethod]
	public async Task CountAsync()
	{
		var token = CancellationToken;
		var count = await GetAsyncData(token).CountAsync(token);
		count.AssertEqual(3);
	}

	[TestMethod]
	public async Task LongCountAsync()
	{
		var token = CancellationToken;
		var count = await GetAsyncData(token).LongCountAsync(token);
		count.AssertEqual(3L);
	}

	[TestMethod]
	public async Task SumAsync()
	{
		var token = CancellationToken;
		var sum = await GetAsyncData(token).SumAsync(token);
		sum.AssertEqual(6);
	}

	[TestMethod]
	public async Task AverageAsync()
	{
		var token = CancellationToken;
		var avg = await GetAsyncData(token).AverageAsync(token);
		avg.AssertEqual(2.0);
	}

	[TestMethod]
	public async Task MinAsync()
	{
		var token = CancellationToken;
		var min = await GetAsyncData(token).MinAsync(token);
		min.AssertEqual(1);
	}

	[TestMethod]
	public async Task MaxAsync()
	{
		var token = CancellationToken;
		var max = await GetAsyncData(token).MaxAsync(token);
		max.AssertEqual(3);
	}

	[TestMethod]
	public async Task Where()
	{
		var token = CancellationToken;
		var filtered = await GetAsyncData(token).Where(x => x > 1, token).ToArrayAsync(token);
		filtered.Length.AssertEqual(2);
		filtered[0].AssertEqual(2);
		filtered[1].AssertEqual(3);
	}

	[TestMethod]
	public async Task Select()
	{
		var token = CancellationToken;
		var doubled = await GetAsyncData(token).Select(x => x * 2).ToArrayAsync(token);
		doubled.Length.AssertEqual(3);
		doubled[0].AssertEqual(2);
		doubled[1].AssertEqual(4);
		doubled[2].AssertEqual(6);
	}

	[TestMethod]
	public async Task Skip()
	{
		var token = CancellationToken;
		var skipped = await GetAsyncData(token).Skip(1, token).ToArrayAsync(token);
		skipped.Length.AssertEqual(2);
		skipped[0].AssertEqual(2);
		skipped[1].AssertEqual(3);
	}

	[TestMethod]
	public async Task Take()
	{
		var token = CancellationToken;
		var taken = await GetAsyncData(token).Take(2, token).ToArrayAsync(token);
		taken.Length.AssertEqual(2);
		taken[0].AssertEqual(1);
		taken[1].AssertEqual(2);
	}

	[TestMethod]
	public async Task Distinct()
	{
		var token = CancellationToken;
		var source = new[] { 1, 2, 2, 3, 3, 3 };
		var distinct = await source.ToAsyncEnumerable().Distinct(token).ToArrayAsync(token);
		distinct.Length.AssertEqual(3);
		distinct.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public async Task Concat()
	{
		var token = CancellationToken;
		var first = new[] { 1, 2 }.ToAsyncEnumerable();
		var second = new[] { 3, 4 }.ToAsyncEnumerable();
		var concatenated = await first.Concat(second, token).ToArrayAsync(token);
		concatenated.AssertEqual([1, 2, 3, 4]);
	}

	[TestMethod]
	public async Task Append()
	{
		var token = CancellationToken;
		var appended = await GetAsyncData(token).Append(4, token).ToArrayAsync(token);
		appended.Length.AssertEqual(4);
		appended[3].AssertEqual(4);
	}

	[TestMethod]
	public async Task Prepend()
	{
		var token = CancellationToken;
		var prepended = await GetAsyncData(token).Prepend(0, token).ToArrayAsync(token);
		prepended.Length.AssertEqual(4);
		prepended[0].AssertEqual(0);
		prepended[1].AssertEqual(1);
	}

	[TestMethod]
	public async Task Reverse()
	{
		var token = CancellationToken;
		var reversed = await GetAsyncData(token).Reverse(token).ToArrayAsync(token);
		reversed.Length.AssertEqual(3);
		reversed[0].AssertEqual(3);
		reversed[1].AssertEqual(2);
		reversed[2].AssertEqual(1);
	}

	[TestMethod]
	public async Task WhereSelectChain()
	{
		var token = CancellationToken;
		var result = await GetAsyncData(token)
			.Where(x => x > 1, token)
			.Select(x => x * 10)
			.ToListAsync(token);

		result.Count.AssertEqual(2);
		result[0].AssertEqual(20);
		result[1].AssertEqual(30);
	}
}
#endif