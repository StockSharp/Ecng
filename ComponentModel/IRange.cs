namespace Ecng.ComponentModel;

/// <summary>
/// Represents a range with a minimum and maximum value.
/// </summary>
public interface IRange
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
public interface IRange<out T> : IRange
{
	/// <summary>
	/// Gets the minimum value of the range.
	/// </summary>
	T Min { get; }

	/// <summary>
	/// Gets the maximum value of the range.
	/// </summary>
	T Max { get; }
}