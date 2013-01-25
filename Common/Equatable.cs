namespace Ecng.Common
{
	using System;

	[Serializable]
	public abstract class Equatable<T> : Cloneable<T>, IEquatable<T>, IComparable<T>, IComparable
		where T : class
	{
		/// <summary>
		/// Operator !=s the specified left.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool operator !=(Equatable<T> left, T right)
		{
			return !(left == right);
		}

		public static bool operator !=(Equatable<T> left, Equatable<T> right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Operator ==s the specified left.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool operator ==(Equatable<T> left, T right)
		{
			if (ReferenceEquals(left, right))
				return true;

			return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);
		}

		public static bool operator ==(Equatable<T> left, Equatable<T> right)
		{
			if (ReferenceEquals(left, right))
				return true;

			return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right as T);
		}

		#region IEquatable<T> Members

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>
		/// true if the current object is equal to the other parameter; otherwise, false.
		/// </returns>
		public virtual bool Equals(T other)
		{
			if (ReferenceEquals(other, null))
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

		public virtual int CompareTo(T value)
		{
			return Equals(value) ? 0 : -1;
		}

		#endregion

		#region IComparable Members

		public int CompareTo(object value)
		{
			return CompareTo((T)value);
		}

		#endregion

		#region Object Members

		/// <summary>
		/// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj is T)
				return Equals((T)obj);
			else
				return false;
		}

		/// <summary>
		/// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		#endregion

		protected virtual bool OnEquals(T other)
		{
			return ReferenceEquals(this, other);
		}
	}
}