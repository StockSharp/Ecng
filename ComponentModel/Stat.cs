namespace Ecng.ComponentModel
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Net;
	using System.Linq;
	using System.Text;

	using Ecng.Common;
	using Ecng.Collections;

	public struct StatInfo<TAction>
	{
		public struct Item<TValue>
		{
			public TValue Value { get; set; }
			public IPAddress Address { get; set; }
			public TAction Action { get; set; }
		}

		public int UniqueCount { get; set; }
		public int PendingCount { get; set; }

		public IPAddress AggressiveAddress { get; set; }
		public TimeSpan AggressiveTime { get; set; }

		public Item<int>[] Freq { get; set; }
		public Item<TimeSpan>[] Longest { get; set; }
		public Item<TimeSpan>[] Pendings { get; set; }

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendLine($"Unique count: {UniqueCount}");
			sb.AppendLine($"Pending count: {PendingCount}");
			sb.AppendLine($"Aggressive IP: {AggressiveAddress}");
			sb.AppendLine($"Aggressive time: {AggressiveTime}");

			sb.AppendLine();

			sb.AppendLine("Freq:");

			foreach (var p in Freq)
				sb.AppendLine($"({p.Value}): '{p.Action}'");

			sb.AppendLine();

			sb.AppendLine("Long:");

			foreach (var p in Longest)
				sb.AppendLine($"({p.Value}): '{p.Action}'");

			sb.AppendLine();

			sb.AppendLine("Pend:");

			foreach (var p in Pendings)
				sb.AppendLine($"({p.Value}/{p.Address}): '{p.Action}'");

			sb.AppendLine();

			return sb.ToString();
		}
	}

	public class Stat<TAction>
	{
		public class Item : IDisposable
		{
			private readonly Stat<TAction> _parent;
			private readonly Stopwatch _watch;

			internal Item(Stat<TAction> parent, TAction action, IPAddress address, Stopwatch watch)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				Action = action ?? throw new ArgumentNullException(nameof(action));
				Address = address ?? throw new ArgumentNullException(nameof(address));
				_watch = watch ?? throw new ArgumentNullException(nameof(watch));
			}

			internal readonly TAction Action;
			internal readonly IPAddress Address;

			void IDisposable.Dispose()
			{
				_watch.Stop();
				_parent.End(this, _watch);
			}
		}

		public int LongestLimit { get; set; } = 100;
		public int FreqLimit { get; set; } = 100000;

		private IPAddress _aggressiveIp;
		private TimeSpan _aggressiveTime;
		private readonly Dictionary<TAction, int> _freq = new();
		private readonly OrderedPriorityQueue<TimeSpan, TAction> _longests = new(new BackwardComparer<TimeSpan>());
		private readonly Dictionary<Stopwatch, (IPAddress, TAction)> _pendings = new();
		private readonly Dictionary<IPAddress, RefTriple<HashSet<Stopwatch>, long, TimeSpan>> _allWatches = new();
		private readonly SyncObject _sync = new();

		public StatInfo<TAction> GetInfo(int skip, int take)
		{
			lock (_sync)
			{
				return new()
				{
					UniqueCount = _allWatches.Count,
					PendingCount = _pendings.Count,

					AggressiveAddress = _aggressiveIp,
					AggressiveTime = _aggressiveTime,

					Freq = _freq.OrderByDescending(p => p.Value).Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<int>
					{
						Value = p.Value,
						Action = p.Key,
					}).ToArray(),

					Longest = _longests.Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<TimeSpan>
					{
						Value = p.Key,
						Action = p.Value,
					}).ToArray(),

					Pendings = _pendings.Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<TimeSpan>
					{
						Value = p.Key.Elapsed,
						Address = p.Value.Item1,
						Action = p.Value.Item2,
					}).ToArray(),
				};
			}
		}

		public Item Begin(TAction action, IPAddress address)
		{
			if (action is null)		throw new ArgumentNullException(nameof(action));
			if (address is null)	throw new ArgumentNullException(nameof(address));

			var watch = Stopwatch.StartNew();

			lock (_sync)
			{
				_pendings.Add(watch, new(address, action));
				var tuple = _allWatches.SafeAdd(address, key => new(new(), default, default));
				tuple.First.Add(watch);
				tuple.Second++;

				if (_freq.TryGetValue(action, out var counter))
				{
					counter++;
					_freq[action] = counter;
				}
				else
				{
					if (_freq.Count < FreqLimit)
						_freq.Add(action, 1);
				}
			}

			return new(this, action, address, watch);
		}

		private void End(Item item, Stopwatch watch)
		{
			if (item is null)	throw new ArgumentNullException(nameof(item));
			if (watch is null)	throw new ArgumentNullException(nameof(watch));

			var elapsed = watch.Elapsed;
			var addr = item.Address;

			lock (_sync)
			{
				_pendings.Remove(watch);

				if (_allWatches.TryGetValue(addr, out var tuple))
				{
					tuple.First.Remove(watch);
					tuple.Third += elapsed;

					if (_aggressiveTime == default)
					{
						_aggressiveTime = tuple.Third;
						_aggressiveIp = addr;
					}
					else if (_aggressiveTime < tuple.Third)
					{
						_aggressiveTime = tuple.Third;
						_aggressiveIp = addr;
					}
				}

				_longests.Enqueue(watch.Elapsed, item.Action);

				if (_longests.Count > LongestLimit)
					_longests.Dequeue();
			}
		}

		public void Clear()
		{
			lock (_sync)
			{
				_aggressiveIp = default;
				_aggressiveTime = default;
				_allWatches.Clear();
				_freq.Clear();
				_pendings.Clear();
				_longests.Clear();
			}
		}
	}
}