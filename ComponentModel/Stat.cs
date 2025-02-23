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

	/// <summary>
	/// Contains statistical information related to actions.
	/// </summary>
	/// <typeparam name="TAction">The type representing the action.</typeparam>
	public struct StatInfo<TAction>
	{
		/// <summary>
		/// Represents an item that holds a value, an IP address, and an action.
		/// </summary>
		/// <typeparam name="TValue">The type of the value.</typeparam>
		public struct Item<TValue>
		{
			/// <summary>
			/// Gets or sets the value.
			/// </summary>
			public TValue Value { get; set; }

			/// <summary>
			/// Gets or sets the IP address.
			/// </summary>
			public IPAddress Address { get; set; }

			/// <summary>
			/// Gets or sets the action.
			/// </summary>
			public TAction Action { get; set; }
		}

		/// <summary>
		/// Gets or sets the unique count.
		/// </summary>
		public int UniqueCount { get; set; }

		/// <summary>
		/// Gets or sets the pending count.
		/// </summary>
		public int PendingCount { get; set; }

		/// <summary>
		/// Gets or sets the aggressive IP address.
		/// </summary>
		public IPAddress AggressiveAddress { get; set; }

		/// <summary>
		/// Gets or sets the aggressive time.
		/// </summary>
		public TimeSpan AggressiveTime { get; set; }

		/// <summary>
		/// Gets or sets the frequency information.
		/// </summary>
		public Item<int>[] Freq { get; set; }

		/// <summary>
		/// Gets or sets the longest duration information.
		/// </summary>
		public Item<TimeSpan>[] Longest { get; set; }

		/// <summary>
		/// Gets or sets the pending actions information.
		/// </summary>
		public Item<TimeSpan>[] Pendings { get; set; }

		/// <summary>
		/// Returns a string representation of the statistical information.
		/// </summary>
		/// <returns>A string that represents the current stat info.</returns>
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

	/// <summary>
	/// Provides statistical tracking for actions.
	/// </summary>
	/// <typeparam name="TAction">The type representing the action.</typeparam>
	public class Stat<TAction>
	{
		/// <summary>
		/// Represents an item that tracks the duration of an action.
		/// </summary>
		public class Item : Disposable
		{
			private readonly Stat<TAction> _parent;
			private readonly Stopwatch _watch;

			/// <summary>
			/// Initializes a new instance of the <see cref="Item"/> class.
			/// </summary>
			/// <param name="parent">The parent statistic instance.</param>
			/// <param name="action">The action being tracked.</param>
			/// <param name="address">The IP address associated with the action.</param>
			/// <param name="watch">The stopwatch tracking the action duration.</param>
			internal Item(Stat<TAction> parent, TAction action, IPAddress address, Stopwatch watch)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				Action = action ?? throw new ArgumentNullException(nameof(action));
				Address = address ?? throw new ArgumentNullException(nameof(address));
				_watch = watch ?? throw new ArgumentNullException(nameof(watch));
			}

			/// <summary>
			/// Gets the action being tracked.
			/// </summary>
			internal readonly TAction Action;

			/// <summary>
			/// Gets the IP address associated with the action.
			/// </summary>
			internal readonly IPAddress Address;

			/// <summary>
			/// Disposes the current tracking item and updates the parent statistics.
			/// </summary>
			protected override void DisposeManaged()
			{
				_watch.Stop();
				_parent.End(this, _watch);

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Gets or sets the maximum number of longest duration records to keep.
		/// </summary>
		public int LongestLimit { get; set; } = 100;

		/// <summary>
		/// Gets or sets the maximum number of frequency records to keep.
		/// </summary>
		public int FreqLimit { get; set; } = 100000;

		private IPAddress _aggressiveIp;
		private TimeSpan _aggressiveTime;
		private readonly Dictionary<TAction, int> _freq = [];
		private readonly Collections.PriorityQueue<TimeSpan, TAction> _longests = new((p1, p2) => (p1 - p2).Abs(), new BackwardComparer<TimeSpan>());
		private readonly Dictionary<Stopwatch, (IPAddress, TAction)> _pendings = [];
		private readonly Dictionary<IPAddress, RefTriple<HashSet<Stopwatch>, long, TimeSpan>> _allWatches = [];
		private readonly SyncObject _sync = new();

		/// <summary>
		/// Retrieves statistical information with support for paging.
		/// </summary>
		/// <param name="skip">The number of records to skip.</param>
		/// <param name="take">The number of records to take.</param>
		/// <returns>A <see cref="StatInfo{TAction}"/> structure containing the current statistics.</returns>
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

					Freq = [.. _freq.OrderByDescending(p => p.Value).Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<int>
					{
						Value = p.Value,
						Action = p.Key,
					})],

					Longest = [.. _longests.Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<TimeSpan>
					{
						Value = p.Item1,
						Action = p.Item2,
					})],

					Pendings = [.. _pendings.Skip(skip).Take(take).Select(p => new StatInfo<TAction>.Item<TimeSpan>
					{
						Value = p.Key.Elapsed,
						Address = p.Value.Item1,
						Action = p.Value.Item2,
					})],
				};
			}
		}

		/// <summary>
		/// Starts tracking an action.
		/// </summary>
		/// <param name="action">The action to track.</param>
		/// <param name="address">The IP address associated with the action.</param>
		/// <returns>An <see cref="Item"/> that represents the action tracking session.</returns>
		/// <exception cref="ArgumentNullException">Thrown when action or address is null.</exception>
		public Item Begin(TAction action, IPAddress address)
		{
			if (action is null)		throw new ArgumentNullException(nameof(action));
			if (address is null)	throw new ArgumentNullException(nameof(address));

			var watch = Stopwatch.StartNew();

			lock (_sync)
			{
				_pendings.Add(watch, new(address, action));
				var tuple = _allWatches.SafeAdd(address, key => new([], default, default));
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

				if (_longests.Count >= LongestLimit)
					_longests.EnqueueDequeue(watch.Elapsed, item.Action);
				else
					_longests.Enqueue(watch.Elapsed, item.Action);
			}
		}

		/// <summary>
		/// Clears all the recorded statistics.
		/// </summary>
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