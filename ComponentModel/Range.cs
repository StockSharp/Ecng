namespace Ecng.ComponentModel;

using System;
using System.ComponentModel;

using Ecng.Common;

/// <summary>
/// Represents a generic range defined by a minimum and maximum value.
/// </summary>
/// <typeparam name="T">The type of the range values. Must implement IComparable&lt;T&gt;.</typeparam>
[Serializable]
public class Range<T> : Equatable<Range<T>>, IConvertible, IRange<T>
	where T : IComparable<T>
{
	/// <summary>
	/// Initializes static members of the <see cref="Range{T}"/> class.
	/// </summary>
	static Range() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Range{T}"/> class.
	/// </summary>
	public Range()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Range{T}"/> class with specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value of the range.</param>
	/// <param name="max">The maximum value of the range.</param>
	public Range(T min, T max)
	{
		Init(min, max);
	}

	/// <summary>
	/// Gets a value indicating whether the range has a specified minimum value.
	/// </summary>
	[Browsable(false)]
	public bool HasMinValue => _min.HasValue;

	/// <summary>
	/// Gets a value indicating whether the range has a specified maximum value.
	/// </summary>
	[Browsable(false)]
	public bool HasMaxValue => _max.HasValue;

	/// <summary>
	/// Gets or sets the operator used for arithmetic operations within the range.
	/// </summary>
	public IOperator<T> Operator { get; set; }

	#region Length

	/// <summary>
	/// Gets the difference between the maximum and minimum values of the range.
	/// </summary>
	[Browsable(false)]
	public T Length
	{
		get
		{
			Operator ??= OperatorRegistry.GetOperator<T>();

			if (HasMinValue && HasMaxValue)
				return Operator.Subtract(Max, Min);

			throw new InvalidOperationException("Length is undefined.");
		}
	}

	#endregion

	#region Min

	private readonly NullableEx<T> _min = new();

	/// <summary>
	/// Gets or sets the minimum value of the range.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the new minimum value is greater than the current maximum value.
	/// </exception>
	public T Min
	{
		get => _min.Value;
		set
		{
			if (_max.HasValue)
				ValidateBounds(value, Max);

			_min.Value = value;
		}
	}

	#endregion

	#region Max

	private readonly NullableEx<T> _max = new();

	/// <summary>
	/// Gets or sets the maximum value of the range.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the new maximum value is less than the current minimum value.
	/// </exception>
	public T Max
	{
		get => _max.Value;
		set
		{
			if (_min.HasValue)
				ValidateBounds(Min, value);

			_max.Value = value;
		}
	}

	#endregion

	#region Object Members

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return (HasMinValue ? Min.GetHashCode() : 0) ^ (HasMaxValue ? Max.GetHashCode() : 0);
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return "{{Min:{0} Max:{1}}}".Put(HasMinValue ? Min.ToString() : "null", HasMaxValue ? Max.ToString() : "null");
	}

	#endregion

	#region Equatable<Range<T>> Members

	/// <inheritdoc/>
	protected override bool OnEquals(Range<T> other)
		=> _min == other._min && _max == other._max;

	#endregion

	object IRange.MinObj
	{
		get => HasMinValue ? Min : null;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			Min = (T)value;
		}
	}

	object IRange.MaxObj
	{
		get => HasMaxValue ? Max : null;
		set
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));
			Max = (T)value;
		}
	}

	/// <inheritdoc />
	T IRange<T>.Min => Min;

	/// <inheritdoc />
	T IRange<T>.Max => Max;

	/// <inheritdoc />
	bool IRange<T>.Contains(T value) => Contains(value);

	/// <inheritdoc />
	IRange<T> IRange<T>.SubRange(T min, T max) => SubRange(min, max);

	/// <inheritdoc/>
	public override Range<T> Clone()
	{
		var result = new Range<T> { Operator = Operator };

		if (HasMinValue)
			result.Min = Min;

		if (HasMaxValue)
			result.Max = Max;

		return result;
	}

	/// <inheritdoc />
	public bool Contains(IRange<T> range)
	{
		if (range is null)
			throw new ArgumentNullException(nameof(range));

		return Contains(range.Min) && Contains(range.Max);
	}

	/// <inheritdoc />
	public IRange<T> Intersect(IRange<T> range)
	{
		if (range is null)
			throw new ArgumentNullException(nameof(range));

		if (Contains(range))
			return range.TypedClone();
		else if (range.Contains(this))
			return Clone();
		else
		{
			var containsMin = Contains(range.Min);
			var containsMax = Contains(range.Max);

			if (containsMin)
				return new Range<T>(range.Min, Max);
			else if (containsMax)
				return new Range<T>(Min, range.Max);
			else
				return null;
		}
	}

	/// <summary>
	/// Creates a sub-range from the current range given the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value of the sub-range.</param>
	/// <param name="max">The maximum value of the sub-range.</param>
	/// <returns>A new <see cref="Range{T}"/> representing the sub-range.</returns>
	/// <exception cref="ArgumentException">Thrown if either min or max is not contained within the current range.</exception>
	public Range<T> SubRange(T min, T max)
	{
		if (!Contains(min))
			throw new ArgumentException("Not in range.", nameof(min));

		if (!Contains(max))
			throw new ArgumentException("Not in range.", nameof(max));

		return new(min, max);
	}

	/// <summary>
	/// Determines whether the specified value is within the current range.
	/// </summary>
	/// <param name="value">The value to test.</param>
	/// <returns>true if the value is within the range; otherwise, false.</returns>
	public bool Contains(T value)
	{
		if (_min.HasValue && Min.CompareTo(value) > 0)
			return false;
		else if (_max.HasValue && Max.CompareTo(value) < 0)
			return false;
		else
			return true;
	}

	/// <summary>
	/// Initializes the range with the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value.</param>
	/// <param name="max">The maximum value.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if the minimum value is greater than the maximum value.</exception>
	private void Init(T min, T max)
	{
		ValidateBounds(min, max);

		_min.Value = min;
		_max.Value = max;
	}

	private static void ValidateBounds(T min, T max)
	{
		if (min.CompareTo(max) > 0)
			throw new ArgumentOutOfRangeException(nameof(min), $"Min value {min} is more than max value {max}.");
	}

	TypeCode IConvertible.GetTypeCode()
	{
		return TypeCode.Object;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException();
	}

	string IConvertible.ToString(IFormatProvider provider)
	{
		return ToString();
	}

	object IConvertible.ToType(Type conversionType, IFormatProvider provider)
	{
		if (conversionType == typeof(string))
			return ToString();

		throw new InvalidCastException();
	}
}
