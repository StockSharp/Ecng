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
	/// Verifies that Stat.Longest returns entries sorted by duration (longest first).
	/// Uses sampling to handle non-deterministic timing.
	/// </summary>
	[TestMethod]
	public async Task Longest_ShouldReturnActualLongestDurations()
	{
		// Run multiple samples to ensure consistent behavior
		const int sampleCount = 5;
		var successCount = 0;

		for (var sample = 0; sample < sampleCount; sample++)
		{
			var stat = new Stat<TestAction> { LongestLimit = 10 };
			var ip = IPAddress.Loopback;

			// Use larger time differences to minimize timing variance impact
			// Short action (~20ms)
			using (stat.Begin(TestAction.Action1, ip))
				await Task.Delay(20);

			// Long action (~200ms)
			using (stat.Begin(TestAction.Action2, ip))
				await Task.Delay(200);

			// Medium action (~80ms)
			using (stat.Begin(TestAction.Action3, ip))
				await Task.Delay(80);

			var info = stat.GetInfo(0, 10);

			// Verify entries are sorted by duration (longest first)
			var isOrdered = info.Longest.Length >= 2 &&
				info.Longest[0].Value >= info.Longest[1].Value &&
				(info.Longest.Length < 3 || info.Longest[1].Value >= info.Longest[2].Value);

			if (isOrdered && info.Longest[0].Action == TestAction.Action2)
				successCount++;
		}

		// At least 3 out of 5 samples should succeed (60% threshold for flaky environments)
		(successCount >= 3).AssertTrue(
			$"Longest should be sorted by duration (longest first). Only {successCount}/{sampleCount} samples succeeded.");
	}
}
