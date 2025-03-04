namespace Ecng.Common;

using System;

/// <summary>
/// Provides operations for performing arithmetic calculations on objects of type T.
/// </summary>
/// <typeparam name="T">The type on which arithmetic operations are performed.</typeparam>
public interface IOperable<T> : IComparable<T>
{
	/// <summary>
	/// Adds the specified object to the current object.
	/// </summary>
	/// <param name="other">The object to add.</param>
	/// <returns>The result of the addition operation.</returns>
	T Add(T other);

	/// <summary>
	/// Subtracts the specified object from the current object.
	/// </summary>
	/// <param name="other">The object to subtract.</param>
	/// <returns>The result of the subtraction operation.</returns>
	T Subtract(T other);

	/// <summary>
	/// Multiplies the current object by the specified object.
	/// </summary>
	/// <param name="other">The object to multiply with.</param>
	/// <returns>The result of the multiplication operation.</returns>
	T Multiply(T other);

	/// <summary>
	/// Divides the current object by the specified object.
	/// </summary>
	/// <param name="other">The object to divide by.</param>
	/// <returns>The result of the division operation.</returns>
	T Divide(T other);
}