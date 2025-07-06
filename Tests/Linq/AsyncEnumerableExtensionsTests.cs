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

		await foreach (var g in Source().GroupByAsync2(x => x % 2))
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
}
