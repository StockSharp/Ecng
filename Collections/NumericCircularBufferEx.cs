#if NET7_0_OR_GREATER
namespace Ecng.Collections;

using System.Linq;
using System.Numerics;

using Ecng.Common;

/// <summary>
/// <see cref="CircularBuffer{TItem}"/> with additional features for numeric types.
/// Uses <see cref="INumber{TSelf}"/> operators for calculations.
/// </summary>
/// <typeparam name="TItem">The numeric type of elements in the buffer.</typeparam>
public class NumericCircularBufferEx<TItem> : CircularBuffer<TItem>, ICircularBufferEx<TItem>
	where TItem : INumber<TItem>
{
	private CircularBufferStats _stats;

	/// <summary>
	/// Initializes a new instance of the <see cref="NumericCircularBufferEx{TItem}"/>.
	/// </summary>
	/// <param name="capacity">Capacity.</param>
	public NumericCircularBufferEx(int capacity)
		: base(capacity)
	{
		Clear();
	}

	/// <inheritdoc />
	public CircularBufferStats Stats
	{
		get => _stats;
		set => _stats = value;
	}

	private bool CalcSum => (_stats & CircularBufferStats.Sum) != 0;
	private bool CalcMin => (_stats & CircularBufferStats.Min) != 0;
	private bool CalcMax => (_stats & CircularBufferStats.Max) != 0;

	/// <inheritdoc />
	public NullableEx<TItem> Max { get; private set; } = new();

	/// <inheritdoc />
	public NullableEx<TItem> Min { get; private set; } = new();

	/// <inheritdoc />
	public TItem Sum { get; private set; }

	/// <inheritdoc />
	public TItem SumNoFirst
	{
		get
		{
			if (Count == 0)
				return default;

			return Sum - this[0];
		}
	}

	/// <inheritdoc />
	public override void PushBack(TItem item)
	{
		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			var removed = this[0];

			if (CalcSum)
				Sum -= removed;

			if (CalcMax && Max.HasValue && Max.Value == removed)
				recalcMax = true;

			if (CalcMin && Min.HasValue && Min.Value == removed)
				recalcMin = true;
		}

		base.PushBack(item);

		if (CalcSum)
			Sum += item;

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
				Sum -= removed;

			if (CalcMax && Max.HasValue && Max.Value == removed)
				recalcMax = true;

			if (CalcMin && Min.HasValue && Min.Value == removed)
				recalcMin = true;
		}

		base.PushFront(item);

		if (CalcSum)
			Sum += item;

		UpdateMaxMin(item, recalcMax, recalcMin);
	}

	private void UpdateMaxMin(TItem item, bool recalcMax, bool recalcMin)
	{
		if (CalcMax)
		{
			if (recalcMax)
				Max.Value = this.Max();
			else if (!Max.HasValue || item > Max.Value)
				Max.Value = item;
		}

		if (CalcMin)
		{
			if (recalcMin)
				Min.Value = this.Min();
			else if (!Min.HasValue || item < Min.Value)
				Min.Value = item;
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
			Sum = this.Aggregate(TItem.Zero, static (acc, x) => acc + x);
		else
			Sum = default;

		if (CalcMax)
			Max.Value = this.Max();
		else
			Max = new();

		if (CalcMin)
			Min.Value = this.Min();
		else
			Min = new();
	}
}
#endif