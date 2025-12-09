namespace Ecng.Collections;

using System.Linq;
#if NET7_0_OR_GREATER
using System.Numerics;
#else
using System.Collections.Generic;
#endif

using Ecng.Common;

/// <summary>
/// <see cref="CircularBuffer{TItem}"/> with additional features.
/// </summary>
/// <typeparam name="TItem">The type of elements in the buffer.</typeparam>
public class CircularBufferEx<TItem> : CircularBuffer<TItem>
#if NET7_0_OR_GREATER
	where TItem : INumber<TItem>
#endif
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CircularBufferEx{TItem}"/>.
	/// </summary>
	/// <param name="capacity">Capacity.</param>
	public CircularBufferEx(int capacity)
		: base(capacity)
	{
		Clear();
	}

#if !NET7_0_OR_GREATER
	/// <summary>
	/// Operator for arithmetic operations.
	/// </summary>
	public IOperator<TItem> Operator { get; set; }

	/// <summary>
	/// Comparer for calculating <see cref="Max"/>.
	/// If not set, <see cref="Operator"/> will be used.
	/// </summary>
	public IComparer<TItem> MaxComparer { get; set; }

	/// <summary>
	/// Comparer for calculating <see cref="Min"/>.
	/// If not set, <see cref="Operator"/> will be used.
	/// </summary>
	public IComparer<TItem> MinComparer { get; set; }
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
			return Operator.Subtract(Sum, this[0]);
#endif
		}
	}

#if NET7_0_OR_GREATER
	private static TItem Add(TItem a, TItem b) => a + b;
	private static TItem Subtract(TItem a, TItem b) => a - b;
	private static int Compare(TItem a, TItem b) => a.CompareTo(b);
#else
	private TItem Add(TItem a, TItem b) => Operator.Add(a, b);
	private TItem Subtract(TItem a, TItem b) => Operator.Subtract(a, b);
	private int Compare(TItem a, TItem b) => Operator?.Compare(a, b) ?? Comparer<TItem>.Default.Compare(a, b);
#endif

	/// <inheritdoc />
	public override void PushBack(TItem item)
	{
		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			var removed = this[0];

#if NET7_0_OR_GREATER
			Sum -= removed;
#else
			if (Operator is not null)
				Sum = Subtract(Sum, removed);
#endif

#if NET7_0_OR_GREATER
			if (Max.HasValue && Max.Value == removed)
				recalcMax = true;
			if (Min.HasValue && Min.Value == removed)
				recalcMin = true;
#else
			if (Max.HasValue && MaxComparer?.Compare(Max.Value, removed) == 0)
				recalcMax = true;
			if (Min.HasValue && MinComparer?.Compare(Min.Value, removed) == 0)
				recalcMin = true;
#endif
		}

		base.PushBack(item);

#if NET7_0_OR_GREATER
		Sum += item;
#else
		if (Operator is not null)
			Sum = Add(Sum, item);
#endif

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

#if NET7_0_OR_GREATER
			Sum -= removed;
#else
			if (Operator is not null)
				Sum = Subtract(Sum, removed);
#endif

#if NET7_0_OR_GREATER
			if (Max.HasValue && Max.Value == removed)
				recalcMax = true;
			if (Min.HasValue && Min.Value == removed)
				recalcMin = true;
#else
			if (Max.HasValue && MaxComparer?.Compare(Max.Value, removed) == 0)
				recalcMax = true;
			if (Min.HasValue && MinComparer?.Compare(Min.Value, removed) == 0)
				recalcMin = true;
#endif
		}

		base.PushFront(item);

#if NET7_0_OR_GREATER
		Sum += item;
#else
		if (Operator is not null)
			Sum = Add(Sum, item);
#endif

		UpdateMaxMin(item, recalcMax, recalcMin);
	}

	private void UpdateMaxMin(TItem item, bool recalcMax, bool recalcMin)
	{
#if NET7_0_OR_GREATER
		if (recalcMax)
			Max.Value = this.Max();
		else if (!Max.HasValue || item > Max.Value)
			Max.Value = item;

		if (recalcMin)
			Min.Value = this.Min();
		else if (!Min.HasValue || item < Min.Value)
			Min.Value = item;
#else
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		if (maxComparer is not null)
		{
			if (recalcMax)
				Max.Value = this.Max(maxComparer);
			else if (!Max.HasValue || maxComparer.Compare(Max.Value, item) < 0)
				Max.Value = item;
		}

		if (minComparer is not null)
		{
			if (recalcMin)
				Min.Value = this.Min(minComparer);
			else if (!Min.HasValue || minComparer.Compare(Min.Value, item) > 0)
				Min.Value = item;
		}
#endif
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

#if NET7_0_OR_GREATER
		Sum = this.Aggregate(TItem.Zero, (acc, x) => acc + x);
		Max.Value = this.Max();
		Min.Value = this.Min();
#else
		var op = Operator;
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		if (op is not null)
		{
			Sum = this.Aggregate(op.Add);
		}
		else
		{
			Sum = default;
		}

		if (maxComparer is not null)
		{
			Max.Value = this.Max(maxComparer);
		}
		else
		{
			Max = new();
		}

		if (minComparer is not null)
		{
			Min.Value = this.Min(minComparer);
		}
		else
		{
			Min = new();
		}
#endif
	}
}