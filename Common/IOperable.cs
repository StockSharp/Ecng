namespace Ecng.Common
{
	using System;

	public interface IOperable<T> : IComparable<T>
	{
		T Add(T other);
		T Subtract(T other);
		T Multiply(T other);
		T Divide(T other);
	}
}