#if NET7_0_OR_GREATER
namespace Ecng.ComponentModel;

using System;
using System.Numerics;

/// <summary>
/// Represents a numeric range for <typeparamref name="TNumber"/> supporting generic math operations.
/// </summary>
/// <typeparam name="TNumber">The numeric type that implements <see cref="INumber{TSelf}"/>.</typeparam>
public readonly struct NumericRange<TNumber> : IRange<TNumber>
	where TNumber : struct, INumber<TNumber>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NumericRange{TNumber}"/> struct.
	/// </summary>
	/// <param name="min">The lower bound of the range (inclusive).</param>
	/// <param name="max">The upper bound of the range (inclusive).</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
	public NumericRange(TNumber min, TNumber max)
	{
		if (min > max)
			throw new ArgumentOutOfRangeException(nameof(min), min, $"Min {min} greater than max {max}.");

		Min = min;
		Max = max;
	}

	/// <summary>
	/// Gets the lower bound of the range (inclusive).
	/// </summary>
	public TNumber Min { get; }

	/// <summary>
	/// Gets the upper bound of the range (inclusive).
	/// </summary>
	public TNumber Max { get; }

	/// <summary>
	/// Gets the range length computed as <see cref="Max"/> minus <see cref="Min"/>.
	/// </summary>
	public TNumber Length => Max - Min;

	/// <summary>
	/// Determines whether the specified value is contained in the current range (inclusive).
	/// </summary>
	/// <param name="value">The value to test.</param>
	/// <returns><c>true</c> if <paramref name="value"/> lies in [<see cref="Min"/>, <see cref="Max"/>]; otherwise <c>false</c>.</returns>
	public bool Contains(TNumber value)
		=> value >= Min && value <= Max;

	/// <summary>
	/// Determines whether the specified range is completely contained in the current range.
	/// </summary>
	/// <param name="other">The range to test.</param>
	/// <returns><c>true</c> if <paramref name="other"/> is inside this range; otherwise <c>false</c>.</returns>
	public bool Contains(NumericRange<TNumber> other)
		=> other.Min >= Min && other.Max <= Max;

	/// <summary>
	/// Computes the intersection between the current range and <paramref name="other"/>.
	/// </summary>
	/// <param name="other">The range to intersect with.</param>
	/// <returns>
	/// A new <see cref="NumericRange{TNumber}"/> describing the overlap, or <c>null</c> when the ranges do not intersect.
	/// </returns>
	public NumericRange<TNumber>? Intersect(NumericRange<TNumber> other)
	{
		var min = TNumber.Max(Min, other.Min);
		var max = TNumber.Min(Max, other.Max);

		return min <= max ? new(min, max) : null;
	}

	/// <summary>
	/// Returns a string that represents the current range using the format "{Min: X Max: Y}".
	/// </summary>
	/// <returns>A string representation of the range.</returns>
	public override string ToString() => $"{{Min:{Min} Max:{Max}}}";

	// IRange<TNumber>
	TNumber IRange<TNumber>.Min => Min;
	TNumber IRange<TNumber>.Max => Max;

	// IRange (non-generic)
	bool IRange.HasMinValue => true;
	bool IRange.HasMaxValue => true;
	object IRange.MinObj
	{
		get => Min;
		set => throw new NotSupportedException("NumericRange is immutable.");
	}
	object IRange.MaxObj
	{
		get => Max;
		set => throw new NotSupportedException("NumericRange is immutable.");
	}
}
#endif
