namespace Ecng.Tests.ComponentModel;

using System.Net;

using Ecng.ComponentModel;

[TestClass]
public class StatTests : BaseTestClass
{
	private enum TestAction
	{
		Action1,
		Action2,
		Action3
	}

	/// <summary>
	/// Verifies that Stat.Longest returns actual longest durations, not shortest.
	/// </summary>
	[TestMethod]
	public void Longest_ShouldReturnActualLongestDurations()
	{
		var stat = new Stat<TestAction> { LongestLimit = 10 };
		var ip = IPAddress.Loopback;

		// Simulate actions with different durations
		// We'll use Thread.Sleep to create measurable differences

		// Short action (~10ms)
		using (stat.Begin(TestAction.Action1, ip))
			Thread.Sleep(10);

		// Long action (~100ms)
		using (stat.Begin(TestAction.Action2, ip))
			Thread.Sleep(100);

		// Medium action (~50ms)
		using (stat.Begin(TestAction.Action3, ip))
			Thread.Sleep(50);

		// Get statistics
		var info = stat.GetInfo(0, 10);

		// Should have 3 longest entries
		info.Longest.Length.AssertEqual(3, "Should have 3 entries in Longest");

		// The first entry should be the longest (Action2 ~100ms)
		// If bug exists, it will be the shortest (Action1 ~10ms)
		var firstLongest = info.Longest[0];
		firstLongest.Action.AssertEqual(TestAction.Action2,
			$"First Longest should be Action2 (longest), but got {firstLongest.Action} with {firstLongest.Value.TotalMilliseconds}ms");

		// Verify order: longest first
		(info.Longest[0].Value >= info.Longest[1].Value).AssertTrue(
			$"Longest[0] ({info.Longest[0].Value.TotalMilliseconds}ms) should be >= Longest[1] ({info.Longest[1].Value.TotalMilliseconds}ms)");
		(info.Longest[1].Value >= info.Longest[2].Value).AssertTrue(
			$"Longest[1] ({info.Longest[1].Value.TotalMilliseconds}ms) should be >= Longest[2] ({info.Longest[2].Value.TotalMilliseconds}ms)");
	}
}
