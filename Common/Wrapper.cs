namespace Ecng.Common;

#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#endregion

/// <summary>
/// Represents an abstract wrapper class for a value of type <typeparamref name="T"/> that supports equality, cloning, and disposal.
/// </summary>
/// <typeparam name="T">The type of the wrapped value.</typeparam>
[Serializable]
public abstract class Wrapper<T> : Equatable<Wrapper<T>>, IDisposable
{
	#region Wrapper.ctor()

	/// <summary>
	/// Initializes a new instance of the <see cref="Wrapper{T}"/> class.
	/// </summary>
	protected Wrapper()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Wrapper{T}"/> class with the specified value.
	/// </summary>
	/// <param name="value">The value to wrap.</param>
	protected Wrapper(T value)
	{
		Value = value;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the wrapped value.
	/// </summary>
	public virtual T Value { get; set; }

	/// <summary>
	/// Gets a value indicating whether the wrapped value is not equal to the default value of type <typeparamref name="T"/>.
	/// </summary>
	public bool HasValue => !ReferenceEquals(Value, default(T));

	#endregion

	#region Operators

	/// <summary>
	/// Defines an explicit conversion operator from <see cref="Wrapper{T}"/> to <typeparamref name="T"/>.
	/// </summary>
	/// <param name="wrapper">The wrapper instance.</param>
	/// <returns>The wrapped value.</returns>
	public static explicit operator T(Wrapper<T> wrapper)
	{
		return wrapper.Value;
	}

	#endregion

	#region Equality Members

	/// <summary>
	/// Determines whether the wrapped value of this instance equals the wrapped value of another instance.
	/// </summary>
	/// <param name="other">Another instance of <see cref="Wrapper{T}"/> to compare with.</param>
	/// <returns><c>true</c> if the wrapped values are equal; otherwise, <c>false</c>.</returns>
	protected override bool OnEquals(Wrapper<T> other)
	{
		if (Value is IEnumerable<T>)
			return ((IEnumerable<T>)Value).SequenceEqual((IEnumerable<T>)other.Value);
		else
			return Value.Equals(other.Value);
	}

	/// <summary>
	/// Returns a hash code for this instance based on the wrapped value.
	/// </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		if (!HasValue)
			return 0;

		if (Value is IEnumerable enumerable)
		{
			unchecked
			{
				int hash = 17;

				foreach (var item in enumerable)
					hash = hash * 31 + (item?.GetHashCode() ?? 0);

				return hash;
			}
		}

		return Value.GetHashCode();
	}

	#endregion

	#region IDisposable Members

	/// <summary>
	/// Gets a value indicating whether this instance has already been disposed.
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <summary>
	/// Releases all resources used by the current instance.
	/// </summary>
	public void Dispose()
	{
		lock (this)
		{
			if (!IsDisposed)
			{
				DisposeManaged();
				DisposeNative();
				IsDisposed = true;
				GC.SuppressFinalize(this);
			}
		}
	}

	/// <summary>
	/// Releases managed resources used by the current instance.
	/// </summary>
	protected virtual void DisposeManaged()
	{
		Value.DoDispose();
	}

	/// <summary>
	/// Releases unmanaged (native) resources used by the current instance.
	/// </summary>
	protected virtual void DisposeNative()
	{
	}

	/// <summary>
	/// Finalizes an instance of the <see cref="Wrapper{T}"/> class.
	/// </summary>
	~Wrapper()
	{
		DisposeNative();
	}

	#endregion

	#region Object Overrides

	/// <summary>
	/// Returns a string that represents the current instance.
	/// </summary>
	/// <returns>A string representation of the wrapped value, or an empty string if no value is present.</returns>
	public override string ToString()
	{
		return HasValue ? Value.To<string>() : string.Empty;
	}

	#endregion
}