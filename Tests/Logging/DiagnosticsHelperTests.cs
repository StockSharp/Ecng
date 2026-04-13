namespace Ecng.Tests.Logging;

using System.Collections.Concurrent;

using Ecng.Logging;

[TestClass]
public class DiagnosticsHelperTests : BaseTestClass
{
	/// <summary>
	/// Captures warning messages for assertions.
	/// </summary>
	private sealed class TestReceiver : BaseLogReceiver
	{
		public ConcurrentQueue<string> Warnings { get; } = new();
		public ConcurrentQueue<string> Infos { get; } = new();

		protected override void RaiseLog(LogMessage message)
		{
			var text = message.Message;
			if (message.Level == LogLevels.Warning)
				Warnings.Enqueue(text);
			else if (message.Level == LogLevels.Info)
				Infos.Enqueue(text);
		}
	}

	#region GrowthTracker

	[TestMethod]
	public void GrowthTracker_BelowFirstTier_NoTierCrossed()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("x", 50).AssertEqual(0L);
		tracker.CheckTier("x", 99).AssertEqual(0L);
	}

	[TestMethod]
	public void GrowthTracker_CrossFirstTier_ReturnsTier()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("x", 100).AssertEqual(100L);
	}

	[TestMethod]
	public void GrowthTracker_DoublingTiers_ReturnsCorrectly()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("x", 100).AssertEqual(100L);
		tracker.CheckTier("x", 150).AssertEqual(0L); // still below 200
		tracker.CheckTier("x", 200).AssertEqual(200L);
		tracker.CheckTier("x", 350).AssertEqual(0L); // still below 400
		tracker.CheckTier("x", 400).AssertEqual(400L);
	}

	[TestMethod]
	public void GrowthTracker_IndependentNames()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("a", 100).AssertEqual(100L);
		tracker.CheckTier("b", 50).AssertEqual(0L);
		tracker.CheckTier("b", 100).AssertEqual(100L);
	}

	[TestMethod]
	public void GrowthTracker_SkipsMultipleTiers()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		// jump from 0 straight to 500 — should return 100 (first tier)
		tracker.CheckTier("x", 500).AssertEqual(100L);
		// next call at 500 — should advance to 200
		tracker.CheckTier("x", 500).AssertEqual(200L);
		// next at 500 — should advance to 400
		tracker.CheckTier("x", 500).AssertEqual(400L);
		// next at 500 — below 800, no crossing
		tracker.CheckTier("x", 500).AssertEqual(0L);
	}

	[TestMethod]
	public void GrowthTracker_CustomMultiplier()
	{
		var tracker = new GrowthTracker(firstTier: 10, multiplier: 3);

		tracker.CheckTier("x", 10).AssertEqual(10L);
		tracker.CheckTier("x", 29).AssertEqual(0L);
		tracker.CheckTier("x", 30).AssertEqual(30L); // 10 * 3
		tracker.CheckTier("x", 89).AssertEqual(0L);
		tracker.CheckTier("x", 90).AssertEqual(90L); // 30 * 3
	}

	[TestMethod]
	public void GrowthTracker_Reset_AllowsRefire()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("x", 100).AssertEqual(100L);
		tracker.Reset("x");
		tracker.CheckTier("x", 100).AssertEqual(100L); // fires again after reset
	}

	[TestMethod]
	public void GrowthTracker_ResetAll_ClearsEverything()
	{
		var tracker = new GrowthTracker(firstTier: 100);

		tracker.CheckTier("a", 100).AssertEqual(100L);
		tracker.CheckTier("b", 100).AssertEqual(100L);

		tracker.ResetAll();

		tracker.CheckTier("a", 100).AssertEqual(100L);
		tracker.CheckTier("b", 100).AssertEqual(100L);
	}

	[TestMethod]
	public void GrowthTracker_InvalidArgs()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => new GrowthTracker(firstTier: 0));
		ThrowsExactly<ArgumentOutOfRangeException>(() => new GrowthTracker(firstTier: -1));
		ThrowsExactly<ArgumentOutOfRangeException>(() => new GrowthTracker(multiplier: 1));
	}

	#endregion

	#region LogGrowth (GrowthTracker overload)

	[TestMethod]
	public void LogGrowth_Tracker_EmitsWarningOnTierCross()
	{
		var log = new TestReceiver();
		var tracker = new GrowthTracker(firstTier: 100);

		log.LogGrowth(tracker, 50, "test");
		log.Warnings.Count.AssertEqual(0);

		log.LogGrowth(tracker, 100, "test");
		log.Warnings.Count.AssertEqual(1);
		log.Warnings.TryDequeue(out var msg).AssertTrue();
		msg.Contains("test").AssertTrue();
		msg.Contains("100").AssertTrue();
	}

	[TestMethod]
	public void LogGrowth_Tracker_NoSpamBelowNextTier()
	{
		var log = new TestReceiver();
		var tracker = new GrowthTracker(firstTier: 100);

		log.LogGrowth(tracker, 100, "x");
		log.Warnings.Count.AssertEqual(1);

		log.LogGrowth(tracker, 150, "x");
		log.Warnings.Count.AssertEqual(1); // no new warning
	}

	#endregion

	#region LogGrowth (ref long overload)

	[TestMethod]
	public void LogGrowth_RefLong_EmitsWarningOnTierCross()
	{
		var log = new TestReceiver();
		long wm = 0;

		log.LogGrowth(ref wm, 500, "cnt");
		log.Warnings.Count.AssertEqual(0);

		log.LogGrowth(ref wm, 1000, "cnt");
		log.Warnings.Count.AssertEqual(1);
		log.Warnings.TryDequeue(out var msg).AssertTrue();
		msg.Contains("cnt").AssertTrue();
		msg.Contains("1000").AssertTrue();
	}

	[TestMethod]
	public void LogGrowth_RefLong_DoublingBehavior()
	{
		var log = new TestReceiver();
		long wm = 0;

		log.LogGrowth(ref wm, 1000, "x");
		wm.AssertEqual(1000L);

		log.LogGrowth(ref wm, 1500, "x");
		wm.AssertEqual(1000L); // no advance

		log.LogGrowth(ref wm, 2000, "x");
		wm.AssertEqual(2000L);

		log.Warnings.Count.AssertEqual(2);
	}

	#endregion

	#region MemorySnapshot

	[TestMethod]
	public void MemorySnapshot_Capture_ReturnsSensibleValues()
	{
		var snap = MemorySnapshot.Capture();

		(snap.ManagedHeap > 0).AssertTrue("ManagedHeap should be > 0");
		(snap.WorkingSet > 0).AssertTrue("WorkingSet should be > 0");
		(snap.Gen0 >= 0).AssertTrue("Gen0 should be >= 0");
	}

	[TestMethod]
	public void MemorySnapshot_ToString_ContainsAllParts()
	{
		var snap = MemorySnapshot.Capture();
		var s = snap.ToString();

		s.Contains("MEM").AssertTrue($"Expected MEM prefix, got: {s}");
		s.Contains("heap=").AssertTrue($"Expected heap=, got: {s}");
		s.Contains("working=").AssertTrue($"Expected working=, got: {s}");
		s.Contains("GC[0]=").AssertTrue($"Expected GC[0]=, got: {s}");
		s.Contains("[1]=").AssertTrue($"Expected [1]=, got: {s}");
		s.Contains("[2]=").AssertTrue($"Expected [2]=, got: {s}");
	}

	[TestMethod]
	public void MemorySnapshot_FormatBytes_Units()
	{
		// test via ToString with known values
		var snap = new MemorySnapshot(
			ManagedHeap: 1536 * 1024L * 1024,  // 1.5 GB
			WorkingSet: 256 * 1024L * 1024,      // 256 MB
			Gen0: 10, Gen1: 3, Gen2: 1);

		var s = snap.ToString();
		s.Contains("1.5GB").AssertTrue($"Expected 1.5GB, got: {s}");
		s.Contains("256.0MB").AssertTrue($"Expected 256.0MB, got: {s}");
	}

	[TestMethod]
	public void LogMemorySnapshot_EmitsWarning()
	{
		var log = new TestReceiver();

		var snap = log.LogMemorySnapshot();

		(snap.ManagedHeap > 0).AssertTrue();
		log.Warnings.Count.AssertEqual(1);
		log.Warnings.TryDequeue(out var msg).AssertTrue();
		msg.Contains("MEM").AssertTrue($"Expected MEM prefix, got: {msg}");
	}

	[TestMethod]
	public void LogMemorySnapshotIfGrew_BelowThreshold_NoLog()
	{
		var log = new TestReceiver();
		var snap = MemorySnapshot.Capture();
		long lastHeap = snap.ManagedHeap;

		// threshold is huge, so no growth detected
		var result = log.LogMemorySnapshotIfGrew(ref lastHeap, threshold: long.MaxValue);

		result.AssertNull();
		log.Warnings.Count.AssertEqual(0);
	}

	[TestMethod]
	public void LogMemorySnapshotIfGrew_AboveThreshold_Logs()
	{
		var log = new TestReceiver();
		long lastHeap = 0; // start from 0, guaranteed to exceed any reasonable threshold

		var result = log.LogMemorySnapshotIfGrew(ref lastHeap, threshold: 1);

		result.AssertNotNull();
		log.Warnings.Count.AssertEqual(1);
		(lastHeap > 0).AssertTrue("lastHeap should be updated");
	}

	#endregion

	#region CollectionSizeLog

	[TestMethod]
	public void CollectionSizeLog_Snapshot_ReturnsAllTracked()
	{
		var sizeLog = new CollectionSizeLog();
		var list1 = new List<int> { 1, 2, 3 };
		var list2 = new List<int> { 1 };

		sizeLog.Track(() => list1.Count, "list1");
		sizeLog.Track(() => list2.Count, "list2");

		var snap = sizeLog.Snapshot();
		snap.Count.AssertEqual(2);

		// sorted by name
		snap[0].Name.AssertEqual("list1");
		snap[0].Size.AssertEqual(3);
		snap[1].Name.AssertEqual("list2");
		snap[1].Size.AssertEqual(1);
	}

	[TestMethod]
	public void CollectionSizeLog_SnapshotGrowing_DetectsGrowth()
	{
		var sizeLog = new CollectionSizeLog();
		var count = 10L;

		sizeLog.Track(() => count, "counter");

		// first snapshot establishes baseline
		sizeLog.Snapshot();

		// grow
		count = 20;
		var growing = sizeLog.SnapshotGrowing();
		growing.Count.AssertEqual(1);
		growing[0].Name.AssertEqual("counter");
		growing[0].Size.AssertEqual(20);
		growing[0].Prev.AssertEqual(10);
	}

	[TestMethod]
	public void CollectionSizeLog_SnapshotGrowing_NoGrowth_Empty()
	{
		var sizeLog = new CollectionSizeLog();
		var count = 10L;

		sizeLog.Track(() => count, "counter");
		sizeLog.Snapshot(); // baseline

		// no growth
		var growing = sizeLog.SnapshotGrowing();
		growing.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSizeLog_SnapshotGrowing_Shrink_NotReported()
	{
		var sizeLog = new CollectionSizeLog();
		var count = 100L;

		sizeLog.Track(() => count, "counter");
		sizeLog.Snapshot(); // baseline at 100

		count = 50; // shrink
		var growing = sizeLog.SnapshotGrowing();
		growing.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSizeLog_Untrack_RemovesSource()
	{
		var sizeLog = new CollectionSizeLog();
		sizeLog.Track(() => 42, "x");

		sizeLog.Untrack("x");

		var snap = sizeLog.Snapshot();
		snap.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSizeLog_Format_ProducesLogLine()
	{
		var items = new List<(string Name, long Size)>
		{
			("alpha", 100),
			("beta", 200),
		};

		var formatted = CollectionSizeLog.Format(items);
		formatted.AssertEqual("SIZES alpha=100 beta=200");
	}

	[TestMethod]
	public void CollectionSizeLog_Format_Empty()
	{
		var formatted = CollectionSizeLog.Format([]);
		formatted.AssertEqual("SIZES (empty)");
	}

	[TestMethod]
	public void CollectionSizeLog_FormatGrowing_ProducesLogLine()
	{
		var items = new List<(string Name, long Size, long Prev)>
		{
			("queue", 500, 200),
		};

		var formatted = CollectionSizeLog.FormatGrowing(items);
		formatted.AssertEqual("GROWING queue=500(+300)");
	}

	[TestMethod]
	public void CollectionSizeLog_FormatGrowing_Empty_ReturnsEmpty()
	{
		var formatted = CollectionSizeLog.FormatGrowing([]);
		formatted.AssertEqual(string.Empty);
	}

	#endregion

	#region LogSizes / LogGrowingSizes

	[TestMethod]
	public void LogSizes_EmitsInfoMessage()
	{
		var log = new TestReceiver();
		var sizeLog = new CollectionSizeLog();
		sizeLog.Track(() => 42, "things");

		log.LogSizes(sizeLog);

		log.Infos.Count.AssertEqual(1);
		log.Infos.TryDequeue(out var msg).AssertTrue();
		msg.Contains("things=42").AssertTrue($"Expected things=42, got: {msg}");
	}

	[TestMethod]
	public void LogGrowingSizes_WithGrowth_EmitsWarning()
	{
		var log = new TestReceiver();
		var sizeLog = new CollectionSizeLog();
		var count = 10L;
		sizeLog.Track(() => count, "items");
		sizeLog.Snapshot(); // baseline

		count = 50;
		var grew = log.LogGrowingSizes(sizeLog);

		grew.AssertTrue();
		log.Warnings.Count.AssertEqual(1);
		log.Warnings.TryDequeue(out var msg).AssertTrue();
		msg.Contains("items=50").AssertTrue($"Expected items=50, got: {msg}");
		msg.Contains("+40").AssertTrue($"Expected +40, got: {msg}");
	}

	[TestMethod]
	public void LogGrowingSizes_NoGrowth_NoLog()
	{
		var log = new TestReceiver();
		var sizeLog = new CollectionSizeLog();
		sizeLog.Track(() => 10, "stable");
		sizeLog.Snapshot(); // baseline

		var grew = log.LogGrowingSizes(sizeLog);

		grew.AssertFalse();
		log.Warnings.Count.AssertEqual(0);
	}

	#endregion

	#region Thread safety (GrowthTracker)

	[TestMethod]
	public void GrowthTracker_ConcurrentAccess_NoLostTiers()
	{
		var tracker = new GrowthTracker(firstTier: 100);
		var firedTiers = new ConcurrentBag<long>();

		// 10 threads all trying to report value 1000 for same counter
		Parallel.For(0, 10, _ =>
		{
			var tier = tracker.CheckTier("concurrent", 1000);
			if (tier > 0)
				firedTiers.Add(tier);
		});

		// should have fired tiers 100, 200, 400, 800 — exactly once each
		var sorted = firedTiers.OrderBy(x => x).ToList();
		sorted.AssertEqual([100L, 200L, 400L, 800L]);
	}

	#endregion
}
