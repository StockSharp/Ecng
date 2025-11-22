#if NET10_0_OR_GREATER == false
namespace Ecng.Tests.Linq;

using Ecng.Linq;

[TestClass]
public class AsyncEnumerableTests : BaseTestClass
{
	private static readonly TimeSpan _1mls = TimeSpan.FromMilliseconds(1);

	private async IAsyncEnumerable<int> GetAsyncData()
	{
		for (int i = 1; i <= 3; i++)
		{
			await _1mls.Delay(CancellationToken);
			yield return i;
		}
	}

	[TestMethod]
	public async Task ToArrayAsync2()
	{
		var arr = await GetAsyncData().ToArrayAsync(CancellationToken);
		arr.Length.AssertEqual(3);
		arr[0].AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstAsync2()
	{
		var first = await GetAsyncData().FirstAsync(CancellationToken);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync2()
	{
		var first = await GetAsyncData().FirstOrDefaultAsync(CancellationToken);
		first.AssertEqual(1);
	}

	private class RefItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private async IAsyncEnumerable<RefItem> GetAsyncRefData()
	{
		await _1mls.Delay(CancellationToken);
		yield return new RefItem { Id = 10, Name = "a" };
		await _1mls.Delay(CancellationToken);
		yield return new RefItem { Id = 11, Name = "b" };
	}

	[TestMethod]
	public async Task ToArrayAsync2_RefType()
	{
		var arr = await GetAsyncRefData().ToArrayAsync(CancellationToken);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task FirstAsync2_RefType()
	{
		var first = await GetAsyncRefData().FirstAsync(CancellationToken);
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

		var first = await Empty().FirstOrDefaultAsync(CancellationToken);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_Ints()
	{
		var source = new[] { 1, 2, 3 };
		var arr = await source.ToAsyncEnumerable(CancellationToken).ToArrayAsync(CancellationToken);
		arr.AssertEqual(source);
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_RefType()
	{
		var source = new[] { new RefItem { Id = 1, Name = "a" }, new RefItem { Id = 2, Name = "b" } };
		var list = new List<RefItem>();

		await foreach (var item in source.ToAsyncEnumerable(CancellationToken))
			list.Add(item);

		list.Count.AssertEqual(2);
		list[0].Id.AssertEqual(1);
		list[1].Name.AssertEqual("b");
	}

	[TestMethod]
	public async Task ToAsyncEnumerable2_Cancel()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		var asyncEnu = new[] { 1 }.ToAsyncEnumerable(cts.Token);
		await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => { await foreach (var _ in asyncEnu) { } });
	}
}
#endif