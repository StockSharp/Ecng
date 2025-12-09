namespace Ecng.Collections;

using System;
using System.Linq;
#if NET7_0_OR_GREATER
using System.Numerics;
#else
using System.Collections.Generic;
#endif

using Ecng.Common;

/// <summary>
/// Specifies which statistics to calculate in <see cref="CircularBufferEx{TItem}"/>.
/// </summary>
[Flags]
public enum CircularBufferStats
{
	/// <summary>
	/// No statistics calculated.
	/// </summary>
	None = 0,

	/// <summary>
	/// Calculate sum of elements.
	/// </summary>
	Sum = 1,

	/// <summary>
	/// Calculate minimum value.
	/// </summary>
	Min = 2,

	/// <summary>
	/// Calculate maximum value.
	/// </summary>
	Max = 4,

	/// <summary>
	/// Calculate all statistics.
	/// </summary>
	All = Sum | Min | Max
}

/// <summary>
/// <see cref="CircularBuffer{TItem}"/> with additional features.
/// </summary>
/// <typeparam name="TItem">The type of elements in the buffer.</typeparam>
public class CircularBufferEx<TItem> : CircularBuffer<TItem>
#if NET7_0_OR_GREATER
	where TItem : INumber<TItem>
#endif
{
	private CircularBufferStats _stats;
#if !NET7_0_OR_GREATER
	private bool _statsExplicitlySet;
#endif

	/// <summary>
	/// Initializes a new instance of the <see cref="CircularBufferEx{TItem}"/>.
	/// </summary>
	/// <param name="capacity">Capacity.</param>
	public CircularBufferEx(int capacity)
		: base(capacity)
	{
		Clear();
	}

	/// <summary>
	/// Specifies which statistics to calculate.
	/// </summary>
	public CircularBufferStats Stats
	{
		get => _stats;
		set
		{
			_stats = value;
#if !NET7_0_OR_GREATER
			_statsExplicitlySet = true;
#endif
		}
	}

	private bool CalcSum => (_stats & CircularBufferStats.Sum) != 0;
	private bool CalcMin => (_stats & CircularBufferStats.Min) != 0;
	private bool CalcMax => (_stats & CircularBufferStats.Max) != 0;

#if !NET7_0_OR_GREATER
	private IOperator<TItem> _operator;

	/// <summary>
	/// Operator for arithmetic operations.
	/// </summary>
	public IOperator<TItem> Operator
	{
		get => _operator;
		set
        {
            _operator = value;

            if (_statsExplicitlySet)
                return;

            if (value is not null)
                _stats |= CircularBufferStats.Sum;
            else
                _stats &= ~CircularBufferStats.Sum;
        }
    }

	private IComparer<TItem> _minComparer;

	/// <summary>
	/// Comparer for calculating <see cref="Min"/>.
	/// </summary>
	[Obsolete("Use Stats property with CircularBufferStats.Min flag instead.")]
	public IComparer<TItem> MinComparer
	{
		get => _minComparer;
		set
        {
            _minComparer = value;

            if (_statsExplicitlySet)
                return;

            if (value is not null)
                _stats |= CircularBufferStats.Min;
            else
                _stats &= ~CircularBufferStats.Min;
        }
    }

	private IComparer<TItem> _maxComparer;

	/// <summary>
	/// Comparer for calculating <see cref="Max"/>.
	/// </summary>
	[Obsolete("Use Stats property with CircularBufferStats.Max flag instead.")]
	public IComparer<TItem> MaxComparer
	{
		get => _maxComparer;
		set
        {
            _maxComparer = value;

            if (_statsExplicitlySet)
                return;

            if (value is not null)
                _stats |= CircularBufferStats.Max;
            else
                _stats &= ~CircularBufferStats.Max;
        }
    }

	private IComparer<TItem> GetMinComparer() => _minComparer ?? _operator;
	private IComparer<TItem> GetMaxComparer() => _maxComparer ?? _operator;
#endif

	/// <summary>
	/// Max value.
	/// </summary>
	public NullableEx<TItem> Max { get; private set; } = new();

	/// <summary>
	/// Min value.
	/// </summary>
	public NullableEx<TItem> Min { get; private set; } = new();

	/// <summary>
	/// Sum of all elements in buffer.
	/// </summary>
	public TItem Sum { get; private set; }

	/// <summary>
	/// Sum of all elements in buffer without the first element.
	/// </summary>
	public TItem SumNoFirst
	{
		get
		{
			if (Count == 0)
				return default;

#if NET7_0_OR_GREATER
			return Sum - this[0];
#else
			return Subtract(Sum, this[0]);
#endif
		}
	}

#if !NET7_0_OR_GREATER
	private TItem Add(TItem a, TItem b) => _operator.Add(a, b);
	private TItem Subtract(TItem a, TItem b) => _operator.Subtract(a, b);
#endif

	/// <inheritdoc />
	public override void PushBack(TItem item)
	{
		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			var removed = this[0];

			if (CalcSum)
			{
#if NET7_0_OR_GREATER
				Sum -= removed;
#else
				if (_operator is not null)
					Sum = Subtract(Sum, removed);
#endif
			}

			if (CalcMax)
			{
#if NET7_0_OR_GREATER
				if (Max.HasValue && Max.Value == removed)
					recalcMax = true;
#else
				var maxComparer = GetMaxComparer();
				if (Max.HasValue && maxComparer?.Compare(Max.Value, removed) == 0)
					recalcMax = true;
#endif
			}

			if (CalcMin)
			{
#if NET7_0_OR_GREATER
				if (Min.HasValue && Min.Value == removed)
					recalcMin = true;
#else
				var minComparer = GetMinComparer();
				if (Min.HasValue && minComparer?.Compare(Min.Value, removed) == 0)
					recalcMin = true;
#endif
			}
		}

		base.PushBack(item);

		if (CalcSum)
		{
#if NET7_0_OR_GREATER
			Sum += item;
#else
			if (_operator is not null)
				Sum = Add(Sum, item);
#endif
		}

		UpdateMaxMin(item, recalcMax, recalcMin);
	}

	/// <inheritdoc />
	public override void PushFront(TItem item)
	{
		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			var lastIndex = Count - 1;
			var removed = this[lastIndex];

			if (CalcSum)
			{
#if NET7_0_OR_GREATER
				Sum -= removed;
#else
				if (_operator is not null)
					Sum = Subtract(Sum, removed);
#endif
			}

			if (CalcMax)
			{
#if NET7_0_OR_GREATER
				if (Max.HasValue && Max.Value == removed)
					recalcMax = true;
#else
				var maxComparer = GetMaxComparer();

				if (Max.HasValue && maxComparer?.Compare(Max.Value, removed) == 0)
					recalcMax = true;
#endif
			}

			if (CalcMin)
			{
#if NET7_0_OR_GREATER
				if (Min.HasValue && Min.Value == removed)
					recalcMin = true;
#else
				var minComparer = GetMinComparer();

				if (Min.HasValue && minComparer?.Compare(Min.Value, removed) == 0)
					recalcMin = true;
#endif
			}
		}

		base.PushFront(item);

		if (CalcSum)
		{
#if NET7_0_OR_GREATER
			Sum += item;
#else
			if (_operator is not null)
				Sum = Add(Sum, item);
#endif
		}

		UpdateMaxMin(item, recalcMax, recalcMin);
	}

	private void UpdateMaxMin(TItem item, bool recalcMax, bool recalcMin)
	{
		if (CalcMax)
		{
#if NET7_0_OR_GREATER
			if (recalcMax)
				Max.Value = this.Max();
			else if (!Max.HasValue || item > Max.Value)
				Max.Value = item;
#else
			var maxComparer = GetMaxComparer();

			if (maxComparer is not null)
			{
				if (recalcMax)
					Max.Value = this.Max(maxComparer);
				else if (!Max.HasValue || maxComparer.Compare(Max.Value, item) < 0)
					Max.Value = item;
			}
#endif
		}

		if (CalcMin)
		{
#if NET7_0_OR_GREATER
			if (recalcMin)
				Min.Value = this.Min();
			else if (!Min.HasValue || item < Min.Value)
				Min.Value = item;
#else
			var minComparer = GetMinComparer();

			if (minComparer is not null)
			{
				if (recalcMin)
					Min.Value = this.Min(minComparer);
				else if (!Min.HasValue || minComparer.Compare(Min.Value, item) > 0)
					Min.Value = item;
			}
#endif
		}
	}

	/// <inheritdoc />
	public override int Capacity
	{
		get => base.Capacity;
		set
		{
			base.Capacity = value;
			Clear();
		}
	}

	/// <summary>
	/// Reset state.
	/// </summary>
	public override void Clear()
	{
		base.Clear();

		Sum = default;
		Max = new();
		Min = new();
	}

	/// <inheritdoc />
	public override void PopBack()
	{
		base.PopBack();

		RecalculateStats();
	}

	/// <inheritdoc />
	public override void PopFront()
	{
		base.PopFront();

		RecalculateStats();
	}

	/// <inheritdoc />
	public override TItem this[int index]
	{
		get => base[index];
		set
		{
			base[index] = value;
			RecalculateStats();
		}
	}

	private void RecalculateStats()
	{
		if (Count == 0)
		{
			Sum = default;
			Max = new();
			Min = new();
			return;
		}

		if (CalcSum)
		{
#if NET7_0_OR_GREATER
			Sum = this.Aggregate(TItem.Zero, (acc, x) => acc + x);
#else
			if (_operator is not null)
				Sum = this.Aggregate(Add);
			else
				Sum = default;
#endif
		}
		else
		{
			Sum = default;
		}

		if (CalcMax)
		{
#if NET7_0_OR_GREATER
			Max.Value = this.Max();
#else
			var maxComparer = GetMaxComparer();

			if (maxComparer is not null)
				Max.Value = this.Max(maxComparer);
			else
				Max = new();
#endif
		}
		else
		{
			Max = new();
		}

		if (CalcMin)
		{
#if NET7_0_OR_GREATER
			Min.Value = this.Min();
#else
			var minComparer = GetMinComparer();

			if (minComparer is not null)
				Min.Value = this.Min(minComparer);
			else
				Min = new();
#endif
		}
		else
		{
			Min = new();
		}
	}
}