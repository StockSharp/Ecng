namespace Ecng.Logging;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using Ecng.Common;

/// <summary>
/// Tracks the high-watermark for a set of named counters and emits a warning
/// only when a counter crosses the next doubling tier (1000 → 2000 → 4000 → …).
/// Thread-safe. One instance per class replaces N separate <c>long</c> watermark fields.
/// </summary>
public sealed class GrowthTracker
{
	private readonly ConcurrentDictionary<string, long> _watermarks = new(StringComparer.Ordinal);
	private readonly long _firstTier;
	private readonly int _multiplier;

	/// <summary>
	/// Initializes a new instance.
	/// </summary>
	/// <param name="firstTier">First threshold that triggers a warning (default 1000).</param>
	/// <param name="multiplier">Multiplier for successive tiers (default 2 = doubling).</param>
	public GrowthTracker(long firstTier = 1000, int multiplier = 2)
	{
		if (firstTier <= 0)
			throw new ArgumentOutOfRangeException(nameof(firstTier));
		if (multiplier < 2)
			throw new ArgumentOutOfRangeException(nameof(multiplier));

		_firstTier = firstTier;
		_multiplier = multiplier;
	}

	/// <summary>
	/// Checks whether <paramref name="current"/> has crossed the next tier for
	/// the counter identified by <paramref name="name"/>. Returns the crossed tier
	/// value, or 0 if no tier was crossed.
	/// </summary>
	public long CheckTier(string name, long current)
	{
		while (true)
		{
			var prev = _watermarks.GetOrAdd(name, 0L);
			var next = prev == 0 ? _firstTier : prev * _multiplier;

			if (current < next)
				return 0;

			if (_watermarks.TryUpdate(name, next, prev))
				return next;
		}
	}

	/// <summary>
	/// Resets the watermark for the specified counter.
	/// </summary>
	public void Reset(string name)
		=> _watermarks.TryRemove(name, out _);

	/// <summary>
	/// Resets all watermarks.
	/// </summary>
	public void ResetAll()
		=> _watermarks.Clear();
}

/// <summary>
/// Tracks sizes of named data sources (collections, queues, buffers) and can produce
/// a snapshot or dump only the ones that grew since the last snapshot.
/// Thread-safe.
/// </summary>
public sealed class CollectionSizeLog
{
	private readonly ConcurrentDictionary<string, Func<long>> _sources = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, long> _prevSizes = new(StringComparer.Ordinal);

	/// <summary>
	/// Registers a named data source whose size will be tracked.
	/// </summary>
	/// <param name="sizeFunc">Delegate returning the current size.</param>
	/// <param name="name">Human-readable name for the counter.</param>
	public void Track(Func<long> sizeFunc, string name)
	{
		ArgumentNullException.ThrowIfNull(sizeFunc);

		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		_sources[name] = sizeFunc;
	}

	/// <summary>
	/// Unregisters a tracked source.
	/// </summary>
	public void Untrack(string name)
	{
		_sources.TryRemove(name, out _);
		_prevSizes.TryRemove(name, out _);
	}

	/// <summary>
	/// Returns a snapshot of all tracked sizes as name→value pairs.
	/// Updates the internal previous-size cache.
	/// </summary>
	public IReadOnlyList<(string Name, long Size)> Snapshot()
	{
		var result = new List<(string, long)>(_sources.Count);

		foreach (var (name, func) in _sources)
		{
			var size = func();
			_prevSizes[name] = size;
			result.Add((name, size));
		}

		result.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.Ordinal));
		return result;
	}

	/// <summary>
	/// Returns only the sources that grew since the last <see cref="Snapshot"/> or
	/// <see cref="SnapshotGrowing"/> call.
	/// </summary>
	public IReadOnlyList<(string Name, long Size, long Prev)> SnapshotGrowing()
	{
		var result = new List<(string, long, long)>();

		foreach (var (name, func) in _sources)
		{
			var size = func();
			var prev = _prevSizes.GetOrAdd(name, 0L);
			_prevSizes[name] = size;

			if (size > prev)
				result.Add((name, size, prev));
		}

		result.Sort((a, b) => string.Compare(a.Item1, b.Item1, StringComparison.Ordinal));
		return result;
	}

	/// <summary>
	/// Formats a snapshot as a single log-friendly line.
	/// </summary>
	public static string Format(IReadOnlyList<(string Name, long Size)> snapshot)
	{
		if (snapshot.Count == 0)
			return "SIZES (empty)";

		var sb = new StringBuilder("SIZES ");

		for (var i = 0; i < snapshot.Count; i++)
		{
			if (i > 0) sb.Append(' ');
			sb.Append(snapshot[i].Name).Append('=').Append(snapshot[i].Size);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Formats a growing snapshot as a single log-friendly line.
	/// </summary>
	public static string FormatGrowing(IReadOnlyList<(string Name, long Size, long Prev)> snapshot)
	{
		if (snapshot.Count == 0)
			return string.Empty;

		var sb = new StringBuilder("GROWING ");

		for (var i = 0; i < snapshot.Count; i++)
		{
			if (i > 0) sb.Append(' ');
			sb.Append(snapshot[i].Name).Append('=').Append(snapshot[i].Size)
				.Append("(+").Append(snapshot[i].Size - snapshot[i].Prev).Append(')');
		}

		return sb.ToString();
	}
}

/// <summary>
/// Represents a point-in-time snapshot of process memory.
/// </summary>
public readonly record struct MemorySnapshot(
	long ManagedHeap,
	long WorkingSet,
	int Gen0,
	int Gen1,
	int Gen2)
{
	/// <summary>
	/// Takes a snapshot of current process memory state.
	/// </summary>
	public static MemorySnapshot Capture()
		=> new(
			GC.GetTotalMemory(false),
			Process.GetCurrentProcess().WorkingSet64,
			GC.CollectionCount(0),
			GC.CollectionCount(1),
			GC.CollectionCount(2));

	/// <inheritdoc />
	public override string ToString()
		=> $"MEM heap={FormatBytes(ManagedHeap)} working={FormatBytes(WorkingSet)} GC[0]={Gen0} [1]={Gen1} [2]={Gen2}";

	private static string FormatBytes(long bytes)
	{
		return bytes switch
		{
			>= 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F1}GB",
			>= 1024L * 1024 => $"{bytes / (1024.0 * 1024):F1}MB",
			>= 1024L => $"{bytes / 1024.0:F1}KB",
			_ => $"{bytes}B",
		};
	}
}

/// <summary>
/// Extension methods for diagnostics logging on <see cref="ILogReceiver"/>.
/// </summary>
public static class DiagnosticsHelper
{
	/// <summary>
	/// Emits a warning if <paramref name="current"/> has crossed the next doubling tier
	/// for the counter identified by <paramref name="name"/>.
	/// Uses a <see cref="GrowthTracker"/> instance so that one field per class
	/// replaces many individual watermark fields.
	/// </summary>
	public static void LogGrowth(this ILogReceiver log, GrowthTracker tracker, long current, string name)
	{
		var tier = tracker.CheckTier(name, current);

		if (tier > 0)
			log.AddWarningLog("STATS GROWTH {0}={1} (tier>={2})", name, current, tier);
	}

	/// <summary>
	/// Emits a warning if <paramref name="current"/> has crossed the next doubling tier.
	/// Uses a raw <c>ref long</c> watermark — one field per tracked counter.
	/// </summary>
	public static void LogGrowth(this ILogReceiver log, ref long watermark, long current, string name)
	{
		const long firstTier = 1000;

		while (true)
		{
			var prev = Interlocked.Read(ref watermark);
			var next = prev == 0 ? firstTier : prev * 2;

			if (current < next)
				return;

			if (Interlocked.CompareExchange(ref watermark, next, prev) == prev)
			{
				log.AddWarningLog("STATS GROWTH {0}={1} (tier>={2})", name, current, next);
				return;
			}
		}
	}

	/// <summary>
	/// Logs a memory snapshot at warning level.
	/// </summary>
	public static MemorySnapshot LogMemorySnapshot(this ILogReceiver log)
	{
		var snap = MemorySnapshot.Capture();
		log.AddWarningLog(snap.ToString());
		return snap;
	}

	/// <summary>
	/// Logs a memory snapshot only if managed heap grew by at least <paramref name="threshold"/>
	/// bytes since the last reported value stored in <paramref name="lastHeap"/>.
	/// </summary>
	/// <returns>The captured snapshot, or <c>null</c> if no log was emitted.</returns>
	public static MemorySnapshot? LogMemorySnapshotIfGrew(this ILogReceiver log, ref long lastHeap, long threshold = 50_000_000)
	{
		var snap = MemorySnapshot.Capture();
		var prev = Interlocked.Read(ref lastHeap);

		if (snap.ManagedHeap - prev < threshold)
			return null;

		if (Interlocked.CompareExchange(ref lastHeap, snap.ManagedHeap, prev) != prev)
			return null;

		log.AddWarningLog(snap.ToString());
		return snap;
	}

	/// <summary>
	/// Logs all tracked collection sizes at info level.
	/// </summary>
	public static void LogSizes(this ILogReceiver log, CollectionSizeLog sizeLog)
	{
		var snapshot = sizeLog.Snapshot();
		log.AddInfoLog(CollectionSizeLog.Format(snapshot));
	}

	/// <summary>
	/// Logs only the tracked collections that grew since the last call, at warning level.
	/// Returns <c>true</c> if any growth was detected.
	/// </summary>
	public static bool LogGrowingSizes(this ILogReceiver log, CollectionSizeLog sizeLog)
	{
		var growing = sizeLog.SnapshotGrowing();

		if (growing.Count == 0)
			return false;

		log.AddWarningLog(CollectionSizeLog.FormatGrowing(growing));
		return true;
	}
}
