namespace Ecng.Tests.Linq;

using Ecng.Linq;

[TestClass]
public class AsyncEnumerableExtensionsTests : BaseTestClass
{
	private static async IAsyncEnumerable<int> GetAsyncData()
	{
		for (int i = 1; i <= 3; i++)
		{
			await Task.Delay(1, CancellationToken.None);
			yield return i;
		}
	}

	[TestMethod]
	public async Task ToArrayAsync2()
	{
		var arr = await GetAsyncData().ToArrayAsync2(CancellationToken);
		arr.Length.AssertEqual(3);
		arr[0].AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstAsync2()
	{
		var first = await GetAsyncData().FirstAsync2(CancellationToken);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync2()
	{
		var first = await GetAsyncData().FirstOrDefaultAsync2(CancellationToken);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task GroupByAsync2()
	{
		static async IAsyncEnumerable<int> Source()
		{
			for (int i = 1; i <= 6; i++)
			{
				await Task.Delay(1, CancellationToken.None);
				yield return i;
			}
		}

		var groups = new List<IGrouping<int, int>>();

		await foreach (var g in Source().GroupByAsync2(x => x % 2, default))
		{
			groups.Add(g);
		}

		groups.Count.AssertEqual(6);
		groups[0].Key.AssertEqual(1 % 2);
		groups[0].AssertEqual(new int[] { 1 });
		groups[1].Key.AssertEqual(0);
		groups[1].AssertEqual(new int[] { 2 });
		groups[2].Key.AssertEqual(1);
		groups[2].AssertEqual(new int[] { 3 });
		groups[3].Key.AssertEqual(0);
		groups[3].AssertEqual(new int[] { 4 });
		groups[4].Key.AssertEqual(1);
		groups[4].AssertEqual(new int[] { 5 });
		groups[5].Key.AssertEqual(0);
		groups[5].AssertEqual(new int[] { 6 });
	}

	private class RefItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private static async IAsyncEnumerable<RefItem> GetAsyncRefData()
	{
		await Task.Delay(1, CancellationToken.None);
		yield return new RefItem { Id = 10, Name = "a" };
		await Task.Delay(1, CancellationToken.None);
		yield return new RefItem { Id = 11, Name = "b" };
	}

	[TestMethod]
	public async Task ToArrayAsync2_RefType()
	{
		var arr = await GetAsyncRefData().ToArrayAsync2(CancellationToken);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task FirstAsync2_RefType()
	{
		var first = await GetAsyncRefData().FirstAsync2(CancellationToken);
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

		var first = await Empty().FirstOrDefaultAsync2(CancellationToken);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task GroupByAsync2_RefType()
	{
		static async IAsyncEnumerable<RefItem> Source()
		{
			await Task.Delay(1, CancellationToken.None);
			yield return new RefItem { Id = 1, Name = "x" };
			await Task.Delay(1, CancellationToken.None);
			yield return new RefItem { Id = 1, Name = "y" };
			await Task.Delay(1, CancellationToken.None);
			yield return new RefItem { Id = 2, Name = "z" };
		}

		var groups = new List<IGrouping<int, RefItem>>();

		await foreach (var g in Source().GroupByAsync2(i => i.Id, CancellationToken))
		{
			groups.Add(g);
		}

		groups.Count.AssertEqual(2);
		groups[0].Key.AssertEqual(1);
		groups[0].Count().AssertEqual(2);
		groups[1].Key.AssertEqual(2);
		groups[1].Count().AssertEqual(1);
	}
}