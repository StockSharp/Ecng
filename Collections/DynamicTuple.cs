namespace Ecng.Collections;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

public class DynamicTuple(object[] values) : Equatable<DynamicTuple>
{
	public readonly IReadOnlyCollection<object> Values = values;

	protected override bool OnEquals(DynamicTuple other)
		=> Values.SequenceEqual(other.Values);

	public override int GetHashCode()
		=> Values.GetHashCodeEx();

	public override DynamicTuple Clone()
		=> new([.. Values]);

	public override string ToString()
		=> Values.Select(v => v.ToString()).JoinComma();
}