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
}
