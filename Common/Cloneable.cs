namespace Ecng.Common;

using System;

/// <summary>
/// The base class for objects that can be cloned.
/// </summary>
/// <typeparam name="T">The type of cloned object.</typeparam>
[Serializable]
public abstract class Cloneable<T> : ICloneable<T>
	//where T : Cloneable<T>
{
	/// <summary>
	/// Creates a new object that is a copy of the current instance.
	/// </summary>
	/// <returns>
	/// A new object that is a copy of this instance.
	/// </returns>
	public abstract T Clone();

	#region ICloneable Members

	/// <summary>
	/// Creates a new object that is a copy of the current instance.
	/// </summary>
	/// <returns>
	/// A new object that is a copy of this instance.
	/// </returns>
	object ICloneable.Clone()
	{
		return Clone();
	}

	#endregion
}