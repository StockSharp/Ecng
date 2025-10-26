namespace Ecng.Tests.Linq;

using System.Threading;

using Ecng.Linq;

[TestClass]
public class AsyncEnumerableExtensionsTests
{
	private static async IAsyncEnumerable<int> GetAsyncData()
	{
		for (int i = 1; i <= 3; i++)
		{
			await Task.Delay(1);
			yield return i;
		}
	}

	[TestMethod]
	public async Task ToArrayAsync2()
	{
		var arr = await GetAsyncData().ToArrayAsync2(CancellationToken.None);
		arr.Length.AssertEqual(3);
		arr[0].AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstAsync2()
	{
		var first = await GetAsyncData().FirstAsync2(CancellationToken.None);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync2()
	{
		var first = await GetAsyncData().FirstOrDefaultAsync2(CancellationToken.None);
		first.AssertEqual(1);
	}

	[TestMethod]
	public async Task GroupByAsync2()
	{
		static async IAsyncEnumerable<int> Source()
		{
			for (int i = 1; i <= 6; i++)
			{
				await Task.Delay(1);
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
		groups[0].ToArray().SequenceEqual([1]).AssertTrue();
		groups[1].Key.AssertEqual(0);
		groups[1].ToArray().SequenceEqual([2]).AssertTrue();
		groups[2].Key.AssertEqual(1);
		groups[2].ToArray().SequenceEqual([3]).AssertTrue();
		groups[3].Key.AssertEqual(0);
		groups[3].ToArray().SequenceEqual([4]).AssertTrue();
		groups[4].Key.AssertEqual(1);
		groups[4].ToArray().SequenceEqual([5]).AssertTrue();
		groups[5].Key.AssertEqual(0);
		groups[5].ToArray().SequenceEqual([6]).AssertTrue();
	}

	private class RefItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	private static async IAsyncEnumerable<RefItem> GetAsyncRefData()
	{
		await Task.Delay(1);
		yield return new RefItem { Id = 10, Name = "a" };
		await Task.Delay(1);
		yield return new RefItem { Id = 11, Name = "b" };
	}

	[TestMethod]
	public async Task ToArrayAsync2_RefType()
	{
		var arr = await GetAsyncRefData().ToArrayAsync2(CancellationToken.None);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task FirstAsync2_RefType()
	{
		var first = await GetAsyncRefData().FirstAsync2(CancellationToken.None);
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

		var first = await Empty().FirstOrDefaultAsync2(CancellationToken.None);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task GroupByAsync2_RefType()
	{
		static async IAsyncEnumerable<RefItem> Source()
		{
			await Task.Delay(1);
			yield return new RefItem { Id = 1, Name = "x" };
			await Task.Delay(1);
			yield return new RefItem { Id = 1, Name = "y" };
			await Task.Delay(1);
			yield return new RefItem { Id = 2, Name = "z" };
		}

		var groups = new List<IGrouping<int, RefItem>>();

		await foreach (var g in Source().GroupByAsync2(i => i.Id, CancellationToken.None))
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