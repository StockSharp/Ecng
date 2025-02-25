namespace Ecng.Collections;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

/// <summary>
/// Represents a dynamic tuple that holds a collection of objects and provides equality comparison.
/// </summary>
public class DynamicTuple(object[] values) : Equatable<DynamicTuple>
{
	/// <summary>
	/// Gets the read-only collection of values stored in the tuple.
	/// </summary>
	public readonly IReadOnlyCollection<object> Values = values;

	/// <summary>
	/// Determines whether this instance is equal to another <see cref="DynamicTuple"/> by comparing their values.
	/// </summary>
	/// <param name="other">The other <see cref="DynamicTuple"/> to compare with.</param>
	/// <returns>True if the values are equal in the same order; otherwise, false.</returns>
	protected override bool OnEquals(DynamicTuple other)
		=> Values.SequenceEqual(other.Values);

	/// <summary>
	/// Computes a hash code for this tuple based on its values.
	/// </summary>
	/// <returns>A hash code for the tuple.</returns>
	public override int GetHashCode()
		=> Values.GetHashCodeEx();

	/// <summary>
	/// Creates a shallow copy of this tuple.
	/// </summary>
	/// <returns>A new <see cref="DynamicTuple"/> with the same values.</returns>
	public override DynamicTuple Clone()
		=> new([.. Values]);

	/// <summary>
	/// Returns a string representation of the tuple, with values separated by commas.
	/// </summary>
	/// <returns>A string representing the tuple's values.</returns>
	public override string ToString()
		=> Values.Select(v => v.ToString()).JoinComma();
}