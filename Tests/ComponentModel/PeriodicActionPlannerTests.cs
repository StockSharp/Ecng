namespace Ecng.Tests.ComponentModel;

using Ecng.ComponentModel;

[TestClass]
public class PeriodicActionPlannerTests : BaseTestClass
{
	[TestMethod]
	public void MaxErrors_RemovesActionAfterThreshold()
	{
		var planner = new PeriodicActionPlanner { MaxErrors = 2 };

		var calls = 0;
		using var sub = planner.Register(() =>
		{
			calls++;
			throw new InvalidOperationException("boom");
		}, TimeSpan.FromMilliseconds(1));

		var now = DateTime.UtcNow.AddSeconds(1);

		for (var i = 0; i < 2; i++)
		{
			var due = planner.GetDueActions(now);
			due.Length.AssertEqual(1, $"Expected one due action on iteration {i}.");

			try
			{
				due[0]();
			}
			catch (InvalidOperationException)
			{
				// expected
			}

			now = now.AddSeconds(1);
		}

		calls.AssertEqual(2, "Expected exactly two calls before auto-removal.");
		planner.MinInterval.AssertNull("Expected planner to be empty after reaching MaxErrors.");

		planner.GetDueActions(now).Length.AssertEqual(0, "Expected no actions after auto-removal.");
	}

	[TestMethod]
	public void MaxErrors_ResetsAfterSuccess()
	{
		var planner = new PeriodicActionPlanner { MaxErrors = 2 };

		var call = 0;
		using var sub = planner.Register(() =>
		{
			call++;
			if (call == 1 || call == 3)
				throw new InvalidOperationException("boom");
		}, TimeSpan.FromMilliseconds(1));

		var now = DateTime.UtcNow.AddSeconds(1);

		// 1st call throws -> errors=1
		var due1 = planner.GetDueActions(now);
		due1.Length.AssertEqual(1);
		try { due1[0](); } catch (InvalidOperationException) { }
		planner.MinInterval.AssertNotNull("Expected action to remain registered after first error.");

		// 2nd call succeeds -> errors reset to 0
		now = now.AddSeconds(1);
		var due2 = planner.GetDueActions(now);
		due2.Length.AssertEqual(1);
		due2[0]();
		planner.MinInterval.AssertNotNull("Expected action to remain registered after success.");

		// 3rd call throws again -> errors=1 (not 2)
		now = now.AddSeconds(1);
		var due3 = planner.GetDueActions(now);
		due3.Length.AssertEqual(1);
		try { due3[0](); } catch (InvalidOperationException) { }
		planner.MinInterval.AssertNotNull("Expected action to remain registered (errors not consecutive).");
	}
	[TestMethod]
	public void Register_NullAction_Throws()
	{
		var planner = new PeriodicActionPlanner();
		ThrowsExactly<ArgumentNullException>(() => planner.Register(null, TimeSpan.FromSeconds(1)));
	}

	[TestMethod]
	public void Register_InvalidInterval_Throws()
	{
		var planner = new PeriodicActionPlanner();
		ThrowsExactly<ArgumentOutOfRangeException>(() => planner.Register(() => { }, TimeSpan.Zero));
		ThrowsExactly<ArgumentOutOfRangeException>(() => planner.Register(() => { }, TimeSpan.FromMilliseconds(-1)));
	}

	[TestMethod]
	public void EmptyPlanner_MinIntervalIsNull_AndNoDueActions()
	{
		var planner = new PeriodicActionPlanner();
		planner.MinInterval.AssertNull();
		planner.GetDueActions(DateTime.UtcNow).Length.AssertEqual(0);
	}

	[TestMethod]
	public void Scheduling_BasicAdvanceAndNoEarlyRun()
	{
		var planner = new PeriodicActionPlanner();
		var calls = 0;

		using var sub = planner.Register(() => calls++, TimeSpan.FromSeconds(10));

		var t0 = DateTime.UtcNow;
		planner.GetDueActions(t0).Length.AssertEqual(0, "Should not be due immediately for long interval.");

		var t1 = t0.AddSeconds(11);
		var due1 = planner.GetDueActions(t1);
		due1.Length.AssertEqual(1, "Expected the action to become due.");
		due1[0]();
		calls.AssertEqual(1);

		// Next run is scheduled relative to t1 (no drift accumulation).
		var t2 = t1.AddSeconds(1);
		planner.GetDueActions(t2).Length.AssertEqual(0, "Should not be due before next interval.");

		var t3 = t1.AddSeconds(11);
		var due3 = planner.GetDueActions(t3);
		due3.Length.AssertEqual(1, "Expected the action to become due again after interval.");
		due3[0]();
		calls.AssertEqual(2);
	}

	[TestMethod]
	public void MinInterval_TracksAndUpdatesOnUnsubscribe()
	{
		var planner = new PeriodicActionPlanner();

		var sub200 = planner.Register(() => { }, TimeSpan.FromMilliseconds(200));
		planner.MinInterval.AssertNotNull();
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(200));

		var sub50 = planner.Register(() => { }, TimeSpan.FromMilliseconds(50));
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(50));

		var sub500 = planner.Register(() => { }, TimeSpan.FromMilliseconds(500));
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(50));

		sub50.Dispose();
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(200), "Expected MinInterval to increase after removing fastest.");

		sub200.Dispose();
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(500));

		sub500.Dispose();
		planner.MinInterval.AssertNull();
	}

	[TestMethod]
	public void SameActionRegisteredTwice_DisposeOneKeepsOther()
	{
		var planner = new PeriodicActionPlanner();

		var calls = 0;
		void action() => calls++;

		using var fast = planner.Register(action, TimeSpan.FromMilliseconds(10));
		var slow = planner.Register(action, TimeSpan.FromMilliseconds(100));
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(10));

		slow.Dispose();
		planner.MinInterval.Value.AssertEqual(TimeSpan.FromMilliseconds(10), "Fast registration should remain.");

		var due = planner.GetDueActions(DateTime.UtcNow.AddSeconds(1));
		(due.Length >= 1).AssertTrue("Expected at least one due action.");
		due[0]();
		calls.AssertEqual(1);
	}

	[TestMethod]
	public void MaxErrors_Disabled_DoesNotAutoRemove()
	{
		var planner = new PeriodicActionPlanner { MaxErrors = 0 };

		var calls = 0;
		using var sub = planner.Register(() =>
		{
			calls++;
			throw new InvalidOperationException("boom");
		}, TimeSpan.FromMilliseconds(1));

		var now = DateTime.UtcNow.AddSeconds(1);

		for (var i = 0; i < 5; i++)
		{
			var due = planner.GetDueActions(now);
			due.Length.AssertEqual(1);

			try { due[0](); } catch (InvalidOperationException) { }

			planner.MinInterval.AssertNotNull("Expected action to remain registered when MaxErrors is disabled.");
			now = now.AddSeconds(1);
		}

		calls.AssertEqual(5);
	}
}


