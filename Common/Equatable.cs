namespace Ecng.Common;

using System;

/// <summary>
/// Provides a base implementation for objects that support equality, comparison, and cloning.
/// </summary>
/// <typeparam name="T">The type of the derived class.</typeparam>
[Serializable]
public abstract class Equatable<T> : Cloneable<T>, IEquatable<T>, IComparable<T>, IComparable
	where T : class
{
	/// <summary>
	/// Determines whether two Equatable objects are not equal.
	/// </summary>
	/// <param name="left">The left Equatable object.</param>
	/// <param name="right">The right object of type T.</param>
	/// <returns>true if the objects are not equal; otherwise, false.</returns>
	public static bool operator !=(Equatable<T> left, T right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Determines whether two Equatable objects are not equal.
	/// </summary>
	/// <param name="left">The left Equatable object.</param>
	/// <param name="right">The right Equatable object.</param>
	/// <returns>true if the objects are not equal; otherwise, false.</returns>
	public static bool operator !=(Equatable<T> left, Equatable<T> right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Determines whether an Equatable object and an object of type T are equal.
	/// </summary>
	/// <param name="left">The Equatable object.</param>
	/// <param name="right">The object of type T.</param>
	/// <returns>true if the objects are equal; otherwise, false.</returns>
	public static bool operator ==(Equatable<T> left, T right)
	{
		if (ReferenceEquals(left, right))
			return true;

		return left is null ? right is null : left.Equals(right);
	}

	/// <summary>
	/// Determines whether two Equatable objects are equal.
	/// </summary>
	/// <param name="left">The left Equatable object.</param>
	/// <param name="right">The right Equatable object.</param>
	/// <returns>true if the objects are equal; otherwise, false.</returns>
	public static bool operator ==(Equatable<T> left, Equatable<T> right)
	{
		if (ReferenceEquals(left, right))
			return true;

		return left is null ? right is null : left.Equals(right as T);
	}

	#region IEquatable<T> Members

	/// <summary>
	/// Indicates whether the current object is equal to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
	public virtual bool Equals(T other)
	{
		if (other is null)
			return false;
		
		if (ReferenceEquals(other, this))
			return true;

		if (GetType() == other.GetType())
			return OnEquals(other);
		else
			return false;
	}

	#endregion

	#region IComparable<T> Members

	/// <summary>
	/// Compares the current object with another object of the same type.
	/// </summary>
	/// <param name="value">An object to compare with this object.</param>
	/// <returns>
	/// 0 if equal; -1 if not equal.
	/// </returns>
	public virtual int CompareTo(T value)
	{
		return Equals(value) ? 0 : -1;
	}

	#endregion

	#region IComparable Members

	/// <summary>
	/// Compares the current object with another object.
	/// </summary>
	/// <param name="value">An object to compare with this object.</param>
	/// <returns>
	/// 0 if equal; -1 if not equal.
	/// </returns>
	public int CompareTo(object value)
	{
		return CompareTo((T)value);
	}

	#endregion

	#region Object Members

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <param name="obj">The object to compare with this object.</param>
	/// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		if (obj is T t)
			return Equals(t);
		else
			return false;
	}

	/// <summary>
	/// Serves as the default hash function.
	/// </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	#endregion

	/// <summary>
	/// Determines equality between this instance and another instance of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this instance.</param>
	/// <returns>true if the objects are equal; otherwise, false.</returns>
	protected virtual bool OnEquals(T other)
	{
		return ReferenceEquals(this, other);
	}
}