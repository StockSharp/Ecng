namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Represents a range with a minimum and maximum value.
/// </summary>
public interface IRange : ICloneable
{
	/// <summary>
	/// Gets a value indicating whether this instance has a minimum value.
	/// </summary>
	bool HasMinValue { get; }

	/// <summary>
	/// Gets a value indicating whether this instance has a maximum value.
	/// </summary>
	bool HasMaxValue { get; }

	/// <summary>
	/// Gets or sets the minimum value of the range.
	/// </summary>
	object MinObj { get; set; }

	/// <summary>
	/// Gets or sets the maximum value of the range.
	/// </summary>
	object MaxObj { get; set; }
}

/// <summary>
/// Generic range contract exposing strongly-typed bounds.
/// </summary>
/// <typeparam name="T">Range value type.</typeparam>
public interface IRange<T> : IRange
{
	/// <summary>
	/// Gets the minimum value of the range.
	/// </summary>
	T Min { get; }

	/// <summary>
	/// Gets the maximum value of the range.
	/// </summary>
	T Max { get; }

	/// <summary>
	/// Determines whether the specified value is within the current range.
	/// </summary>
	/// <param name="value">The value to test.</param>
	/// <returns>true if the value is within the range; otherwise, false.</returns>
	bool Contains(T value);

	/// <summary>
	/// Creates a sub-range from the current range given the specified minimum and maximum values.
	/// </summary>
	/// <param name="min">The minimum value of the sub-range.</param>
	/// <param name="max">The maximum value of the sub-range.</param>
	/// <returns>A new range representing the sub-range.</returns>
	IRange<T> SubRange(T min, T max);

	/// <summary>
	/// Returns the intersection of the current range with another range.
	/// </summary>
	/// <param name="range">The range with which to intersect.</param>
	/// <returns>
	/// A new <see cref="IRange{T}"/> representing the overlap between the two ranges, or null if there is no intersection.
	/// </returns>
	IRange<T> Intersect(IRange<T> range);

	/// <summary>
	/// Determines whether the current range completely contains another range.
	/// </summary>
	/// <param name="range">The range to check against.</param>
	/// <returns>true if the range is contained; otherwise, false.</returns>
	bool Contains(IRange<T> range);
}