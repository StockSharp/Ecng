namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class StatTests : BaseTestClass
{
	private enum TestAction
	{
		Action1,
		Action2,
		Action3,
	}

	/// <summary>
	/// Verifies that <see cref="Stat{TAction}.GetInfo"/> returns the
	/// <c>Longest</c> array sorted by duration. Action durations are
	/// separated by an order of magnitude so CPU jitter cannot reorder
	/// adjacent samples; the loop bails out the moment a sample reflects
	/// the expected ordering, so a clean run finishes in one pass.
	/// </summary>
	[TestMethod]
	public async Task Longest_ShouldReturnActualLongestDurations()
	{
		const int maxAttempts = 3;

		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			var stat = new Stat<TestAction> { LongestLimit = 10 };
			var ip = IPAddress.Loopback;

			using (stat.Begin(TestAction.Action1, ip))
				await Task.Delay(TimeSpan.FromMilliseconds(200), CancellationToken);

			using (stat.Begin(TestAction.Action2, ip))
				await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken);

			using (stat.Begin(TestAction.Action3, ip))
				await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);

			var info = stat.GetInfo(0, 10);

			var ordered =
				info.Longest.Length == 3 &&
				info.Longest[0].Action.Equals(TestAction.Action2) &&
				info.Longest[1].Action.Equals(TestAction.Action3) &&
				info.Longest[2].Action.Equals(TestAction.Action1);

			if (ordered)
				return;

			if (attempt == maxAttempts)
				Assert.Fail($"Longest order wrong after {maxAttempts} attempts: " +
					$"[{string.Join(", ", info.Longest.Select(i => $"{i.Action}={i.Value.TotalMilliseconds:F0}ms"))}]");
		}
	}
}
