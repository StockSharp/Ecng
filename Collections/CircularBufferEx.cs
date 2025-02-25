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
	public override void PushBack(TItem result)
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

		base.PushBack(result);

		if (op is not null)
			Sum = op.Add(Sum, result);

		if (maxComparer is not null)
		{
			if (recalcMax)
				Max.Value = this.Max(maxComparer);
			else if (!Max.HasValue || maxComparer?.Compare(Max.Value, result) < 0)
				Max.Value = result;
		}

		if (minComparer is not null)
		{
			if (recalcMin)
				Min.Value = this.Min(minComparer);
			else if (!Min.HasValue || minComparer?.Compare(Min.Value, result) > 0)
				Min.Value = result;
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
}