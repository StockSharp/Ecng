namespace Ecng.Collections;

using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

/// <summary>
/// CircularBuffer{TItem} with additional features.
/// </summary>
public class CircularBufferEx<TItem> : CircularBuffer<TItem>
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

	/// <summary>
	/// Calc <see cref="Sum"/>.
	/// </summary>
	public IOperator<TItem> Operator { get; set; }

	/// <summary>
	/// Calc <see cref="Max"/>.
	/// </summary>
	public IComparer<TItem> MaxComparer { get; set; }

	/// <summary>
	/// Calc <see cref="Min"/>.
	/// </summary>
	public IComparer<TItem> MinComparer { get; set; }

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
	public TItem SumNoFirst => Count == 0 ? default : Operator.Subtract(Sum, this[0]);

	/// <inheritdoc />
	public override void PushBack(TItem item)
	{
		var op = Operator;
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			if (op is not null)
				Sum = op.Subtract(Sum, this[0]);

			if (maxComparer?.Compare(Max.Value, this[0]) == 0)
				recalcMax = true;

			if (minComparer?.Compare(Min.Value, this[0]) == 0)
				recalcMin = true;
		}

		base.PushBack(item);

		if (op is not null)
			Sum = op.Add(Sum, item);

		if (maxComparer is not null)
		{
			if (recalcMax)
				Max.Value = this.Max(maxComparer);
			else if (!Max.HasValue || maxComparer?.Compare(Max.Value, item) < 0)
				Max.Value = item;
		}

		if (minComparer is not null)
		{
			if (recalcMin)
				Min.Value = this.Min(minComparer);
			else if (!Min.HasValue || minComparer?.Compare(Min.Value, item) > 0)
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
	public override void PushFront(TItem item)
	{
		var op = Operator;
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		var recalcMax = false;
		var recalcMin = false;

		if (Count == Capacity)
		{
			if (op is not null)
				Sum = op.Subtract(Sum, this[0]);

			if (maxComparer?.Compare(Max.Value, this[0]) == 0)
				recalcMax = true;

			if (minComparer?.Compare(Min.Value, this[0]) == 0)
				recalcMin = true;
		}

		base.PushFront(item);

		if (op is not null)
			Sum = op.Add(Sum, item);

		if (maxComparer is not null)
		{
			if (recalcMax)
				Max.Value = this.Max(maxComparer);
			else if (!Max.HasValue || maxComparer?.Compare(Max.Value, item) < 0)
				Max.Value = item;
		}

		if (minComparer is not null)
		{
			if (recalcMin)
				Min.Value = this.Min(minComparer);
			else if (!Min.HasValue || minComparer?.Compare(Min.Value, item) > 0)
				Min.Value = item;
		}
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
		var op = Operator;
		var maxComparer = MaxComparer;
		var minComparer = MinComparer;

		if (op is not null)
		{
			Sum = this.Any() ? this.Aggregate(op.Add) : default;
		}
		else
		{
			Sum = default;
		}

		if (maxComparer is not null && this.Any())
		{
			Max.Value = this.Max(maxComparer);
		}
		else
		{
			Max = new();
		}

		if (minComparer is not null && this.Any())
		{
			Min.Value = this.Min(minComparer);
		}
		else
		{
			Min = new();
		}
	}
}