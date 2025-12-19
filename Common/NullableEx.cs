namespace Ecng.Common;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Represents a wrapper for nullable types that can be serialized.
/// </summary>
/// <typeparam name="T">The underlying type of the nullable value.</typeparam>
[Serializable]
[DataContract]
public class NullableEx<T> : Equatable<NullableEx<T>>
{
	#region HasValue

	/// <summary>
	/// Gets a value indicating whether the current NullableEx object has a valid value.
	/// </summary>
	public bool HasValue { get; private set; }

	#endregion

	#region Value

	private T _value;

	/// <summary>
	/// Gets or sets the value of the current NullableEx object.
	/// </summary>
	/// <exception cref="InvalidOperationException">The HasValue property is false.</exception>
	[DataMember]
	public T Value
	{
		get
		{
			if (HasValue)
				return _value;
			else
				throw new InvalidOperationException("NullableEx does not have a value.");
		}
		set
		{
			_value = value;
			HasValue = true;
		}
	}

	#endregion

	#region Equatable<NullableEx<T>> Members

	/// <inheritdoc/>
	protected override bool OnEquals(NullableEx<T> other)
	{
		if (HasValue != other.HasValue)
			return false;

		return !HasValue || Value.Equals(other.Value);
	}

	/// <inheritdoc/>
	public override NullableEx<T> Clone()
	{
		var retVal = new NullableEx<T>();

		if (HasValue)
			retVal.Value = Value;

		return retVal;
	}

	#endregion

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return HasValue ? Value.GetHashCode() : typeof(T).GetHashCode();
	}
}